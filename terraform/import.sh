#!/usr/bin/env bash
# Imports existing GitHub resources into Terraform state.
# Run once after `terraform init -backend-config=backend.hcl`.
# Safe to re-run — Terraform skips resources already in state.

set -euo pipefail

terraform import github_repository.repo                        delinea-secretserver-pam
terraform import github_team_repository.integration_engineers  integration-engineers:delinea-secretserver-pam
terraform import github_team_repository.release_builders       release_builders:delinea-secretserver-pam
terraform import github_team_repository.private_access         private-access:delinea-secretserver-pam
terraform import github_repository_ruleset.protect_default_and_release \
  delinea-secretserver-pam:11607153
