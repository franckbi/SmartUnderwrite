output "vpc_id" {
  description = "ID of the VPC"
  value       = module.vpc.vpc_id
}

output "rds_endpoint" {
  description = "RDS instance endpoint"
  value       = module.rds.db_endpoint
  sensitive   = true
}

output "s3_documents_bucket" {
  description = "S3 bucket for documents"
  value       = module.s3.documents_bucket_name
}

output "s3_frontend_bucket" {
  description = "S3 bucket for frontend"
  value       = module.s3.frontend_bucket_name
}

output "app_runner_service_url" {
  description = "App Runner service URL"
  value       = module.app_runner.service_url
}

output "cloudfront_distribution_domain" {
  description = "CloudFront distribution domain"
  value       = module.s3.cloudfront_distribution_domain
}

output "app_runner_instance_role_arn" {
  description = "App Runner instance role ARN"
  value       = module.iam.app_runner_instance_role_arn
}