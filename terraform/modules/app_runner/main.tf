# Auto Scaling Configuration
resource "aws_apprunner_auto_scaling_configuration_version" "main" {
  auto_scaling_configuration_name = "${var.name_prefix}-autoscaling"

  max_concurrency = var.max_concurrency
  max_size        = var.max_size
  min_size        = var.min_size

  tags = var.tags
}

# VPC Connector for RDS access
resource "aws_apprunner_vpc_connector" "main" {
  vpc_connector_name = "${var.name_prefix}-vpc-connector"
  subnets            = var.private_subnet_ids
  security_groups    = var.security_group_ids

  tags = var.tags
}

# App Runner Service
resource "aws_apprunner_service" "main" {
  service_name = "${var.name_prefix}-api"

  source_configuration {
    image_repository {
      image_configuration {
        port = var.container_port
        
        runtime_environment_variables = var.environment_variables
        
        runtime_environment_secrets = {
          ConnectionStrings__DefaultConnection = var.db_connection_string_parameter_arn
          JwtSettings__SecretKey              = var.jwt_secret_parameter_arn
        }
      }
      
      image_identifier      = var.container_image_uri
      image_repository_type = "ECR_PUBLIC"
    }
    
    auto_deployments_enabled = false
  }

  instance_configuration {
    cpu               = var.cpu
    memory            = var.memory
    instance_role_arn = var.instance_role_arn
  }

  network_configuration {
    egress_configuration {
      egress_type       = "VPC"
      vpc_connector_arn = aws_apprunner_vpc_connector.main.arn
    }
  }

  health_check_configuration {
    healthy_threshold   = 1
    interval            = 10
    path                = "/healthz"
    protocol            = "HTTP"
    timeout             = 5
    unhealthy_threshold = 5
  }

  auto_scaling_configuration_arn = aws_apprunner_auto_scaling_configuration_version.main.arn

  tags = var.tags

  depends_on = [aws_apprunner_vpc_connector.main]
}