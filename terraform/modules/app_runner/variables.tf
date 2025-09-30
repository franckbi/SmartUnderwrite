variable "name_prefix" {
  description = "Name prefix for resources"
  type        = string
}

variable "service_role_arn" {
  description = "ARN of the App Runner service role"
  type        = string
}

variable "instance_role_arn" {
  description = "ARN of the App Runner instance role"
  type        = string
}

variable "container_image_uri" {
  description = "Container image URI"
  type        = string
}

variable "container_port" {
  description = "Container port"
  type        = number
  default     = 8080
}

variable "cpu" {
  description = "CPU units for the service"
  type        = string
  default     = "0.25 vCPU"
}

variable "memory" {
  description = "Memory for the service"
  type        = string
  default     = "0.5 GB"
}

variable "min_size" {
  description = "Minimum number of instances"
  type        = number
  default     = 1
}

variable "max_size" {
  description = "Maximum number of instances"
  type        = number
  default     = 10
}

variable "max_concurrency" {
  description = "Maximum concurrent requests per instance"
  type        = number
  default     = 100
}

variable "environment_variables" {
  description = "Environment variables for the container"
  type        = map(string)
  default     = {}
}

variable "db_connection_string_parameter_arn" {
  description = "ARN of the SSM parameter containing the database connection string"
  type        = string
  default     = ""
}

variable "jwt_secret_parameter_arn" {
  description = "ARN of the SSM parameter containing the JWT secret"
  type        = string
  default     = ""
}

variable "private_subnet_ids" {
  description = "Private subnet IDs for VPC connector"
  type        = list(string)
}

variable "security_group_ids" {
  description = "Security group IDs for VPC connector"
  type        = list(string)
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}