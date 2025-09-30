# SmartUnderwrite Operational Runbooks

This document provides step-by-step procedures for common operational tasks, troubleshooting, and maintenance activities.

## Table of Contents

- [System Overview](#system-overview)
- [Deployment Procedures](#deployment-procedures)
- [Monitoring and Health Checks](#monitoring-and-health-checks)
- [Database Operations](#database-operations)
- [Backup and Recovery](#backup-and-recovery)
- [Security Operations](#security-operations)
- [Performance Tuning](#performance-tuning)
- [Troubleshooting](#troubleshooting)
- [Emergency Procedures](#emergency-procedures)

## System Overview

### Architecture Components

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   React SPA     │    │  .NET 8 Web API │    │   PostgreSQL    │
│   (Frontend)    │◄──►│   (Backend)     │◄──►│   (Database)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌─────────────────┐
                       │   MinIO/S3      │
                       │  (Documents)    │
                       └─────────────────┘
```

### Service Dependencies

- **API**: Depends on PostgreSQL, MinIO/S3
- **Frontend**: Depends on API
- **Database**: Independent
- **Storage**: Independent

### Key Metrics to Monitor

- API response times (< 2000ms average)
- Database connection pool usage (< 80%)
- Storage space utilization (< 85%)
- Error rates (< 1%)
- Authentication success rate (> 99%)

## Deployment Procedures

### Local Development Deployment

#### Initial Setup

```bash
# 1. Clone repository
git clone <repository-url>
cd SmartUnderwrite

# 2. Start services
make up

# 3. Run migrations
make migrate

# 4. Seed test data
make seed

# 5. Verify deployment
make verify
```

#### Update Deployment

```bash
# 1. Pull latest changes
git pull origin main

# 2. Rebuild and restart services
make down
make build
make up

# 3. Run any new migrations
make migrate

# 4. Verify health
make verify
```

### Production Deployment (AWS)

#### Prerequisites

- AWS CLI configured with appropriate permissions
- Terraform installed
- Docker images built and pushed to ECR

#### Infrastructure Deployment

```bash
# 1. Navigate to terraform directory
cd terraform

# 2. Initialize Terraform
terraform init

# 3. Plan deployment
terraform plan -var-file="environments/prod.tfvars"

# 4. Apply changes (with approval)
terraform apply -var-file="environments/prod.tfvars"

# 5. Verify deployment
aws apprunner describe-service --service-arn <service-arn>
```

#### Application Deployment

```bash
# 1. Build and tag Docker images
docker build -t smartunderwrite-api:latest .
docker tag smartunderwrite-api:latest <ecr-repo>:latest

# 2. Push to ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <ecr-repo>
docker push <ecr-repo>:latest

# 3. Update App Runner service
aws apprunner start-deployment --service-arn <service-arn>

# 4. Monitor deployment
aws apprunner describe-service --service-arn <service-arn>
```

#### Database Migration in Production

```bash
# 1. Create database backup
pg_dump -h <rds-endpoint> -U <username> -d smartunderwrite > backup_$(date +%Y%m%d_%H%M%S).sql

# 2. Run migrations
dotnet ef database update --connection "<production-connection-string>"

# 3. Verify migration
psql -h <rds-endpoint> -U <username> -d smartunderwrite -c "\dt"
```

### Rollback Procedures

#### Application Rollback

```bash
# 1. Identify previous working version
aws apprunner list-operations --service-arn <service-arn>

# 2. Deploy previous image version
docker tag <ecr-repo>:<previous-tag> <ecr-repo>:latest
docker push <ecr-repo>:latest

# 3. Start deployment
aws apprunner start-deployment --service-arn <service-arn>

# 4. Verify rollback
curl -f https://<app-url>/api/health/healthz
```

#### Database Rollback

```bash
# 1. Stop application traffic
aws apprunner pause-service --service-arn <service-arn>

# 2. Restore from backup
psql -h <rds-endpoint> -U <username> -d smartunderwrite < backup_<timestamp>.sql

# 3. Restart application
aws apprunner resume-service --service-arn <service-arn>
```

## Monitoring and Health Checks

### Health Check Endpoints

#### API Health Check

```bash
# Basic health check
curl -f https://<api-url>/api/health/healthz

# Expected response:
{
  "status": "Healthy",
  "timestamp": "2024-01-01T12:00:00Z",
  "version": "1.0.0"
}
```

#### Readiness Check

```bash
# Readiness check with dependencies
curl -f https://<api-url>/api/health/readyz

# Expected response:
{
  "status": "Ready",
  "timestamp": "2024-01-01T12:00:00Z",
  "checks": {
    "database": "Healthy",
    "storage": "Healthy"
  }
}
```

### Monitoring Dashboards

#### CloudWatch Metrics (AWS)

Key metrics to monitor:

```bash
# API response times
aws cloudwatch get-metric-statistics \
  --namespace "AWS/AppRunner" \
  --metric-name "ResponseTime" \
  --dimensions Name=ServiceName,Value=smartunderwrite-api \
  --start-time 2024-01-01T00:00:00Z \
  --end-time 2024-01-01T23:59:59Z \
  --period 300 \
  --statistics Average

# Error rates
aws cloudwatch get-metric-statistics \
  --namespace "AWS/AppRunner" \
  --metric-name "4xxStatusResponses" \
  --dimensions Name=ServiceName,Value=smartunderwrite-api \
  --start-time 2024-01-01T00:00:00Z \
  --end-time 2024-01-01T23:59:59Z \
  --period 300 \
  --statistics Sum
```

#### Database Monitoring

```sql
-- Connection count
SELECT count(*) as active_connections
FROM pg_stat_activity
WHERE state = 'active';

-- Long running queries
SELECT pid, now() - pg_stat_activity.query_start AS duration, query
FROM pg_stat_activity
WHERE (now() - pg_stat_activity.query_start) > interval '5 minutes';

-- Database size
SELECT pg_size_pretty(pg_database_size('smartunderwrite')) as database_size;

-- Table sizes
SELECT schemaname,tablename,pg_size_pretty(size) as size
FROM (
  SELECT schemaname,tablename,pg_relation_size(schemaname||'.'||tablename) as size
  FROM pg_tables WHERE schemaname NOT IN ('information_schema','pg_catalog')
) s
ORDER BY size DESC;
```

### Log Analysis

#### Application Logs

```bash
# View recent API logs (local)
docker-compose logs -f --tail=100 api

# Search for errors
docker-compose logs api | grep -i error

# View logs with correlation ID
docker-compose logs api | grep "12345678-1234-1234-1234-123456789012"
```

#### AWS CloudWatch Logs

```bash
# View log streams
aws logs describe-log-streams --log-group-name "/aws/apprunner/smartunderwrite-api"

# Get recent logs
aws logs get-log-events \
  --log-group-name "/aws/apprunner/smartunderwrite-api" \
  --log-stream-name "<stream-name>" \
  --start-time $(date -d "1 hour ago" +%s)000
```

## Database Operations

### Routine Maintenance

#### Database Statistics Update

```sql
-- Update table statistics
ANALYZE;

-- Update specific table statistics
ANALYZE "LoanApplications";

-- Vacuum to reclaim space
VACUUM ANALYZE;
```

#### Index Maintenance

```sql
-- Check index usage
SELECT schemaname, tablename, indexname, idx_scan, idx_tup_read, idx_tup_fetch
FROM pg_stat_user_indexes
ORDER BY idx_scan DESC;

-- Rebuild index if needed
REINDEX INDEX CONCURRENTLY ix_loanapplication_affiliateid_status;

-- Check for missing indexes
SELECT schemaname, tablename, attname, n_distinct, correlation
FROM pg_stats
WHERE schemaname = 'public'
AND n_distinct > 100
AND correlation < 0.1;
```

#### Connection Pool Management

```sql
-- Check connection pool status
SELECT state, count(*)
FROM pg_stat_activity
GROUP BY state;

-- Kill long-running connections
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE state = 'idle in transaction'
AND now() - query_start > interval '30 minutes';
```

### Data Archival

#### Archive Old Audit Logs

```sql
-- Create archive table
CREATE TABLE "AuditLogs_Archive" (LIKE "AuditLogs" INCLUDING ALL);

-- Move old records (older than 1 year)
WITH moved_rows AS (
  DELETE FROM "AuditLogs"
  WHERE "Timestamp" < NOW() - INTERVAL '1 year'
  RETURNING *
)
INSERT INTO "AuditLogs_Archive" SELECT * FROM moved_rows;

-- Verify archive
SELECT COUNT(*) FROM "AuditLogs_Archive";
```

#### Clean Up Old Applications

```sql
-- Archive completed applications older than 2 years
WITH archived_apps AS (
  SELECT "Id" FROM "LoanApplications"
  WHERE "CreatedAt" < NOW() - INTERVAL '2 years'
  AND "Status" IN ('Approved', 'Rejected')
)
UPDATE "LoanApplications"
SET "IsArchived" = true
WHERE "Id" IN (SELECT "Id" FROM archived_apps);
```

## Backup and Recovery

### Database Backup

#### Automated Backup (Production)

```bash
#!/bin/bash
# backup-database.sh

BACKUP_DIR="/backups/smartunderwrite"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="smartunderwrite_backup_${TIMESTAMP}.sql"

# Create backup directory
mkdir -p $BACKUP_DIR

# Create backup
pg_dump -h <rds-endpoint> -U <username> -d smartunderwrite > "$BACKUP_DIR/$BACKUP_FILE"

# Compress backup
gzip "$BACKUP_DIR/$BACKUP_FILE"

# Upload to S3
aws s3 cp "$BACKUP_DIR/${BACKUP_FILE}.gz" s3://smartunderwrite-backups/database/

# Clean up local files older than 7 days
find $BACKUP_DIR -name "*.gz" -mtime +7 -delete

echo "Backup completed: ${BACKUP_FILE}.gz"
```

#### Manual Backup

```bash
# Full database backup
pg_dump -h localhost -U postgres -d smartunderwrite > backup_$(date +%Y%m%d_%H%M%S).sql

# Schema-only backup
pg_dump -h localhost -U postgres -d smartunderwrite --schema-only > schema_backup.sql

# Data-only backup
pg_dump -h localhost -U postgres -d smartunderwrite --data-only > data_backup.sql

# Specific table backup
pg_dump -h localhost -U postgres -d smartunderwrite -t "LoanApplications" > applications_backup.sql
```

### Database Recovery

#### Full Database Restore

```bash
# 1. Stop application
docker-compose stop api

# 2. Drop and recreate database
psql -h localhost -U postgres -c "DROP DATABASE IF EXISTS smartunderwrite;"
psql -h localhost -U postgres -c "CREATE DATABASE smartunderwrite;"

# 3. Restore from backup
psql -h localhost -U postgres -d smartunderwrite < backup_20240101_120000.sql

# 4. Verify restore
psql -h localhost -U postgres -d smartunderwrite -c "SELECT COUNT(*) FROM \"LoanApplications\";"

# 5. Restart application
docker-compose start api
```

#### Point-in-Time Recovery (AWS RDS)

```bash
# 1. Create new RDS instance from point-in-time
aws rds restore-db-instance-to-point-in-time \
  --source-db-instance-identifier smartunderwrite-prod \
  --target-db-instance-identifier smartunderwrite-recovery \
  --restore-time 2024-01-01T12:00:00Z

# 2. Wait for instance to be available
aws rds wait db-instance-available --db-instance-identifier smartunderwrite-recovery

# 3. Update connection string in application
# 4. Test recovery instance
# 5. Switch traffic to recovery instance if needed
```

### Document Storage Backup

#### S3 Cross-Region Replication

```bash
# Enable versioning
aws s3api put-bucket-versioning \
  --bucket smartunderwrite-documents \
  --versioning-configuration Status=Enabled

# Set up cross-region replication
aws s3api put-bucket-replication \
  --bucket smartunderwrite-documents \
  --replication-configuration file://replication-config.json
```

#### Manual Document Backup

```bash
# Sync documents to backup bucket
aws s3 sync s3://smartunderwrite-documents s3://smartunderwrite-documents-backup

# Create local backup
aws s3 sync s3://smartunderwrite-documents ./document-backup/
```

## Security Operations

### Certificate Management

#### SSL Certificate Renewal (AWS)

```bash
# Check certificate expiration
aws acm describe-certificate --certificate-arn <cert-arn>

# Request new certificate
aws acm request-certificate \
  --domain-name smartunderwrite.example.com \
  --validation-method DNS

# Update App Runner service with new certificate
aws apprunner update-service \
  --service-arn <service-arn> \
  --source-configuration file://service-config.json
```

### Access Management

#### User Account Management

```sql
-- Create new user
INSERT INTO "AspNetUsers" ("Id", "UserName", "Email", "EmailConfirmed", "PasswordHash", "SecurityStamp")
VALUES (NEWID(), 'newuser@example.com', 'newuser@example.com', 1, '<hashed-password>', NEWID());

-- Assign role
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "AspNetUsers" u, "AspNetRoles" r
WHERE u."Email" = 'newuser@example.com' AND r."Name" = 'Affiliate';

-- Disable user account
UPDATE "AspNetUsers" SET "LockoutEnabled" = 1, "LockoutEnd" = '2099-12-31'
WHERE "Email" = 'user@example.com';
```

#### API Key Rotation

```bash
# Generate new JWT secret
NEW_SECRET=$(openssl rand -base64 32)

# Update in AWS Parameter Store
aws ssm put-parameter \
  --name "/smartunderwrite/jwt-secret" \
  --value "$NEW_SECRET" \
  --type "SecureString" \
  --overwrite

# Restart application to pick up new secret
aws apprunner start-deployment --service-arn <service-arn>
```

### Security Monitoring

#### Failed Login Attempts

```sql
-- Check failed login attempts
SELECT "UserName", COUNT(*) as failed_attempts, MAX("Timestamp") as last_attempt
FROM "AuditLogs"
WHERE "Action" = 'LoginFailed'
AND "Timestamp" > NOW() - INTERVAL '1 hour'
GROUP BY "UserName"
HAVING COUNT(*) > 5
ORDER BY failed_attempts DESC;
```

#### Suspicious Activity Detection

```sql
-- Multiple applications from same IP
SELECT "IpAddress", COUNT(*) as application_count, COUNT(DISTINCT "UserId") as user_count
FROM "AuditLogs"
WHERE "EntityType" = 'LoanApplication'
AND "Action" = 'Created'
AND "Timestamp" > NOW() - INTERVAL '1 hour'
GROUP BY "IpAddress"
HAVING COUNT(*) > 10
ORDER BY application_count DESC;
```

## Performance Tuning

### Database Performance

#### Query Optimization

```sql
-- Find slow queries
SELECT query, mean_time, calls, total_time
FROM pg_stat_statements
WHERE mean_time > 1000
ORDER BY mean_time DESC
LIMIT 10;

-- Check index usage
SELECT schemaname, tablename, indexname, idx_scan, idx_tup_read
FROM pg_stat_user_indexes
WHERE idx_scan < 100
ORDER BY idx_scan;

-- Analyze query plans
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM "LoanApplications"
WHERE "AffiliateId" = 1 AND "Status" = 'Submitted';
```

#### Connection Pool Tuning

```bash
# Check current pool settings
docker-compose exec postgres psql -U postgres -c "SHOW max_connections;"
docker-compose exec postgres psql -U postgres -c "SHOW shared_buffers;"

# Monitor connection usage
docker-compose exec postgres psql -U postgres -c "
SELECT state, count(*)
FROM pg_stat_activity
GROUP BY state;
"
```

### Application Performance

#### Memory Usage Monitoring

```bash
# Check container memory usage
docker stats --no-stream

# Check .NET memory usage
curl http://localhost:8080/api/health/metrics | grep memory
```

#### Rules Engine Performance

```sql
-- Check rule evaluation times
SELECT AVG("EvaluationTimeMs") as avg_time, MAX("EvaluationTimeMs") as max_time
FROM "Decisions"
WHERE "CreatedAt" > NOW() - INTERVAL '1 hour';

-- Find applications taking long to evaluate
SELECT "Id", "CreatedAt", "EvaluatedAt",
       EXTRACT(EPOCH FROM ("EvaluatedAt" - "CreatedAt")) as evaluation_seconds
FROM "LoanApplications"
WHERE "EvaluatedAt" IS NOT NULL
AND EXTRACT(EPOCH FROM ("EvaluatedAt" - "CreatedAt")) > 10
ORDER BY evaluation_seconds DESC;
```

## Troubleshooting

### Common Issues

#### API Not Responding

**Symptoms:**

- Health check endpoints return 500 errors
- Application logs show database connection errors
- High response times

**Diagnosis:**

```bash
# Check service status
docker-compose ps

# Check API logs
docker-compose logs api --tail=50

# Check database connectivity
docker-compose exec api dotnet ef database update --dry-run
```

**Resolution:**

```bash
# Restart API service
docker-compose restart api

# If database issues, restart database
docker-compose restart postgres

# Check for resource constraints
docker stats
```

#### Database Connection Pool Exhaustion

**Symptoms:**

- "Connection pool exhausted" errors in logs
- Slow API responses
- Timeouts on database operations

**Diagnosis:**

```sql
-- Check active connections
SELECT count(*) as active_connections
FROM pg_stat_activity
WHERE state = 'active';

-- Check connection pool settings
SHOW max_connections;
```

**Resolution:**

```bash
# Increase connection pool size in appsettings.json
# "DefaultConnection": "...;Maximum Pool Size=50;"

# Restart API to apply changes
docker-compose restart api

# Monitor connection usage
watch -n 5 'docker-compose exec postgres psql -U postgres -c "SELECT state, count(*) FROM pg_stat_activity GROUP BY state;"'
```

#### High Memory Usage

**Symptoms:**

- Out of memory errors
- Container restarts
- Slow performance

**Diagnosis:**

```bash
# Check memory usage
docker stats --no-stream

# Check for memory leaks in logs
docker-compose logs api | grep -i "memory\|gc\|heap"
```

**Resolution:**

```bash
# Increase container memory limits
# Update docker-compose.yml:
# services:
#   api:
#     deploy:
#       resources:
#         limits:
#           memory: 2G

# Restart with new limits
docker-compose up -d api
```

#### Document Upload Failures

**Symptoms:**

- File upload endpoints return 500 errors
- "Storage service unavailable" errors
- Documents not appearing in MinIO

**Diagnosis:**

```bash
# Check MinIO service
docker-compose ps minio

# Check MinIO logs
docker-compose logs minio

# Test MinIO connectivity
curl -f http://localhost:9000/minio/health/live
```

**Resolution:**

```bash
# Restart MinIO service
docker-compose restart minio

# Check bucket permissions
docker-compose exec minio mc ls local/smartunderwrite-documents

# Recreate bucket if needed
docker-compose exec minio mc mb local/smartunderwrite-documents
```

### Performance Issues

#### Slow Rules Engine Evaluation

**Symptoms:**

- Application evaluation takes > 5 seconds
- High CPU usage during evaluation
- Timeouts on evaluation endpoints

**Diagnosis:**

```sql
-- Check rule complexity
SELECT "Name", LENGTH("Definition"::text) as definition_size
FROM "Rules"
WHERE "IsActive" = true
ORDER BY definition_size DESC;

-- Check evaluation times
SELECT AVG("EvaluationTimeMs"), MAX("EvaluationTimeMs")
FROM "Decisions"
WHERE "CreatedAt" > NOW() - INTERVAL '1 hour';
```

**Resolution:**

```bash
# Optimize rule definitions
# - Simplify complex expressions
# - Reduce number of active rules
# - Use rule priorities effectively

# Scale API horizontally if needed
docker-compose up -d --scale api=3
```

#### Database Performance Issues

**Symptoms:**

- Slow query responses
- High database CPU usage
- Connection timeouts

**Diagnosis:**

```sql
-- Find slow queries
SELECT query, mean_time, calls
FROM pg_stat_statements
WHERE mean_time > 1000
ORDER BY mean_time DESC;

-- Check for missing indexes
SELECT schemaname, tablename, attname
FROM pg_stats
WHERE schemaname = 'public'
AND n_distinct > 100
AND correlation < 0.1;
```

**Resolution:**

```sql
-- Add missing indexes
CREATE INDEX CONCURRENTLY ix_auditlogs_timestamp
ON "AuditLogs" ("Timestamp");

-- Update statistics
ANALYZE;

-- Consider partitioning large tables
-- (for AuditLogs table with > 1M records)
```

## Emergency Procedures

### Service Outage Response

#### Immediate Response (0-15 minutes)

1. **Assess Impact**

   ```bash
   # Check service status
   curl -f https://<api-url>/api/health/healthz

   # Check error rates
   aws cloudwatch get-metric-statistics \
     --namespace "AWS/AppRunner" \
     --metric-name "4xxStatusResponses" \
     --start-time $(date -d "15 minutes ago" -u +%Y-%m-%dT%H:%M:%SZ) \
     --end-time $(date -u +%Y-%m-%dT%H:%M:%SZ) \
     --period 300 \
     --statistics Sum
   ```

2. **Notify Stakeholders**

   ```bash
   # Send alert to operations team
   # Update status page
   # Notify key users if needed
   ```

3. **Initial Mitigation**

   ```bash
   # Restart services if needed
   aws apprunner start-deployment --service-arn <service-arn>

   # Scale up if resource constrained
   # (Update App Runner configuration)
   ```

#### Investigation Phase (15-60 minutes)

1. **Gather Information**

   ```bash
   # Collect logs
   aws logs get-log-events \
     --log-group-name "/aws/apprunner/smartunderwrite-api" \
     --start-time $(date -d "1 hour ago" +%s)000

   # Check database status
   aws rds describe-db-instances --db-instance-identifier smartunderwrite-prod

   # Check storage status
   aws s3api head-bucket --bucket smartunderwrite-documents
   ```

2. **Identify Root Cause**

   - Review recent deployments
   - Check for infrastructure changes
   - Analyze error patterns
   - Review performance metrics

3. **Implement Fix**
   - Rollback if deployment-related
   - Scale resources if capacity issue
   - Fix configuration if config-related

#### Recovery Phase (1+ hours)

1. **Verify Resolution**

   ```bash
   # Run health checks
   make verify

   # Test critical workflows
   ./scripts/e2e-validation.sh

   # Monitor metrics
   watch -n 30 'curl -s https://<api-url>/api/health/healthz'
   ```

2. **Post-Incident Activities**
   - Document incident timeline
   - Conduct post-mortem review
   - Implement preventive measures
   - Update runbooks based on learnings

### Data Corruption Response

#### Immediate Actions

1. **Stop Write Operations**

   ```bash
   # Put API in read-only mode
   aws apprunner update-service \
     --service-arn <service-arn> \
     --source-configuration file://readonly-config.json
   ```

2. **Assess Damage**

   ```sql
   -- Check data integrity
   SELECT COUNT(*) FROM "LoanApplications" WHERE "Id" IS NULL;
   SELECT COUNT(*) FROM "Decisions" WHERE "LoanApplicationId" NOT IN (SELECT "Id" FROM "LoanApplications");
   ```

3. **Restore from Backup**

   ```bash
   # Identify last good backup
   aws s3 ls s3://smartunderwrite-backups/database/ --recursive

   # Restore database
   # (Follow database recovery procedures above)
   ```

### Security Incident Response

#### Suspected Breach

1. **Immediate Containment**

   ```bash
   # Disable affected user accounts
   UPDATE "AspNetUsers" SET "LockoutEnabled" = 1, "LockoutEnd" = '2099-12-31'
   WHERE "Email" IN ('suspicious@email.com');

   # Rotate API keys
   # (Follow API key rotation procedure above)
   ```

2. **Investigation**

   ```sql
   -- Check access patterns
   SELECT "UserName", "IpAddress", "Action", "Timestamp"
   FROM "AuditLogs"
   WHERE "Timestamp" > NOW() - INTERVAL '24 hours'
   AND "UserName" = 'suspicious@email.com'
   ORDER BY "Timestamp";
   ```

3. **Recovery**
   - Reset affected user passwords
   - Review and update security policies
   - Implement additional monitoring
   - Notify relevant authorities if required

---

This runbook should be reviewed and updated regularly based on operational experience and system changes. Keep it accessible to all operations team members and ensure procedures are tested during maintenance windows.
