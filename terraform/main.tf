locals {
  repo_name = "delinea-secretserver-pam"
}

# ── Repository ────────────────────────────────────────────────────────────────

resource "github_repository" "repo" {
  name        = local.repo_name
  description = "The Delinea Secret Server PAM Provider allows for the retrieval of stored account credentials from a Delinea Secret Server secret. A valid username, password and secret share settings are required."
  visibility  = "public"

  has_issues      = true
  has_projects    = true
  has_wiki        = true
  has_discussions = false

  allow_merge_commit     = true
  allow_squash_merge     = true
  allow_rebase_merge     = true
  squash_merge_commit_title   = "COMMIT_OR_PR_TITLE"
  squash_merge_commit_message = "COMMIT_MESSAGES"

  delete_branch_on_merge = false
  allow_auto_merge       = false

  topics = ["keyfactor-pam"]
}

resource "github_repository_dependabot_security_updates" "repo" {
  repository = github_repository.repo.id
  enabled    = true
}

# ── Team access ───────────────────────────────────────────────────────────────

resource "github_team_repository" "integration_engineers" {
  team_id    = "4319508"
  repository = github_repository.repo.name
  permission = "push"
}

resource "github_team_repository" "release_builders" {
  team_id    = "6287033"
  repository = github_repository.repo.name
  permission = "admin"
}

resource "github_team_repository" "private_access" {
  team_id    = "5888166"
  repository = github_repository.repo.name
  permission = "pull"
}

# ── Integration test environment ──────────────────────────────────────────────

resource "github_repository_environment" "integration_tests" {
  repository  = github_repository.repo.name
  environment = "integration-tests"
}

resource "github_actions_environment_secret" "secret_server_url" {
  repository      = github_repository.repo.name
  environment     = github_repository_environment.integration_tests.environment
  secret_name     = "SECRET_SERVER_URL"
  value = var.secret_server_url
}

resource "github_actions_environment_secret" "secret_server_username" {
  repository      = github_repository.repo.name
  environment     = github_repository_environment.integration_tests.environment
  secret_name     = "SECRET_SERVER_USERNAME"
  value = var.secret_server_username
}

resource "github_actions_environment_secret" "secret_server_password" {
  repository      = github_repository.repo.name
  environment     = github_repository_environment.integration_tests.environment
  secret_name     = "SECRET_SERVER_PASSWORD"
  value = var.secret_server_password
}

resource "github_actions_environment_secret" "secret_server_secret_id" {
  repository      = github_repository.repo.name
  environment     = github_repository_environment.integration_tests.environment
  secret_name     = "SECRET_SERVER_SECRET_ID"
  value = var.secret_server_secret_id
}

resource "github_actions_environment_variable" "skip_tls_validation" {
  repository    = github_repository.repo.name
  environment   = github_repository_environment.integration_tests.environment
  variable_name = "SECRET_SERVER_SKIP_TLS_VALIDATION"
  value         = "true"
}

# ── Repository variable to gate integration tests in CI ───────────────────────
# When this variable is absent (no terraform apply yet), the integration-test
# job in dotnet-ci.yml is skipped entirely via `if: vars.INTEGRATION_TESTS_ENABLED == 'true'`.

resource "github_actions_variable" "integration_tests_enabled" {
  repository    = github_repository.repo.name
  variable_name = "INTEGRATION_TESTS_ENABLED"
  value         = "true"
}
