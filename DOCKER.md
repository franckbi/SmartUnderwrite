# SmartUnderwrite Docker Development Environment

This document describes how to set up and use the Docker-based development environment for SmartUnderwrite.

## Prerequisites

- Docker Desktop (or Docker Engine + Docker Compose)
- Git
- Make (optional, for convenience commands)

## Quick Start

1. **Clone the repository and navigate to the project root**

   ```bash
   git clone <repository-url>
   cd SmartUnderwrite
   ```

2. **Start the development environment**

   ```bash
   # Using the setup script
   ./scripts/setup-dev.sh

   # Or using Make
   make setup

   # Or using Docker Compose directly
   docker-compose up --build -d
   ```

3. **Access the applications**
   - Frontend: http://localhost:3000
   - API: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger
   - MinIO Console: http://localhost:9001 (minioadmin/minioadmin123)

## Services

### PostgreSQL Database

- **Port**: 5432
- **Database**: smartunderwrite
- **Username**: postgres
- **Password**: postgres123
- **Connection String**: `Host=localhost;Database=smartunderwrite;Username=postgres;Password=postgres123`

### MinIO Object Storage

- **API Port**: 9000
- **Console Port**: 9001
- **Access Key**: minioadmin
- **Secret Key**: minioadmin123
- **Bucket**: smartunderwrite-documents

### SmartUnderwrite API

- **Port**: 8080
- **Health Check**: http://localhost:8080/healthz
- **Swagger**: http://localhost:8080/swagger

### SmartUnderwrite Frontend

- **Port**: 3000
- **Health Check**: http://localhost:3000/health

## Development Modes

### Production Mode

Standard Docker Compose setup with optimized builds:

```bash
docker-compose up --build -d
```

### Development Mode with Hot Reload

Uses volume mounts and development servers for hot reload:

```bash
# Start development environment
make dev-up

# Or manually
docker-compose -f docker-compose.yml -f docker-compose.override.yml up --build -d
```

## Common Commands

### Using Make (Recommended)

```bash
make help          # Show all available commands
make build         # Build all Docker images
make up            # Start all services
make down          # Stop all services
make logs          # View logs from all services
make clean         # Clean up containers and volumes
make test          # Run all tests
make migrate       # Run database migrations
make seed          # Seed database with test data
make dev-up        # Start development environment
make dev-down      # Stop development environment
```

### Using Docker Compose Directly

```bash
# Build and start services
docker-compose up --build -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Clean up
docker-compose down --volumes --remove-orphans

# Run commands in containers
docker-compose exec api dotnet ef database update
docker-compose exec api dotnet test
docker-compose exec postgres psql -U postgres -d smartunderwrite
```

## Database Operations

### Run Migrations

```bash
make migrate
# Or
docker-compose exec api dotnet ef database update
```

### Seed Test Data

```bash
make seed
# Or
docker-compose exec api dotnet run --project SmartUnderwrite.Api -- --seed
```

### Access Database

```bash
docker-compose exec postgres psql -U postgres -d smartunderwrite
```

### Reset Database

```bash
docker-compose down --volumes
docker-compose up -d postgres
make migrate
make seed
```

## File Storage Operations

### Access MinIO Console

1. Open http://localhost:9001
2. Login with minioadmin/minioadmin123
3. Create buckets and manage files

### MinIO CLI (Optional)

```bash
# Install MinIO client
brew install minio/stable/mc  # macOS
# or download from https://min.io/download#/linux

# Configure
mc alias set local http://localhost:9000 minioadmin minioadmin123

# List buckets
mc ls local

# Upload file
mc cp file.pdf local/smartunderwrite-documents/
```

## Troubleshooting

### Services Not Starting

1. Check if ports are available:

   ```bash
   lsof -i :3000,8080,5432,9000,9001
   ```

2. Check Docker logs:

   ```bash
   docker-compose logs [service-name]
   ```

3. Restart services:
   ```bash
   docker-compose restart [service-name]
   ```

### Database Connection Issues

1. Ensure PostgreSQL is healthy:

   ```bash
   docker-compose exec postgres pg_isready -U postgres
   ```

2. Check connection string in API logs:
   ```bash
   docker-compose logs api | grep -i connection
   ```

### Frontend Build Issues

1. Clear node_modules and rebuild:

   ```bash
   docker-compose exec frontend-dev rm -rf node_modules
   docker-compose restart frontend-dev
   ```

2. Check for TypeScript errors:
   ```bash
   docker-compose logs frontend-dev
   ```

### API Build Issues

1. Clear build artifacts:

   ```bash
   docker-compose exec api-dev dotnet clean
   docker-compose exec api-dev dotnet restore
   ```

2. Check for compilation errors:
   ```bash
   docker-compose logs api-dev
   ```

## Performance Optimization

### For Development

- Use the development override file for hot reload
- Mount source code as volumes to avoid rebuilds
- Use multi-stage builds to optimize image size

### For Production

- Use production Dockerfile with optimized builds
- Enable health checks for all services
- Use proper resource limits

## Security Considerations

### Development Environment

- Default passwords are used for convenience
- Services run with elevated privileges for development ease
- All ports are exposed for debugging

### Production Deployment

- Change all default passwords
- Use secrets management
- Implement proper network segmentation
- Run containers as non-root users
- Use TLS for all communications

## Environment Variables

### API Configuration

```bash
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Host=postgres;Database=smartunderwrite;Username=postgres;Password=postgres123
JwtSettings__SecretKey=your-super-secret-jwt-key-that-is-at-least-32-characters-long
MinioSettings__Endpoint=minio:9000
MinioSettings__AccessKey=minioadmin
MinioSettings__SecretKey=minioadmin123
```

### Frontend Configuration

```bash
VITE_API_BASE_URL=http://localhost:8080
```

## Volumes

- `postgres_data`: PostgreSQL database files
- `minio_data`: MinIO object storage files
- Source code volumes (development mode only)

## Networks

All services communicate through the `smartunderwrite-network` bridge network, allowing service discovery by container name.

## Next Steps

After setting up the development environment:

1. Run database migrations: `make migrate`
2. Seed test data: `make seed`
3. Run tests: `make test`
4. Start developing!

For production deployment, see the deployment documentation in the `docs/` directory.
