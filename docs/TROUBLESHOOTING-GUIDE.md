# SmartUnderwrite Troubleshooting Guide

Comprehensive troubleshooting guide for development and production issues in the SmartUnderwrite system.

## Table of Contents

- [Quick Diagnostics](#quick-diagnostics)
- [Development Environment Issues](#development-environment-issues)
- [Production Environment Issues](#production-environment-issues)
- [Database Issues](#database-issues)
- [Authentication and Authorization Issues](#authentication-and-authorization-issues)
- [Rules Engine Issues](#rules-engine-issues)
- [Performance Issues](#performance-issues)
- [Integration Issues](#integration-issues)
- [Monitoring and Logging](#monitoring-and-logging)

## Quick Diagnostics

### System Health Check

Run this quick diagnostic to assess overall system health:

```bash
# Check all services
make verify

# If that fails, check individual components:

# 1. Check Docker services
docker-compose ps

# 2. Check API health
curl -f http://localhost:8080/api/health/healthz

# 3. Check database connectivity
docker-compose exec postgres pg_isready -U postgres

# 4. Check MinIO storage
curl -f http://localhost:9000/minio/health/live

# 5. Check frontend
curl -f http://localhost:3000/health
```

### Log Analysis

```bash
# View all service logs
make logs

# View specific service logs
docker-compose logs api
docker-compose logs postgres
docker-compose logs minio
docker-compose logs frontend

# Search for errors
docker-compose logs | grep -i error

# Follow logs in real-time
docker-compose logs -f --tail=50
```

## Development Environment Issues

### Issue: Services Won't Start

**Symptoms:**

- `docker-compose up` fails
- Services exit immediately
- Port binding errors

**Common Causes & Solutions:**

#### Port Conflicts

```bash
# Check what's using the ports
lsof -i :8080  # API port
lsof -i :3000  # Frontend port
lsof -i :5432  # PostgreSQL port
lsof -i :9000  # MinIO port

# Kill processes using the ports
sudo kill -9 <PID>

# Or use different ports in docker-compose.override.yml
```

#### Docker Issues

```bash
# Check Docker daemon
docker info

# Clean up Docker resources
docker system prune -f

# Rebuild images
make clean
make build
```

#### Permission Issues (Linux/macOS)

```bash
# Fix Docker socket permissions
sudo chmod 666 /var/run/docker.sock

# Fix file permissions
sudo chown -R $USER:$USER .
```

### Issue: Database Connection Failures

**Symptoms:**

- API logs show "Connection refused" errors
- Entity Framework migration failures
- Database health check fails

**Diagnosis:**

```bash
# Check PostgreSQL container
docker-compose ps postgres

# Check PostgreSQL logs
docker-compose logs postgres

# Test connection manually
docker-compose exec postgres psql -U postgres -d smartunderwrite -c "SELECT 1;"
```

**Solutions:**

#### Container Not Running

```bash
# Start PostgreSQL container
docker-compose up -d postgres

# Check for startup errors
docker-compose logs postgres
```

#### Wrong Connection String

```bash
# Check connection string in appsettings.json
cat SmartUnderwrite.Api/appsettings.Development.json | grep ConnectionStrings

# Verify environment variables
docker-compose exec api env | grep ConnectionStrings
```

#### Database Not Created

```bash
# Create database manually
docker-compose exec postgres createdb -U postgres smartunderwrite

# Run migrations
make migrate
```

### Issue: Frontend Build Failures

**Symptoms:**

- Frontend container fails to start
- Build errors in logs
- TypeScript compilation errors

**Diagnosis:**

```bash
# Check frontend logs
docker-compose logs frontend

# Check Node.js version
docker-compose exec frontend node --version

# Check package.json dependencies
docker-compose exec frontend npm list
```

**Solutions:**

#### Dependency Issues

```bash
# Clear npm cache
docker-compose exec frontend npm cache clean --force

# Delete node_modules and reinstall
docker-compose exec frontend rm -rf node_modules package-lock.json
docker-compose exec frontend npm install

# Rebuild frontend container
docker-compose build frontend
```

#### TypeScript Errors

```bash
# Check TypeScript configuration
docker-compose exec frontend npx tsc --noEmit

# Fix type errors in the code
# Common issues:
# - Missing type definitions
# - Incorrect prop types
# - API response type mismatches
```

### Issue: Hot Reload Not Working

**Symptoms:**

- Changes to code don't reflect in running application
- Need to restart containers for changes

**Solutions:**

#### API Hot Reload

```bash
# Ensure volume mounts are correct in docker-compose.override.yml
# Check that ASPNETCORE_ENVIRONMENT=Development

# Restart with development configuration
make dev-up
```

#### Frontend Hot Reload

```bash
# Check Vite configuration
# Ensure proper volume mounts for src directory

# Check if polling is needed (Windows/WSL)
# Add to vite.config.ts:
# server: {
#   watch: {
#     usePolling: true
#   }
# }
```

## Production Environment Issues

### Issue: App Runner Service Failing

**Symptoms:**

- App Runner service shows "Failed" status
- HTTP 503 errors from load balancer
- Service won't start after deployment

**Diagnosis:**

```bash
# Check service status
aws apprunner describe-service --service-arn <service-arn>

# Check service logs
aws logs get-log-events \
  --log-group-name "/aws/apprunner/smartunderwrite-api" \
  --log-stream-name "<stream-name>"

# Check recent operations
aws apprunner list-operations --service-arn <service-arn>
```

**Solutions:**

#### Configuration Issues

```bash
# Check environment variables
aws apprunner describe-service --service-arn <service-arn> \
  --query 'Service.SourceConfiguration.ImageRepository.ImageConfiguration.RuntimeEnvironmentVariables'

# Update configuration
aws apprunner update-service \
  --service-arn <service-arn> \
  --source-configuration file://updated-config.json
```

#### Resource Constraints

```bash
# Check service configuration
aws apprunner describe-service --service-arn <service-arn> \
  --query 'Service.InstanceConfiguration'

# Scale up resources
aws apprunner update-service \
  --service-arn <service-arn> \
  --instance-configuration Cpu=1024,Memory=2048
```

### Issue: RDS Connection Problems

**Symptoms:**

- Database connection timeouts
- "Too many connections" errors
- Slow database responses

**Diagnosis:**

```bash
# Check RDS instance status
aws rds describe-db-instances --db-instance-identifier smartunderwrite-prod

# Check connection count
psql -h <rds-endpoint> -U <username> -d smartunderwrite -c "
SELECT count(*) as active_connections
FROM pg_stat_activity
WHERE state = 'active';"

# Check for long-running queries
psql -h <rds-endpoint> -U <username> -d smartunderwrite -c "
SELECT pid, now() - pg_stat_activity.query_start AS duration, query
FROM pg_stat_activity
WHERE (now() - pg_stat_activity.query_start) > interval '5 minutes';"
```

**Solutions:**

#### Connection Pool Exhaustion

```bash
# Increase max_connections parameter
aws rds modify-db-parameter-group \
  --db-parameter-group-name smartunderwrite-params \
  --parameters ParameterName=max_connections,ParameterValue=200,ApplyMethod=pending-reboot

# Reboot RDS instance to apply changes
aws rds reboot-db-instance --db-instance-identifier smartunderwrite-prod
```

#### Application Connection Pool Issues

```bash
# Update connection string with larger pool size
# "DefaultConnection": "...;Maximum Pool Size=50;Connection Lifetime=300;"

# Deploy updated configuration
aws apprunner start-deployment --service-arn <service-arn>
```

### Issue: S3/Storage Access Problems

**Symptoms:**

- Document upload failures
- "Access Denied" errors
- Files not appearing in S3 bucket

**Diagnosis:**

```bash
# Check bucket exists and is accessible
aws s3 ls s3://smartunderwrite-documents/

# Check IAM permissions
aws iam get-role-policy \
  --role-name SmartUnderwriteAppRunnerRole \
  --policy-name S3AccessPolicy

# Test upload manually
aws s3 cp test-file.txt s3://smartunderwrite-documents/test/
```

**Solutions:**

#### Permission Issues

```bash
# Update IAM policy to include necessary S3 permissions
aws iam put-role-policy \
  --role-name SmartUnderwriteAppRunnerRole \
  --policy-name S3AccessPolicy \
  --policy-document file://s3-policy.json

# Restart App Runner service
aws apprunner start-deployment --service-arn <service-arn>
```

#### Bucket Configuration

```bash
# Check bucket policy
aws s3api get-bucket-policy --bucket smartunderwrite-documents

# Update CORS configuration if needed
aws s3api put-bucket-cors \
  --bucket smartunderwrite-documents \
  --cors-configuration file://cors-config.json
```

## Database Issues

### Issue: Migration Failures

**Symptoms:**

- `dotnet ef database update` fails
- Database schema out of sync
- Missing tables or columns

**Diagnosis:**

```bash
# Check current migration status
dotnet ef migrations list --project SmartUnderwrite.Infrastructure

# Check database schema
psql -h localhost -U postgres -d smartunderwrite -c "\dt"

# Check migration history
psql -h localhost -U postgres -d smartunderwrite -c "SELECT * FROM \"__EFMigrationsHistory\";"
```

**Solutions:**

#### Migration Conflicts

```bash
# Reset migrations (development only)
dotnet ef database drop --project SmartUnderwrite.Infrastructure
dotnet ef database update --project SmartUnderwrite.Infrastructure

# For production, create corrective migration
dotnet ef migrations add FixMigrationIssue --project SmartUnderwrite.Infrastructure
dotnet ef database update --project SmartUnderwrite.Infrastructure
```

#### Connection Issues During Migration

```bash
# Check connection string
echo $ConnectionStrings__DefaultConnection

# Test connection manually
psql -h localhost -U postgres -d smartunderwrite -c "SELECT version();"

# Run migration with verbose logging
dotnet ef database update --project SmartUnderwrite.Infrastructure --verbose
```

### Issue: Data Corruption

**Symptoms:**

- Inconsistent data relationships
- Foreign key constraint violations
- Missing or duplicate records

**Diagnosis:**

```sql
-- Check for orphaned records
SELECT COUNT(*) FROM "Decisions" d
LEFT JOIN "LoanApplications" la ON d."LoanApplicationId" = la."Id"
WHERE la."Id" IS NULL;

-- Check for duplicate records
SELECT "Email", COUNT(*)
FROM "AspNetUsers"
GROUP BY "Email"
HAVING COUNT(*) > 1;

-- Check constraint violations
SELECT conname, conrelid::regclass
FROM pg_constraint
WHERE NOT convalidated;
```

**Solutions:**

#### Clean Up Orphaned Records

```sql
-- Remove orphaned decisions
DELETE FROM "Decisions"
WHERE "LoanApplicationId" NOT IN (SELECT "Id" FROM "LoanApplications");

-- Fix referential integrity
UPDATE "Decisions" SET "LoanApplicationId" = NULL
WHERE "LoanApplicationId" NOT IN (SELECT "Id" FROM "LoanApplications");
```

#### Restore from Backup

```bash
# Stop application
docker-compose stop api

# Restore from latest backup
psql -h localhost -U postgres -d smartunderwrite < backup_latest.sql

# Restart application
docker-compose start api
```

### Issue: Performance Problems

**Symptoms:**

- Slow query responses
- High CPU usage on database
- Connection timeouts

**Diagnosis:**

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

-- Check table sizes
SELECT schemaname,tablename,pg_size_pretty(size) as size
FROM (
  SELECT schemaname,tablename,pg_relation_size(schemaname||'.'||tablename) as size
  FROM pg_tables WHERE schemaname = 'public'
) s
ORDER BY size DESC;
```

**Solutions:**

#### Add Missing Indexes

```sql
-- Common indexes to add
CREATE INDEX CONCURRENTLY ix_loanapplications_status_createdat
ON "LoanApplications" ("Status", "CreatedAt");

CREATE INDEX CONCURRENTLY ix_auditlogs_entitytype_entityid
ON "AuditLogs" ("EntityType", "EntityId");

CREATE INDEX CONCURRENTLY ix_decisions_loanapplicationid
ON "Decisions" ("LoanApplicationId");
```

#### Optimize Queries

```sql
-- Use EXPLAIN ANALYZE to understand query plans
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM "LoanApplications"
WHERE "Status" = 'Submitted'
ORDER BY "CreatedAt" DESC
LIMIT 10;

-- Rewrite inefficient queries
-- Instead of: SELECT * FROM large_table WHERE condition
-- Use: SELECT specific_columns FROM large_table WHERE indexed_condition
```

## Authentication and Authorization Issues

### Issue: JWT Token Problems

**Symptoms:**

- "Invalid token" errors
- Users getting logged out frequently
- Authentication failures

**Diagnosis:**

```bash
# Check JWT configuration
grep -r "JwtSettings" SmartUnderwrite.Api/appsettings*.json

# Decode JWT token to check claims
echo "<jwt-token>" | cut -d. -f2 | base64 -d | jq

# Check token expiration
curl -H "Authorization: Bearer <token>" http://localhost:8080/api/applications
```

**Solutions:**

#### Token Expiration Issues

```json
// Update JWT settings in appsettings.json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "Issuer": "SmartUnderwrite",
    "Audience": "SmartUnderwrite",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

#### Secret Key Issues

```bash
# Generate new secret key
openssl rand -base64 32

# Update in configuration
# Restart API service
docker-compose restart api
```

### Issue: Role-Based Access Control Problems

**Symptoms:**

- Users accessing unauthorized resources
- "Forbidden" errors for valid operations
- Incorrect role assignments

**Diagnosis:**

```sql
-- Check user roles
SELECT u."Email", r."Name" as Role
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'user@example.com';

-- Check affiliate assignments
SELECT u."Email", a."Name" as Affiliate
FROM "AspNetUsers" u
LEFT JOIN "Affiliates" a ON u."AffiliateId" = a."Id"
WHERE u."Email" = 'user@example.com';
```

**Solutions:**

#### Fix Role Assignments

```sql
-- Remove incorrect role
DELETE FROM "AspNetUserRoles"
WHERE "UserId" = (SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'user@example.com')
AND "RoleId" = (SELECT "Id" FROM "AspNetRoles" WHERE "Name" = 'WrongRole');

-- Add correct role
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "AspNetUsers" u, "AspNetRoles" r
WHERE u."Email" = 'user@example.com' AND r."Name" = 'CorrectRole';
```

#### Fix Affiliate Assignments

```sql
-- Update user's affiliate
UPDATE "AspNetUsers"
SET "AffiliateId" = (SELECT "Id" FROM "Affiliates" WHERE "ExternalId" = 'PFP001')
WHERE "Email" = 'user@example.com';
```

## Rules Engine Issues

### Issue: Rule Evaluation Failures

**Symptoms:**

- Applications stuck in "Submitted" status
- "Rule evaluation failed" errors
- Inconsistent decision outcomes

**Diagnosis:**

```bash
# Check rules engine logs
docker-compose logs api | grep -i "rule\|evaluation"

# Check active rules
curl -H "Authorization: Bearer <admin-token>" http://localhost:8080/api/rules
```

```sql
-- Check rule definitions
SELECT "Id", "Name", "IsActive", LENGTH("Definition"::text) as definition_size
FROM "Rules"
WHERE "IsActive" = true
ORDER BY definition_size DESC;

-- Check recent evaluations
SELECT la."Id", la."Status", d."Outcome", d."CreatedAt"
FROM "LoanApplications" la
LEFT JOIN "Decisions" d ON la."Id" = d."LoanApplicationId"
WHERE la."CreatedAt" > NOW() - INTERVAL '1 hour'
ORDER BY la."CreatedAt" DESC;
```

**Solutions:**

#### Invalid Rule Syntax

```bash
# Validate rule JSON
echo '<rule-json>' | jq .

# Test rule compilation
curl -X POST -H "Authorization: Bearer <admin-token>" \
     -H "Content-Type: application/json" \
     -d '<rule-json>' \
     http://localhost:8080/api/rules/validate
```

#### Rule Conflicts

```sql
-- Check for conflicting rules
SELECT r1."Name" as Rule1, r2."Name" as Rule2, r1."Priority", r2."Priority"
FROM "Rules" r1, "Rules" r2
WHERE r1."Id" != r2."Id"
AND r1."IsActive" = true AND r2."IsActive" = true
AND r1."Priority" = r2."Priority";

-- Fix priority conflicts
UPDATE "Rules" SET "Priority" = 15 WHERE "Id" = <rule-id>;
```

### Issue: Performance Problems

**Symptoms:**

- Slow rule evaluation (>5 seconds)
- High CPU usage during evaluation
- Timeout errors

**Diagnosis:**

```sql
-- Check evaluation times
SELECT AVG(EXTRACT(EPOCH FROM (d."CreatedAt" - la."CreatedAt"))) as avg_eval_seconds
FROM "LoanApplications" la
JOIN "Decisions" d ON la."Id" = d."LoanApplicationId"
WHERE la."CreatedAt" > NOW() - INTERVAL '1 hour';

-- Find slow evaluations
SELECT la."Id", EXTRACT(EPOCH FROM (d."CreatedAt" - la."CreatedAt")) as eval_seconds
FROM "LoanApplications" la
JOIN "Decisions" d ON la."Id" = d."LoanApplicationId"
WHERE EXTRACT(EPOCH FROM (d."CreatedAt" - la."CreatedAt")) > 10
ORDER BY eval_seconds DESC;
```

**Solutions:**

#### Optimize Rule Complexity

```json
// Simplify complex rules
// Instead of:
{
  "if": "CreditScore >= 600 && CreditScore < 650 && IncomeMonthly > 3000 && IncomeMonthly < 5000 && Amount > 10000",
  "then": "MANUAL"
}

// Use:
{
  "if": "CreditScore BETWEEN 600 AND 649 && IncomeMonthly BETWEEN 3001 AND 4999 && Amount > 10000",
  "then": "MANUAL"
}
```

#### Reduce Active Rules

```sql
-- Deactivate unused rules
UPDATE "Rules" SET "IsActive" = false
WHERE "Id" IN (SELECT "Id" FROM "Rules" WHERE "Name" LIKE '%Test%');

-- Combine similar rules
-- Merge rules with same conditions but different outcomes
```

## Performance Issues

### Issue: High API Response Times

**Symptoms:**

- API responses taking >2 seconds
- Timeout errors from frontend
- High server CPU/memory usage

**Diagnosis:**

```bash
# Check API performance
curl -w "@curl-format.txt" -o /dev/null -s http://localhost:8080/api/applications

# Monitor resource usage
docker stats --no-stream

# Check for memory leaks
docker-compose exec api dotnet-counters monitor --process-id 1
```

**Solutions:**

#### Database Query Optimization

```csharp
// Add pagination to large result sets
public async Task<PagedResult<LoanApplicationDto>> GetApplicationsAsync(
    ApplicationFilter filter,
    int page = 1,
    int pageSize = 10)
{
    var query = _context.LoanApplications
        .Where(/* filters */)
        .OrderByDescending(x => x.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize);

    return await query.ToListAsync();
}
```

#### Caching Implementation

```csharp
// Add caching for frequently accessed data
[HttpGet("rules")]
[ResponseCache(Duration = 300)] // 5 minutes
public async Task<IActionResult> GetRules()
{
    var rules = await _ruleService.GetActiveRulesAsync();
    return Ok(rules);
}
```

#### Connection Pool Tuning

```json
// Optimize connection string
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=smartunderwrite;Username=postgres;Password=postgres;Maximum Pool Size=50;Connection Lifetime=300;Command Timeout=30;"
  }
}
```

### Issue: Frontend Performance Problems

**Symptoms:**

- Slow page loads
- Unresponsive UI
- High browser memory usage

**Diagnosis:**

```bash
# Check bundle size
docker-compose exec frontend npm run build -- --analyze

# Check for console errors
# Open browser dev tools and check console

# Monitor network requests
# Use browser dev tools Network tab
```

**Solutions:**

#### Code Splitting

```typescript
// Implement lazy loading for routes
const ApplicationsPage = lazy(() => import("./pages/ApplicationsPage"));
const AdminPage = lazy(() => import("./pages/AdminPage"));

// Use Suspense for loading states
<Suspense fallback={<LoadingSpinner />}>
  <Routes>
    <Route path="/applications" element={<ApplicationsPage />} />
    <Route path="/admin" element={<AdminPage />} />
  </Routes>
</Suspense>;
```

#### API Request Optimization

```typescript
// Implement request debouncing
const debouncedSearch = useMemo(
  () =>
    debounce((searchTerm: string) => {
      searchApplications(searchTerm);
    }, 300),
  []
);

// Use React Query for caching
const { data: applications, isLoading } = useQuery(
  ["applications", filters],
  () => applicationService.getApplications(filters),
  { staleTime: 5 * 60 * 1000 } // 5 minutes
);
```

## Integration Issues

### Issue: MinIO/S3 Integration Problems

**Symptoms:**

- Document upload failures
- "Service unavailable" errors
- Files not accessible after upload

**Diagnosis:**

```bash
# Check MinIO service
docker-compose ps minio

# Test MinIO API
curl -f http://localhost:9000/minio/health/live

# Check bucket configuration
docker-compose exec minio mc ls local/smartunderwrite-documents
```

**Solutions:**

#### Service Configuration

```bash
# Restart MinIO service
docker-compose restart minio

# Recreate bucket with correct permissions
docker-compose exec minio mc mb local/smartunderwrite-documents
docker-compose exec minio mc policy set public local/smartunderwrite-documents
```

#### Connection Configuration

```json
// Check MinIO settings in appsettings.json
{
  "MinioSettings": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin123",
    "BucketName": "smartunderwrite-documents",
    "UseSSL": false
  }
}
```

### Issue: Email Integration Problems

**Symptoms:**

- Email notifications not sent
- SMTP connection errors
- Authentication failures

**Diagnosis:**

```bash
# Check email configuration
grep -r "EmailSettings" SmartUnderwrite.Api/appsettings*.json

# Test SMTP connection
telnet smtp.gmail.com 587
```

**Solutions:**

#### SMTP Configuration

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "Username": "your-email@gmail.com",
    "Password": "app-password",
    "EnableSsl": true,
    "FromEmail": "noreply@smartunderwrite.com",
    "FromName": "SmartUnderwrite System"
  }
}
```

## Monitoring and Logging

### Issue: Missing or Incomplete Logs

**Symptoms:**

- No logs appearing in expected locations
- Missing correlation IDs
- Incomplete audit trails

**Diagnosis:**

```bash
# Check log configuration
grep -r "Serilog" SmartUnderwrite.Api/appsettings*.json

# Check log files
ls -la SmartUnderwrite.Api/logs/

# Check log levels
docker-compose exec api env | grep Logging
```

**Solutions:**

#### Log Configuration

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/smartunderwrite-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

#### Correlation ID Issues

```csharp
// Ensure correlation ID middleware is registered
app.UseMiddleware<CorrelationIdMiddleware>();

// Check correlation ID is being set
public class CorrelationIdMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                           ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Add("X-Correlation-ID", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
```

### Issue: Performance Monitoring Problems

**Symptoms:**

- No performance metrics available
- Unable to identify bottlenecks
- Missing health check data

**Solutions:**

#### Add Performance Counters

```csharp
// Add performance monitoring
services.AddApplicationInsightsTelemetry();

// Custom metrics
public class PerformanceMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();

        _logger.LogInformation("Request {Method} {Path} completed in {ElapsedMilliseconds}ms",
            context.Request.Method,
            context.Request.Path,
            stopwatch.ElapsedMilliseconds);
    }
}
```

#### Health Check Configuration

```csharp
// Add comprehensive health checks
services.AddHealthChecks()
    .AddDbContext<SmartUnderwriteDbContext>()
    .AddCheck<MinioHealthCheck>("minio")
    .AddCheck<RulesEngineHealthCheck>("rules-engine");

// Custom health check
public class MinioHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test MinIO connection
            var bucketExists = await _minioClient.BucketExistsAsync(_bucketName);
            return bucketExists
                ? HealthCheckResult.Healthy("MinIO is accessible")
                : HealthCheckResult.Unhealthy("MinIO bucket not accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MinIO connection failed", ex);
        }
    }
}
```

---

## Getting Additional Help

If you're still experiencing issues after following this guide:

1. **Check the logs** - Most issues leave traces in the application logs
2. **Review recent changes** - Issues often correlate with recent deployments or configuration changes
3. **Test in isolation** - Try to reproduce the issue with minimal test cases
4. **Check system resources** - Ensure adequate CPU, memory, and disk space
5. **Consult documentation** - Review API documentation and configuration guides
6. **Create an issue** - Document the problem with steps to reproduce and relevant logs

### Useful Commands for Debugging

```bash
# Complete system reset (development)
make clean
make build
make up
make migrate
make seed

# Check system resources
docker system df
docker stats --no-stream

# Export logs for analysis
docker-compose logs > system-logs-$(date +%Y%m%d-%H%M%S).txt

# Database backup before troubleshooting
pg_dump -h localhost -U postgres smartunderwrite > backup-before-fix.sql
```

Remember to always backup your data before attempting fixes in production environments!
