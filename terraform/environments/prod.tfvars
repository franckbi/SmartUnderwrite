environment         = "prod"
aws_region         = "us-east-1"
project_name       = "smartunderwrite"
vpc_cidr          = "10.0.0.0/16"

# Database configuration
db_instance_class = "db.t4g.small"
db_name          = "smartunderwrite"
db_username      = "smartunderwrite"

# Container configuration
container_image_uri = "public.ecr.aws/docker/library/nginx:latest" # Placeholder - will be updated by CI/CD
container_port     = 8080

# JWT secret - MUST be provided via environment variable or CLI
# jwt_secret = "CHANGE_ME"