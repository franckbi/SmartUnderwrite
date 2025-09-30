# Database connection string
resource "aws_ssm_parameter" "db_connection_string" {
  name  = "/${var.name_prefix}/database/connection-string"
  type  = "SecureString"
  value = "Host=${var.db_endpoint};Database=${var.db_name};Username=${var.db_username};Password=${var.db_password};SSL Mode=Require;"

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-db-connection-string"
  })
}

# Database endpoint
resource "aws_ssm_parameter" "db_endpoint" {
  name  = "/${var.name_prefix}/database/endpoint"
  type  = "String"
  value = var.db_endpoint

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-db-endpoint"
  })
}

# Database name
resource "aws_ssm_parameter" "db_name" {
  name  = "/${var.name_prefix}/database/name"
  type  = "String"
  value = var.db_name

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-db-name"
  })
}

# Database username
resource "aws_ssm_parameter" "db_username" {
  name  = "/${var.name_prefix}/database/username"
  type  = "String"
  value = var.db_username

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-db-username"
  })
}

# Database password
resource "aws_ssm_parameter" "db_password" {
  name  = "/${var.name_prefix}/database/password"
  type  = "SecureString"
  value = var.db_password

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-db-password"
  })
}

# JWT Secret
resource "aws_ssm_parameter" "jwt_secret" {
  name  = "/${var.name_prefix}/auth/jwt-secret"
  type  = "SecureString"
  value = var.jwt_secret

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-jwt-secret"
  })
}

# Application configuration
resource "aws_ssm_parameter" "app_config" {
  name = "/${var.name_prefix}/app/config"
  type = "String"
  value = jsonencode({
    Environment = var.environment
    Logging = {
      Level = var.environment == "prod" ? "Information" : "Debug"
    }
    Features = {
      EnableSwagger = var.environment != "prod"
    }
  })

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-app-config"
  })
}