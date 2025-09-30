# SmartUnderwrite Documentation

Welcome to the SmartUnderwrite documentation hub. This directory contains comprehensive guides for developers, operators, and users of the SmartUnderwrite system.

## 📚 Documentation Overview

### For Developers

- **[API Documentation](API-DOCUMENTATION.md)** - Complete API reference with authentication, endpoints, and examples
- **[Troubleshooting Guide](TROUBLESHOOTING-GUIDE.md)** - Comprehensive troubleshooting for development and production issues
- **[Development Setup](../README.md#development)** - Getting started with local development

### For Operations Teams

- **[Deployment Guide](../DEPLOYMENT.md)** - Complete deployment procedures for all environments
- **[Operational Runbooks](OPERATIONAL-RUNBOOKS.md)** - Step-by-step procedures for common operational tasks
- **[Infrastructure Documentation](../terraform/README.md)** - Terraform infrastructure setup and management

### For Users

- **[User Guide](USER-GUIDE.md)** - End-user documentation for the SmartUnderwrite portal
- **[API Integration Guide](API-INTEGRATION-GUIDE.md)** - Guide for integrating with SmartUnderwrite APIs

## 🏗️ System Architecture

SmartUnderwrite is a cloud-native credit underwriting system built with:

- **Backend**: .NET 8 Web API with Entity Framework Core
- **Frontend**: React TypeScript SPA with Material-UI
- **Database**: PostgreSQL with comprehensive audit logging
- **Storage**: MinIO/S3 for document management
- **Infrastructure**: AWS with Terraform for Infrastructure as Code

### Key Features

- ✅ **Automated Underwriting**: Configurable rules engine for loan evaluation
- ✅ **Manual Override**: Underwriter decision capabilities
- ✅ **Role-Based Access**: Admin, Underwriter, and Affiliate roles
- ✅ **Data Segregation**: Affiliate-specific data isolation
- ✅ **Comprehensive Auditing**: Full audit trails for compliance
- ✅ **Document Management**: Secure file upload and storage
- ✅ **Performance**: Concurrent rules evaluation with load balancing

## 🚀 Quick Start

### Development Environment

```bash
# Clone and start development environment
git clone <repository-url>
cd SmartUnderwrite
make up
make migrate
make seed
make verify
```

### Access Points

- **Frontend**: http://localhost:3000
- **API**: http://localhost:8080
- **Swagger**: http://localhost:8080/swagger
- **MinIO Console**: http://localhost:9001

### Test Credentials

| Role        | Email                           | Password      |
| ----------- | ------------------------------- | ------------- |
| Admin       | admin@smartunderwrite.com       | Admin123!     |
| Underwriter | underwriter@smartunderwrite.com | Under123!     |
| Affiliate   | affiliate1@pfp001.com           | Affiliate123! |

## 📖 Documentation Sections

### API Reference

#### Authentication

- [JWT Authentication](API-DOCUMENTATION.md#authentication)
- [Role-Based Access Control](API-DOCUMENTATION.md#user-roles)
- [Token Management](API-DOCUMENTATION.md#token-refresh)

#### Core Endpoints

- [Application Management](API-DOCUMENTATION.md#application-endpoints)
- [Decision Processing](API-DOCUMENTATION.md#decision-endpoints)
- [Rules Management](API-DOCUMENTATION.md#rule-endpoints)
- [Audit Logging](API-DOCUMENTATION.md#audit-endpoints)

#### Integration

- [Error Handling](API-DOCUMENTATION.md#error-handling)
- [Rate Limiting](API-DOCUMENTATION.md#rate-limiting)
- [SDK Examples](API-DOCUMENTATION.md#sdk-examples)

### Deployment & Operations

#### Environment Setup

- [Development Environment](../DEPLOYMENT.md#development-environment)
- [Staging Environment](../DEPLOYMENT.md#staging-environment)
- [Production Environment](../DEPLOYMENT.md#production-environment)

#### Infrastructure

- [Terraform Modules](../DEPLOYMENT.md#infrastructure-as-code)
- [AWS Services](../DEPLOYMENT.md#aws-infrastructure-components)
- [Security Configuration](../DEPLOYMENT.md#security-considerations)

#### CI/CD Pipeline

- [GitHub Actions](../DEPLOYMENT.md#cicd-pipeline)
- [Deployment Environments](../DEPLOYMENT.md#deployment-environments)
- [Rollback Procedures](../DEPLOYMENT.md#rollback-procedures)

### Troubleshooting & Maintenance

#### Common Issues

- [Development Environment Issues](TROUBLESHOOTING-GUIDE.md#development-environment-issues)
- [Production Environment Issues](TROUBLESHOOTING-GUIDE.md#production-environment-issues)
- [Database Issues](TROUBLESHOOTING-GUIDE.md#database-issues)
- [Performance Issues](TROUBLESHOOTING-GUIDE.md#performance-issues)

#### Operational Procedures

- [Deployment Procedures](OPERATIONAL-RUNBOOKS.md#deployment-procedures)
- [Database Operations](OPERATIONAL-RUNBOOKS.md#database-operations)
- [Backup and Recovery](OPERATIONAL-RUNBOOKS.md#backup-and-recovery)
- [Security Operations](OPERATIONAL-RUNBOOKS.md#security-operations)

## 🔧 Development Tools

### Available Commands

```bash
# Development
make dev-up          # Start development environment with hot reload
make test            # Run all tests
make validate-all    # Run complete validation suite

# Database
make migrate         # Run database migrations
make seed            # Seed test data
make reset           # Reset database completely

# Validation
make validate-e2e      # Run end-to-end workflow tests
make validate-security # Run security validation tests
make validate-audit    # Run audit logging tests
make validate-load     # Run rules engine load tests

# Utilities
make verify          # Verify system setup
make logs            # View service logs
make clean           # Clean up Docker resources
```

### Testing Framework

- **Unit Tests**: xUnit with FluentAssertions and Moq
- **Integration Tests**: WebApplicationFactory with test database
- **API Contract Tests**: Bruno CLI with comprehensive scenarios
- **End-to-End Tests**: Automated workflow validation
- **Load Tests**: Rules engine performance testing
- **Security Tests**: Authentication and authorization validation

### Code Quality

- **Static Analysis**: Built-in .NET analyzers
- **Security Scanning**: Trivy vulnerability scanner
- **Code Coverage**: Minimum 80% coverage on business logic
- **API Documentation**: OpenAPI/Swagger with examples

## 📊 Monitoring & Observability

### Health Checks

- **API Health**: `GET /api/health/healthz`
- **Readiness**: `GET /api/health/readyz`
- **Dependencies**: Database, storage, external services

### Logging

- **Structured Logging**: Serilog with correlation IDs
- **Log Levels**: Configurable per environment
- **Audit Trails**: Comprehensive business event logging
- **Error Tracking**: Detailed error information with context

### Metrics

- **Application Metrics**: Response times, throughput, error rates
- **Business Metrics**: Application volumes, decision outcomes
- **Infrastructure Metrics**: CPU, memory, database performance
- **Custom Metrics**: Rules engine evaluation times

## 🔒 Security

### Authentication & Authorization

- **JWT Tokens**: Stateless authentication with role claims
- **Role-Based Access**: Granular permissions by role
- **Data Segregation**: Affiliate-specific data isolation
- **Session Management**: Secure token storage and rotation

### Data Protection

- **Encryption**: TLS 1.3 in transit, AES-256 at rest
- **PII Protection**: Sensitive data hashing and masking
- **Audit Logging**: Immutable audit trails
- **Access Control**: Least privilege principles

### Compliance

- **Regulatory Compliance**: Comprehensive audit trails
- **Data Retention**: Configurable retention policies
- **Access Monitoring**: Failed attempts and suspicious activity
- **Incident Response**: Security incident procedures

## 🤝 Contributing

### Development Workflow

1. **Setup**: Follow development environment setup
2. **Branch**: Create feature branch from main
3. **Develop**: Make changes with tests
4. **Test**: Run validation suite
5. **Review**: Create pull request for review
6. **Deploy**: Automated deployment on merge

### Code Standards

- **C# Style**: Follow .NET coding conventions
- **TypeScript**: ESLint with strict configuration
- **Testing**: Comprehensive test coverage
- **Documentation**: Update docs with changes

### Pull Request Process

1. **Tests**: All tests must pass
2. **Review**: Code review required
3. **Documentation**: Update relevant documentation
4. **Validation**: Run complete validation suite

## 📞 Support

### Getting Help

- **Documentation**: Check relevant guides first
- **Issues**: Create GitHub issues for bugs
- **Discussions**: Use GitHub Discussions for questions
- **Emergency**: Follow incident response procedures

### Useful Resources

- **OpenAPI Spec**: http://localhost:8080/swagger
- **Health Checks**: http://localhost:8080/api/health/healthz
- **Logs**: `make logs` for development environment
- **Metrics**: CloudWatch dashboards for production

### Contact Information

- **Development Team**: Create GitHub issues
- **Operations Team**: Follow escalation procedures
- **Security Issues**: Follow responsible disclosure

---

**Last Updated**: $(date)
**Version**: 1.0.0

For the most up-to-date information, always refer to the latest documentation in the repository.
