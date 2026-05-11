output "vpc_id" {
  description = "VPC ID"
  value       = aws_vpc.main.id
}

output "rds_endpoint" {
  description = "RDS PostgreSQL primary endpoint"
  value       = aws_db_instance.postgres.endpoint
  sensitive   = true
}

output "rds_replica_endpoint" {
  description = "RDS PostgreSQL read replica endpoint"
  value       = aws_db_instance.postgres_replica.endpoint
  sensitive   = true
}

output "redis_endpoint" {
  description = "ElastiCache Redis endpoint"
  value       = aws_elasticache_replication_group.redis.configuration_endpoint_address
}

output "sqs_queue_url" {
  description = "SQS Queue URL for transaction events"
  value       = aws_sqs_queue.transaction_events.url
}

output "sqs_dlq_url" {
  description = "SQS Dead Letter Queue URL"
  value       = aws_sqs_queue.transaction_events_dlq.url
}

output "ecs_cluster_name" {
  description = "ECS Cluster name"
  value       = aws_ecs_cluster.main.name
}

output "lancamentos_alb_dns" {
  description = "Lancamentos API ALB DNS name"
  value       = aws_lb.lancamentos.dns_name
}

output "consolidado_alb_dns" {
  description = "Consolidado API ALB DNS name"
  value       = aws_lb.consolidado.dns_name
}

output "api_gateway_endpoint" {
  description = "API Gateway endpoint"
  value       = aws_apigatewayv2_api.main.api_endpoint
}

output "cognito_user_pool_id" {
  description = "Cognito User Pool ID"
  value       = aws_cognito_user_pool.main.id
}

output "cognito_user_pool_arn" {
  description = "Cognito User Pool ARN"
  value       = aws_cognito_user_pool.main.arn
}
