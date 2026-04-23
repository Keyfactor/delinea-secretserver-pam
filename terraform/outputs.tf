output "repository_url" {
  description = "HTTPS URL of the repository."
  value       = github_repository.repo.html_url
}

output "repository_ssh_clone_url" {
  description = "SSH clone URL."
  value       = github_repository.repo.ssh_clone_url
}

output "integration_tests_environment" {
  description = "Name of the GitHub environment used for integration tests."
  value       = github_repository_environment.integration_tests.environment
}
