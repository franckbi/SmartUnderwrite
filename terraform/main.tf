terraform {
  required_version = ">= 1.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

# Data sources
data "aws_caller_identity" "current" {}
data "aws_region" "current" {}

# Local values
locals {
  common_tags = {
    Project     = "SmartUnderwrite"
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
  
  name_prefix = "${var.project_name}-${var.environment}"
}

# VPC and Networking
module "vpc" {
  source = "./modules/vpc"
  
  name_prefix = local.name_prefix
  cidr_block  = var.vpc_cidr
  
  tags = local.common_tags
}

# RDS PostgreSQL
module "rds" {
  source = "./modules/rds"
  
  name_prefix           = local.name_prefix
  vpc_id               = module.vpc.vpc_id
  private_subnet_ids   = module.vpc.private_subnet_ids
  db_instance_class    = var.db_instance_class
  db_name              = var.db_name
  db_username          = var.db_username
  
  tags = local.common_tags
}

# S3 Buckets
module "s3" {
  source = "./modules/s3"
  
  name_prefix = local.name_prefix
  
  tags = local.common_tags
}

# IAM Roles
module "iam" {
  source = "./modules/iam"
  
  name_prefix           = local.name_prefix
  s3_documents_bucket   = module.s3.documents_bucket_name
  s3_frontend_bucket    = module.s3.frontend_bucket_name
  rds_instance_arn      = module.rds.db_instance_arn
  
  tags = local.common_tags
}

# SSM Parameters
module "ssm" {
  source = "./modules/ssm"
  
  name_prefix     = local.name_prefix
  db_endpoint     = module.rds.db_endpoint
  db_name         = var.db_name
  db_username     = var.db_username
  db_password     = module.rds.db_password
  jwt_secret      = var.jwt_secret
  environment     = var.environment
  
  tags = local.common_tags
}

# App Runner
module "app_runner" {
  source = "./modules/app_runner"
  
  name_prefix                = local.name_prefix
  service_role_arn          = module.iam.app_runner_service_role_arn
  instance_role_arn         = module.iam.app_runner_instance_role_arn
  container_image_uri       = var.container_image_uri
  container_port            = var.container_port
  private_subnet_ids        = module.vpc.private_subnet_ids
  security_group_ids        = [module.rds.security_group_id]
  
  # SSM Parameter ARNs
  db_connection_string_parameter_arn = "arn:aws:ssm:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}:parameter${module.ssm.db_connection_string_parameter_name}"
  jwt_secret_parameter_arn          = "arn:aws:ssm:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}:parameter${module.ssm.jwt_secret_parameter_name}"
  
  # Environment variables
  environment_variables = {
    ASPNETCORE_ENVIRONMENT = var.environment
    AWS_REGION            = data.aws_region.current.name
  }
  
  tags = local.common_tags
}

# CloudWatch
module "cloudwatch" {
  source = "./modules/cloudwatch"
  
  name_prefix = local.name_prefix
  aws_region  = var.aws_region
  
  tags = local.common_tags
}