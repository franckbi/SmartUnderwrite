output "app_runner_service_role_arn" {
  description = "ARN of the App Runner service role"
  value       = aws_iam_role.app_runner_service.arn
}

output "app_runner_instance_role_arn" {
  description = "ARN of the App Runner instance role"
  value       = aws_iam_role.app_runner_instance.arn
}

output "github_actions_role_arn" {
  description = "ARN of the GitHub Actions role"
  value       = aws_iam_role.github_actions.arn
}

output "github_oidc_provider_arn" {
  description = "ARN of the GitHub OIDC provider"
  value       = aws_iam_openid_connect_provider.github.arn
}