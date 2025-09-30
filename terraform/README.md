# SmartUnderwrite Infrastructure

This directory contains Terraform configuration for deploying SmartUnderwrite to AWS.

## Architecture

The infrastructure includes:

- **VPC**: Multi-AZ VPC with public and private subnets
- **RDS**: PostgreSQL database in private subnets
- **S3**: Buckets for document storage and frontend hosting
- **App Runner**: Containerized API service with auto-scaling
- **CloudFront**: CDN for frontend distribution
- **IAM**: Least-privilege roles for services and CI/CD
- **SSM**: Parameter Store for secrets management
- **CloudWatch**: Logging, monitoring, and alerting

## Prerequisites

1. AWS CLI configured with appropriate credentials
2. Terraform >= 1.0 installed
3. Docker for building container images

## Deployment

### Development Environment

```bash
# Initialize Terraform
terraform init

# Plan deployment
terraform plan -var-file="environments/dev.tfvars"

# Apply configuration
terraform apply -var-file="environments/dev.tfvars"
```

### Production Environment

```bash
# Set JWT secret via environment variable
export TF_VAR_jwt_secret="your-secure-jwt-secret"

# Plan deployment
terraform plan -var-file="environments/prod.tfvars"

# Apply configuration
terraform apply -var-file="environments/prod.tfvars"
```

## Configuration

### Required Variables

- `jwt_secret`: JWT signing secret (sensitive)

### Environment Files

- `environments/dev.tfvars`: Development configuration
- `environments/prod.tfvars`: Production configuration

### Outputs

After deployment, Terraform outputs:

- `app_runner_service_url`: API endpoint URL
- `cloudfront_distribution_domain`: Frontend URL
- `s3_documents_bucket`: Document storage bucket name

## Security

- All data encrypted at rest and in transit
- IAM roles follow least-privilege principle
- Secrets stored in SSM Parameter Store
- VPC with private subnets for database
- Security groups restrict network access

## Monitoring

- CloudWatch logs for application and system events
- CloudWatch dashboard for key metrics
- Alarms for high response time and error rates
- Optional SNS notifications for alerts

## CI/CD Integration

The infrastructure includes:

- GitHub OIDC provider for secure CI/CD
- IAM roles for GitHub Actions
- ECR repository for container images
- App Runner service for automated deployments

## Cleanup

```bash
terraform destroy -var-file="environments/dev.tfvars"
```

**Warning**: This will delete all resources including data. Ensure you have backups if needed.
