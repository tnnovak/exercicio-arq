variable "aws_region" {
  description = "AWS region for resources"
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "Project name used for resource naming"
  type        = string
  default     = "merchant-processing"
}

variable "environment" {
  description = "Environment (dev, staging, production)"
  type        = string
  default     = "dev"
}

variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t4g.large"
}

variable "db_username" {
  description = "Database master username"
  type        = string
  default     = "merchant_admin"
  sensitive   = true
}

variable "db_password" {
  description = "Database master password"
  type        = string
  sensitive   = true
  # In production, use AWS Secrets Manager or parameter store
  default = "PLACEHOLDER_ChangeInProduction123!"
}

variable "redis_node_type" {
  description = "ElastiCache Redis node type"
  type        = string
  default     = "cache.t4g.medium"
}

variable "ecs_task_cpu" {
  description = "ECS task CPU units"
  type        = number
  default     = 512
}

variable "ecs_task_memory" {
  description = "ECS task memory (MiB)"
  type        = number
  default     = 1024
}

variable "lancamentos_api_count" {
  description = "Desired count of Lancamentos API tasks"
  type        = number
  default     = 2
}

variable "consolidado_api_count" {
  description = "Desired count of Consolidado API tasks"
  type        = number
  default     = 4
}

variable "worker_count" {
  description = "Desired count of Worker tasks"
  type        = number
  default     = 1
}

variable "ecr_image_lancamentos" {
  description = "ECR image URI for Lancamentos API"
  type        = string
  default     = "123456789012.dkr.ecr.us-east-1.amazonaws.com/lancamentos-api:latest"
}

variable "ecr_image_consolidado" {
  description = "ECR image URI for Consolidado API"
  type        = string
  default     = "123456789012.dkr.ecr.us-east-1.amazonaws.com/consolidado-api:latest"
}

variable "ecr_image_worker" {
  description = "ECR image URI for Consolidator Worker"
  type        = string
  default     = "123456789012.dkr.ecr.us-east-1.amazonaws.com/consolidator-worker:latest"
}

variable "rate_limit_requests_per_second" {
  description = "API Gateway rate limit (requests per second)"
  type        = number
  default     = 55
}

variable "rate_limit_burst" {
  description = "API Gateway burst limit"
  type        = number
  default     = 100
}

variable "tags" {
  description = "Common tags for all resources"
  type        = map(string)
  default = {
    Project    = "MerchantProcessing"
    ManagedBy  = "Terraform"
    Repository = "https://github.com/tnnovak/exercicio-arq"
  }
}
