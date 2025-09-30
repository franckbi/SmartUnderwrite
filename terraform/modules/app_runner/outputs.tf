output "service_arn" {
  description = "ARN of the App Runner service"
  value       = aws_apprunner_service.main.arn
}

output "service_id" {
  description = "ID of the App Runner service"
  value       = aws_apprunner_service.main.service_id
}

output "service_url" {
  description = "URL of the App Runner service"
  value       = aws_apprunner_service.main.service_url
}

output "status" {
  description = "Status of the App Runner service"
  value       = aws_apprunner_service.main.status
}