# Recipes for this repo. Run `just` with no arguments to list them.

set export

# Solution and projects, relative to the repo root.
solution := "delinea-secretserver-pam.sln"
test_project := "delinea-secretserver-pam.Tests/delinea-secretserver-pam.Tests.csproj"
console_project := "TestConsole/TestConsole.csproj"

# doctooldotnet checkout to run. Override for one run: DOCTOOL_REPO=~/src/doctooldotnet just doctool
doctool_repo := env_var_or_default("DOCTOOL_REPO", env_var("HOME") / "RiderProjects/doctooldotnet")
repo_root := justfile_directory()
integration := "delinea-secretserver-pam"

default:
    @just --list

# Build the solution. Pass Debug to build the debug configuration.
build config="Release":
    dotnet build "$solution" -c "$config"

# Run unit tests (live-server tests excluded). Pass a class or test name fragment to narrow the run.
unittest filter="":
    #!/usr/bin/env bash
    set -euo pipefail
    if [ -n "$filter" ]; then
        dotnet test "$test_project" --filter "FullyQualifiedName!~Integration&FullyQualifiedName~$filter"
    else
        dotnet test "$test_project" --filter "FullyQualifiedName!~Integration"
    fi

# Run the live-server tests. They need the SECRET_SERVER_* variables; .env is loaded when present.
integrationtest:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ -f "$repo_root/.env" ]; then set -a; . "$repo_root/.env"; set +a; fi
    dotnet test "$test_project" --filter "FullyQualifiedName~Integration"

# Run TestConsole against the live server. Loads .env when present.
testconsole:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ -f "$repo_root/.env" ]; then set -a; . "$repo_root/.env"; set +a; fi
    dotnet run --project "$console_project"

# Remove build output for the solution.
clean:
    dotnet clean "$solution"

# doctool [branch] runs a local doctooldotnet branch in a temporary worktree; without one it runs the checkout as-is.

# Regenerate README.md and docs/ from docsource/ with doctooldotnet.
doctool branch="":
    #!/usr/bin/env bash
    set -euo pipefail
    [ -d "$doctool_repo" ] || { echo "doctooldotnet not found at $doctool_repo (set DOCTOOL_REPO)" >&2; exit 1; }
    run_dir="$doctool_repo"
    if [ -n "$branch" ]; then
        git -C "$doctool_repo" show-ref --verify --quiet "refs/heads/$branch" \
            || { echo "no local branch '$branch' in $doctool_repo" >&2; exit 1; }
        tmp="$(mktemp -d)"
        run_dir="$tmp/doctooldotnet"
        # A detached worktree builds the branch's commit without moving the checkout's HEAD.
        trap 'git -C "$doctool_repo" worktree remove --force "$run_dir" 2>/dev/null || true; rm -rf "$tmp"' EXIT
        git -C "$doctool_repo" worktree add --quiet --detach "$run_dir" "refs/heads/$branch"
    elif [ -n "$(git -C "$run_dir" status --porcelain)" ]; then
        echo "note: doctooldotnet has uncommitted changes; they are included" >&2
    fi
    echo "doctooldotnet: $(git -C "$run_dir" log -1 --oneline)"
    # Build from the doctooldotnet tree so this repo's global.json does not choose the SDK.
    cd "$run_dir"
    dotnet run --project src/Keyfactor.Doctool/Keyfactor.Doctool.csproj -- \
        generate-docs --integration-name "$integration" --root-repository-path "$repo_root"
