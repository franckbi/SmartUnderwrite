# SmartUnderwrite Deployment Guide

Comprehensive deployment guide covering development, staging, and production environments with Infrastructure as Code, CI/CD pipelines, and operational procedures.

## Table of Contents

- [Overview](#overview)
- [Development Environment](#development-environment)
- [Staging Environment](#staging-environment)
- [Production Environment](#production-environment)
- [Infrastructure as Code](#infrastructure-as-code)
- [CI/CD Pipeline](#cicd-pipeline)
- [Security Considerations](#security-considerations)
- [Monitoring and Maintenance](#monitoring-and-maintenance)
- [Troubleshooting](#troubleshooting)

## Overview

SmartUnderwrite supports multiple deployment environments:

- **Development**: Local Docker Compose setup for development
- **Staging**: AWS environment for testing and validation
- **Production**: AWS environment for live operations

### Architecture Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   CloudFront    │    │   App Runner    │    │   RDS Postgres  │
│   (Frontend)    │◄──►│   (API)         │◄──►│   (Database)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌─────────────────┐
                       │       S3        │
                       │   (Documents)   │
                       └─────────────────┘
```

## Development Environment

### Prerequisites

- **Docker Desktop 4.0+** with Docker Compose
- **.NET 8 SDK** (optional, for local development)
- **Node.js 18+** (optional, for frontend development)
- **Git** for version control

### Quick Start

```bash
# Clone the repository
git clone https://github.com/your-org/SmartUnderwrite.git
cd SmartUnderwrite

# Start all services
make up

# Run database migrations and seed data
make migrate
make seed

# Verify deployment
make verify
```

### Development Services

| Service  | URL                           | Purpose                  |
| -------- | ----------------------------- | ------------------------ |
| Frontend | http://localhost:3000         | React SPA                |
| API      | http://localhost:8080         | .NET Web API             |
| Swagger  | http://localhost:8080/swagger | API Documentation        |
| Database | localhost:5432                | PostgreSQL               |
| MinIO    | http://localhost:9001         | Document Storage Console |

### Test Credentials

| Role        | Email                           | Password      | Purpose                |
| ----------- | ------------------------------- | ------------- | ---------------------- |
| Admin       | admin@smartunderwrite.com       | Admin123!     | System administration  |
| Underwriter | underwriter@smartunderwrite.com | Under123!     | Loan decisions         |
| Affiliate 1 | affiliate1@pfp001.com           | Affiliate123! | Application submission |
| Affiliate 2 | affiliate2@pfp002.com           | Affiliate123! | Application submission |

### Development Commands

```bash
# Service management
make up              # Start all services
make down            # Stop all services
make restart         # Restart all services
make logs            # View service logs

# Database operations
make migrate         # Run database migrations
make seed            # Seed test data
make reset           # Reset database completely

# Development tools
make dev-up          # Start with hot reload
make test            # Run all tests
make validate-all    # Run complete validation suite

# Cleanup
make clean           # Remove containers and volumes
```

### Hot Reload Development

For active development with hot reload:

```bash
# Start development environment
make dev-up

# This enables:
# - API hot reload on code changes
# - Frontend hot reload with Vite
# - Database persistence
# - Live log streaming
```

### Development Configuration

Key configuration files:

- `docker-compose.yml` - Base service definitions
- `docker-compose.override.yml` - Development overrides
- `SmartUnderwrite.Api/appsettings.Development.json` - API configuration
- `SmartUnderwrite.Frontend/.env.development` - Frontend configuration

## Staging Environment

### Purpose

The staging environment provides:

- Pre-production testing
- Integration validation
- Performance testing
- User acceptance testing

### Infrastructure

Staging uses a scaled-down version of production infrastructure:

- **App Runner**: Single instance with 512 CPU / 1024 MB memory
- **RDS**: db.t3.micro instance
- **S3**: Standard storage class
- **CloudWatch**: Basic monitoring

### Deployment

```bash
# Deploy staging infrastructure
cd terraform
terraform workspace select staging
terraform plan -var-file="environments/staging.tfvars"
terraform apply -var-file="environments/staging.tfvars"

# Deploy application
./scripts/deploy-staging.sh
```

### Staging Configuration

```hcl
# environments/staging.tfvars
environment = "staging"
app_name = "smartunderwrite"
region = "us-east-1"

# Scaled-down resources
db_instance_class = "db.t3.micro"
db_allocated_storage = 20
app_runner_cpu = 512
app_runner_memory = 1024

# Staging-specific settings
enable_deletion_protection = false
backup_retention_period = 3
```

## Production Environment

### AWS Infrastructure Components

#### Core Services

- **AWS App Runner**: Containerized API hosting with auto-scaling
- **Amazon RDS**: Managed PostgreSQL database with Multi-AZ
- **Amazon S3**: Document storage with versioning and encryption
- **Amazon CloudFront**: CDN for frontend distribution
- **AWS IAM**: Identity and access management
- **AWS Systems Manager**: Parameter Store for secrets

#### Monitoring and Logging

- **Amazon CloudWatch**: Metrics, logs, and alarms
- **AWS X-Ray**: Distributed tracing (optional)
- **AWS Config**: Configuration compliance monitoring

#### Security

- **AWS WAF**: Web application firewall
- **AWS Certificate Manager**: SSL/TLS certificates
- **Amazon VPC**: Network isolation
- **AWS Secrets Manager**: Database credentials

### Prerequisites

- **AWS CLI** configured with appropriate permissions
- **Terraform 1.0+** for infrastructure management
- **Docker** for building container images
- **ECR repository** for storing container images

### Production Deployment Process

#### 1. Infrastructure Setup

```bash
# Navigate to terraform directory
cd terraform

# Initialize Terraform (first time only)
terraform init

# Select production workspace
terraform workspace select prod || terraform workspace new prod

# Plan infrastructure changes
terraform plan -var-file="environments/prod.tfvars"

# Apply infrastructure (requires approval)
terraform apply -var-file="environments/prod.tfvars"
```

#### 2. Application Deployment

```bash
# Build and tag Docker image
docker build -t smartunderwrite-api:latest -f SmartUnderwrite.Api/Dockerfile .

# Tag for ECR
docker tag smartunderwrite-api:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/smartunderwrite:latest

# Push to ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/smartunderwrite:latest

# Deploy to App Runner
aws apprunner start-deployment --service-arn <service-arn>

# Monitor deployment
aws apprunner describe-service --service-arn <service-arn>
```

#### 3. Database Migration

```bash
# Run database migrations (production)
dotnet ef database update --connection "<production-connection-string>"

# Verify migration
psql -h <rds-endpoint> -U <username> -d smartunderwrite -c "\dt"
```

#### 4. Frontend Deployment

```bash
# Build frontend
cd SmartUnderwrite.Frontend
npm run build

# Deploy to S3
aws s3 sync dist/ s3://<frontend-bucket>/ --delete

# Invalidate CloudFront cache
aws cloudfront create-invalidation --distribution-id <distribution-id> --paths "/*"
```

### Production Configuration

```hcl
# environments/prod.tfvars
environment = "prod"
app_name = "smartunderwrite"
region = "us-east-1"

# Production-grade resources
db_instance_class = "db.t3.small"
db_allocated_storage = 100
db_max_allocated_storage = 1000
app_runner_cpu = 1024
app_runner_memory = 2048

# Production settings
enable_deletion_protection = true
backup_retention_period = 7
multi_az = true
enable_performance_insights = true

# Security settings
enable_waf = true
enable_cloudtrail = true
```

### Environment Variables

Production environment variables are managed through AWS Systems Manager Parameter Store:

```bash
# Set database connection string
aws ssm put-parameter \
  --name "/smartunderwrite/prod/database-connection" \
  --value "Host=<rds-endpoint>;Database=smartunderwrite;Username=<username>;Password=<password>" \
  --type "SecureString"

# Set JWT secret
aws ssm put-parameter \
  --name "/smartunderwrite/prod/jwt-secret" \
  --value "<secure-random-key>" \
  --type "SecureString"

# Set MinIO/S3 configuration
aws ssm put-parameter \
  --name "/smartunderwrite/prod/storage-config" \
  --value '{"BucketName":"smartunderwrite-documents-prod","Region":"us-east-1"}' \
  --type "String"
```

## Infrastructure as Code

### Terraform Structure

```
terraform/
├── main.tf                 # Main configuration
├── variables.tf            # Input variables
├── outputs.tf              # Output values
├── environments/           # Environment-specific configs
│   ├── dev.tfvars
│   ├── staging.tfvars
│   └── prod.tfvars
└── modules/               # Reusable modules
    ├── app_runner/        # App Runner configuration
    ├── rds/              # Database setup
    ├── s3/               # Storage buckets
    ├── iam/              # Security roles
    ├── vpc/              # Network infrastructure
    ├── cloudwatch/       # Monitoring setup
    └── ssm/              # Parameter store
```

### Key Terraform Modules

#### App Runner Module

```hcl
module "app_runner" {
  source = "./modules/app_runner"

  app_name = var.app_name
  environment = var.environment

  # Container configuration
  image_uri = "${aws_ecr_repository.app.repository_url}:latest"
  cpu = var.app_runner_cpu
  memory = var.app_runner_memory

  # Environment variables from Parameter Store
  environment_variables = {
    ASPNETCORE_ENVIRONMENT = var.environment
    ConnectionStrings__DefaultConnection = data.aws_ssm_parameter.db_connection.value
    JwtSettings__SecretKey = data.aws_ssm_parameter.jwt_secret.value
  }

  # Auto scaling
  auto_scaling_config = {
    max_concurrency = 100
    max_size = 10
    min_size = 1
  }
}
```

#### RDS Module

```hcl
module "rds" {
  source = "./modules/rds"

  app_name = var.app_name
  environment = var.environment

  # Instance configuration
  instance_class = var.db_instance_class
  allocated_storage = var.db_allocated_storage
  max_allocated_storage = var.db_max_allocated_storage

  # High availability
  multi_az = var.multi_az
  backup_retention_period = var.backup_retention_period

  # Security
  deletion_protection = var.enable_deletion_protection
  storage_encrypted = true

  # Monitoring
  performance_insights_enabled = var.enable_performance_insights
  monitoring_interval = 60
}
```

### State Management

Terraform state is stored remotely for collaboration and security:

```hcl
terraform {
  backend "s3" {
    bucket = "smartunderwrite-terraform-state"
    key    = "terraform.tfstate"
    region = "us-east-1"

    # State locking
    dynamodb_table = "smartunderwrite-terraform-locks"
    encrypt        = true
  }
}
```

## CI/CD Pipeline

### GitHub Actions Workflow

The CI/CD pipeline provides automated testing, building, and deployment:

```yaml
name: SmartUnderwrite CI/CD
on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test:
    name: Test and Validate
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: "8.0.x"

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: "18"

      - name: Restore dependencies
        run: dotnet restore

      - name: Build application
        run: dotnet build --no-restore

      - name: Run unit tests
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"

      - name: Run integration tests
        run: dotnet test SmartUnderwrite.IntegrationTests/ --verbosity normal

      - name: Build frontend
        run: |
          cd SmartUnderwrite.Frontend
          npm ci
          npm run build

      - name: Run API contract tests
        run: |
          docker-compose up -d
          sleep 30
          ./run-api-tests.sh
          docker-compose down

  security:
    name: Security Scan
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Run Trivy vulnerability scanner
        uses: aquasecurity/trivy-action@master
        with:
          scan-type: "fs"
          scan-ref: "."

      - name: Run CodeQL Analysis
        uses: github/codeql-action/analyze@v2

  build:
    name: Build and Push Images
    needs: [test, security]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v4

      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v2
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: us-east-1

      - name: Login to Amazon ECR
        id: login-ecr
        uses: aws-actions/amazon-ecr-login@v1

      - name: Build and push API image
        env:
          ECR_REGISTRY: ${{ steps.login-ecr.outputs.registry }}
          ECR_REPOSITORY: smartunderwrite-api
          IMAGE_TAG: ${{ github.sha }}
        run: |
          docker build -t $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG -f SmartUnderwrite.Api/Dockerfile .
          docker push $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG
          docker tag $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG $ECR_REGISTRY/$ECR_REPOSITORY:latest
          docker push $ECR_REGISTRY/$ECR_REPOSITORY:latest

  deploy-staging:
    name: Deploy to Staging
    needs: build
    runs-on: ubuntu-latest
    environment: staging
    steps:
      - uses: actions/checkout@v4

      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v2

      - name: Deploy infrastructure
        run: |
          cd terraform
          terraform init
          terraform workspace select staging
          terraform plan -var-file="environments/staging.tfvars"
          terraform apply -auto-approve -var-file="environments/staging.tfvars"

      - name: Deploy application
        run: |
          aws apprunner start-deployment --service-arn ${{ secrets.STAGING_APP_RUNNER_ARN }}

      - name: Run smoke tests
        run: |
          sleep 60  # Wait for deployment
          curl -f ${{ secrets.STAGING_API_URL }}/api/health/healthz

  deploy-production:
    name: Deploy to Production
    needs: deploy-staging
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v4

      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v2

      - name: Deploy infrastructure
        run: |
          cd terraform
          terraform init
          terraform workspace select prod
          terraform plan -var-file="environments/prod.tfvars"
          terraform apply -auto-approve -var-file="environments/prod.tfvars"

      - name: Deploy application
        run: |
          aws apprunner start-deployment --service-arn ${{ secrets.PROD_APP_RUNNER_ARN }}

      - name: Run production validation
        run: |
          sleep 120  # Wait for deployment
          ./scripts/production-validation.sh
```

### Deployment Environments

GitHub environments provide deployment controls:

#### Staging Environment

- **Auto-deployment**: On successful main branch builds
- **Reviewers**: Not required
- **Secrets**: Staging AWS credentials and service ARNs

#### Production Environment

- **Manual approval**: Required before deployment
- **Reviewers**: Senior developers and DevOps team
- **Secrets**: Production AWS credentials and service ARNs
- **Branch protection**: Only main branch deployments allowed

### Rollback Procedures

#### Automatic Rollback

```yaml
- name: Health check and rollback
  run: |
    # Wait for deployment to complete
    aws apprunner wait service-updated --service-arn ${{ secrets.PROD_APP_RUNNER_ARN }}

    # Health check
    if ! curl -f ${{ secrets.PROD_API_URL }}/api/health/healthz; then
      echo "Health check failed, rolling back..."
      
      # Get previous image
      PREVIOUS_IMAGE=$(aws apprunner describe-service --service-arn ${{ secrets.PROD_APP_RUNNER_ARN }} \
        --query 'Service.SourceConfiguration.ImageRepository.ImageIdentifier' --output text | \
        sed 's/:latest/:previous/')
      
      # Update service with previous image
      aws apprunner update-service \
        --service-arn ${{ secrets.PROD_APP_RUNNER_ARN }} \
        --source-configuration ImageRepository="{ImageIdentifier=$PREVIOUS_IMAGE}"
      
      exit 1
    fi
```

#### Manual Rollback

```bash
# List recent deployments
aws apprunner list-operations --service-arn <service-arn>

# Rollback to specific image
aws apprunner update-service \
  --service-arn <service-arn> \
  --source-configuration file://rollback-config.json

# Monitor rollback
aws apprunner describe-service --service-arn <service-arn>
```

## Security Considerations

### Infrastructure Security

#### Network Security

- **VPC**: Isolated network environment
- **Private Subnets**: Database and internal services
- **Security Groups**: Restrictive ingress/egress rules
- **NACLs**: Additional network-level protection

#### Data Protection

- **Encryption at Rest**: RDS and S3 encryption enabled
- **Encryption in Transit**: TLS 1.3 for all communications
- **Key Management**: AWS KMS for encryption keys
- **Backup Encryption**: Automated backups encrypted

#### Access Control

- **IAM Roles**: Least privilege access principles
- **Service Accounts**: Dedicated roles for each service
- **MFA**: Required for administrative access
- **Audit Logging**: CloudTrail for all API calls

### Application Security

#### Authentication & Authorization

- **JWT Tokens**: Short-lived with refresh capability
- **Role-Based Access**: Admin, Underwriter, Affiliate roles
- **Data Segregation**: Affiliate-specific data isolation
- **Session Management**: Secure token storage and rotation

#### Input Validation

- **API Validation**: Comprehensive input validation
- **SQL Injection Protection**: Parameterized queries
- **XSS Protection**: Input sanitization and CSP headers
- **File Upload Security**: Type validation and virus scanning

#### Security Headers

```csharp
// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");

    await next();
});
```

### Secrets Management

#### Development

- **Environment Variables**: Local development secrets
- **Docker Secrets**: Container-level secret management
- **Git Exclusion**: Secrets never committed to repository

#### Production

- **AWS Parameter Store**: Encrypted parameter storage
- **AWS Secrets Manager**: Database credentials rotation
- **IAM Roles**: Service-to-service authentication
- **Environment Injection**: Runtime secret injection

```bash
# Store production secrets
aws ssm put-parameter \
  --name "/smartunderwrite/prod/jwt-secret" \
  --value "$(openssl rand -base64 32)" \
  --type "SecureString" \
  --description "JWT signing key for production"

# Rotate database password
aws secretsmanager rotate-secret \
  --secret-id "smartunderwrite/prod/database" \
  --rotation-lambda-arn "arn:aws:lambda:us-east-1:123456789012:function:SecretsManagerRDSPostgreSQLRotationSingleUser"
```

## Monitoring and Maintenance

### Health Monitoring

#### Application Health Checks

```csharp
// Comprehensive health checks
services.AddHealthChecks()
    .AddDbContext<SmartUnderwriteDbContext>(name: "database")
    .AddCheck<S3HealthCheck>("s3-storage")
    .AddCheck<RulesEngineHealthCheck>("rules-engine")
    .AddCheck<ExternalApiHealthCheck>("external-apis");
```

#### Infrastructure Monitoring

- **CloudWatch Metrics**: CPU, memory, disk, network
- **Custom Metrics**: Application-specific metrics
- **Log Aggregation**: Centralized logging with correlation IDs
- **Distributed Tracing**: Request flow across services

#### Alerting

```hcl
# CloudWatch alarms
resource "aws_cloudwatch_metric_alarm" "high_error_rate" {
  alarm_name          = "smartunderwrite-high-error-rate"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "4XXError"
  namespace           = "AWS/AppRunner"
  period              = "300"
  statistic           = "Sum"
  threshold           = "10"
  alarm_description   = "This metric monitors high error rate"
  alarm_actions       = [aws_sns_topic.alerts.arn]
}
```

### Backup and Recovery

#### Database Backups

- **Automated Backups**: Daily backups with 7-day retention
- **Point-in-Time Recovery**: Up to 35 days
- **Cross-Region Replication**: Disaster recovery
- **Backup Verification**: Automated restore testing

#### Document Storage Backups

- **S3 Versioning**: Multiple versions of documents
- **Cross-Region Replication**: Automatic replication
- **Lifecycle Policies**: Automated archival to Glacier
- **Backup Validation**: Integrity checks

#### Configuration Backups

- **Terraform State**: Remote state with versioning
- **Parameter Store**: Automated snapshots
- **Code Repository**: Git-based version control
- **Infrastructure Documentation**: Automated generation

### Performance Optimization

#### Database Performance

```sql
-- Performance monitoring queries
SELECT schemaname, tablename, seq_scan, seq_tup_read, idx_scan, idx_tup_fetch
FROM pg_stat_user_tables
ORDER BY seq_scan DESC;

-- Index usage analysis
SELECT schemaname, tablename, indexname, idx_scan, idx_tup_read, idx_tup_fetch
FROM pg_stat_user_indexes
WHERE idx_scan < 100
ORDER BY idx_scan;
```

#### Application Performance

- **Connection Pooling**: Optimized database connections
- **Caching**: Redis for frequently accessed data
- **CDN**: CloudFront for static content delivery
- **Auto Scaling**: Automatic scaling based on demand

#### Monitoring Dashboards

- **CloudWatch Dashboards**: Infrastructure metrics
- **Application Insights**: Application performance
- **Custom Dashboards**: Business metrics
- **Real-time Monitoring**: Live system status

## Troubleshooting

### Common Deployment Issues

#### App Runner Deployment Failures

```bash
# Check deployment status
aws apprunner describe-service --service-arn <service-arn>

# View deployment logs
aws logs get-log-events \
  --log-group-name "/aws/apprunner/smartunderwrite-api" \
  --log-stream-name "<stream-name>"

# Common fixes:
# 1. Check environment variables
# 2. Verify ECR image exists
# 3. Check IAM permissions
# 4. Validate health check endpoint
```

#### Database Connection Issues

```bash
# Test database connectivity
psql -h <rds-endpoint> -U <username> -d smartunderwrite -c "SELECT version();"

# Check security groups
aws ec2 describe-security-groups --group-ids <sg-id>

# Verify parameter store values
aws ssm get-parameter --name "/smartunderwrite/prod/database-connection" --with-decryption
```

#### Infrastructure Issues

```bash
# Terraform state issues
terraform refresh
terraform plan

# Resource conflicts
terraform import <resource_type>.<resource_name> <resource_id>

# State corruption
terraform state pull > backup.tfstate
terraform state push backup.tfstate
```

### Debugging Production Issues

#### Log Analysis

```bash
# Search for errors in CloudWatch
aws logs filter-log-events \
  --log-group-name "/aws/apprunner/smartunderwrite-api" \
  --filter-pattern "ERROR" \
  --start-time $(date -d "1 hour ago" +%s)000

# Correlation ID tracking
aws logs filter-log-events \
  --log-group-name "/aws/apprunner/smartunderwrite-api" \
  --filter-pattern "12345678-1234-1234-1234-123456789012"
```

#### Performance Analysis

```bash
# CloudWatch metrics
aws cloudwatch get-metric-statistics \
  --namespace "AWS/AppRunner" \
  --metric-name "ResponseTime" \
  --dimensions Name=ServiceName,Value=smartunderwrite-api \
  --start-time $(date -d "1 hour ago" -u +%Y-%m-%dT%H:%M:%SZ) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%SZ) \
  --period 300 \
  --statistics Average,Maximum
```

### Emergency Procedures

#### Service Outage Response

1. **Immediate Assessment**: Check service health and error rates
2. **Incident Communication**: Notify stakeholders and users
3. **Quick Mitigation**: Restart services or rollback if needed
4. **Root Cause Analysis**: Investigate and document the issue
5. **Post-Incident Review**: Implement preventive measures

#### Data Recovery

1. **Assess Data Loss**: Determine scope and impact
2. **Stop Write Operations**: Prevent further data corruption
3. **Restore from Backup**: Use most recent clean backup
4. **Validate Recovery**: Verify data integrity and completeness
5. **Resume Operations**: Gradually restore service functionality

For detailed troubleshooting procedures, see the [Troubleshooting Guide](docs/TROUBLESHOOTING-GUIDE.md) and [Operational Runbooks](docs/OPERATIONAL-RUNBOOKS.md).
