variable "name_prefix" {
  description = "Name prefix for resources"
  type        = string
}

variable "s3_documents_bucket" {
  description = "S3 documents bucket name"
  type        = string
}

variable "s3_frontend_bucket" {
  description = "S3 frontend bucket name"
  type        = string
}

variable "rds_instance_arn" {
  description = "RDS instance ARN"
  type        = string
}

variable "github_repository" {
  description = "GitHub repository in format owner/repo"
  type        = string
  default     = "*/*" # Allow any repository by default
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}