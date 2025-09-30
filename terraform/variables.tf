variable "aws_region" {
  description = "AWS region for resources"
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"
}

variable "project_name" {
  description = "Project name for resource naming"
  type        = string
  default     = "smartunderwrite"
}

variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t4g.micro"
}

variable "db_name" {
  description = "Database name"
  type        = string
  default     = "smartunderwrite"
}

variable "db_username" {
  description = "Database username"
  type        = string
  default     = "smartunderwrite"
}

variable "container_image_uri" {
  description = "Container image URI for App Runner"
  type        = string
  default     = "public.ecr.aws/docker/library/nginx:latest" # Placeholder
}

variable "container_port" {
  description = "Container port for App Runner"
  type        = number
  default     = 8080
}

variable "jwt_secret" {
  description = "JWT secret key"
  type        = string
  sensitive   = true
}