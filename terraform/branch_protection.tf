# Ruleset ID 11607153 — "Protect default and release branches"
# Covers ~DEFAULT_BRANCH and refs/heads/release-* (enforced, active).

resource "github_repository_ruleset" "protect_default_and_release" {
  name        = "Protect default and release branches"
  repository  = github_repository.repo.name
  target      = "branch"
  enforcement = "active"

  conditions {
    ref_name {
      include = ["~DEFAULT_BRANCH", "refs/heads/release-*"]
      exclude = []
    }
  }

  bypass_actors {
    actor_id    = null
    actor_type  = "OrganizationAdmin"
    bypass_mode = "always"
  }

  bypass_actors {
    actor_id    = null
    actor_type  = "DeployKey"
    bypass_mode = "always"
  }

  bypass_actors {
    actor_id    = 5
    actor_type  = "RepositoryRole"
    bypass_mode = "always"
  }

  rules {
    deletion         = true
    non_fast_forward = true
    update           = true

    pull_request {
      required_approving_review_count   = 0
      dismiss_stale_reviews_on_push     = false
      require_code_owner_review         = false
      require_last_push_approval        = false
      required_review_thread_resolution = false
    }
  }
}
