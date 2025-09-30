output "db_connection_string_parameter_name" {
  description = "SSM parameter name for database connection string"
  value       = aws_ssm_parameter.db_connection_string.name
}

output "jwt_secret_parameter_name" {
  description = "SSM parameter name for JWT secret"
  value       = aws_ssm_parameter.jwt_secret.name
}

output "app_config_parameter_name" {
  description = "SSM parameter name for application config"
  value       = aws_ssm_parameter.app_config.name
}