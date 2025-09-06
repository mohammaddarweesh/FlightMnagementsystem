# Production Deployment Guide

## Overview

This guide provides comprehensive instructions for deploying the Flight Booking Management System to production environments. The system is designed for high availability, scalability, and security.

## Prerequisites

### System Requirements

#### Minimum Production Requirements
- **CPU**: 4 cores (8 recommended)
- **RAM**: 8GB (16GB recommended)
- **Storage**: 100GB SSD (500GB recommended)
- **Network**: 1Gbps connection

#### Software Requirements
- **.NET 8 Runtime** or higher
- **PostgreSQL 15+** (managed service recommended)
- **Redis 7+** (managed service recommended)
- **Reverse Proxy** (Nginx, Apache, or cloud load balancer)
- **SSL Certificate** for HTTPS

### Cloud Provider Recommendations

#### AWS
- **Compute**: EC2 t3.large or larger
- **Database**: RDS PostgreSQL (db.t3.medium or larger)
- **Cache**: ElastiCache Redis (cache.t3.medium or larger)
- **Load Balancer**: Application Load Balancer (ALB)
- **Storage**: EBS gp3 volumes

#### Azure
- **Compute**: Standard B2s or larger
- **Database**: Azure Database for PostgreSQL (General Purpose, 2 vCores)
- **Cache**: Azure Cache for Redis (Standard C1 or larger)
- **Load Balancer**: Azure Load Balancer
- **Storage**: Premium SSD

#### Google Cloud
- **Compute**: e2-standard-2 or larger
- **Database**: Cloud SQL for PostgreSQL (db-standard-2 or larger)
- **Cache**: Memorystore for Redis (Standard Tier, 1GB)
- **Load Balancer**: Cloud Load Balancing
- **Storage**: SSD persistent disks

## Environment Configuration

### Production Environment Variables

```bash
# Database Configuration
CONNECTIONSTRINGS__DEFAULTCONNECTION="Host=your-postgres-host;Database=FlightBookingDb_Production;Username=flightbooking_app;Password=your-secure-password;SSL Mode=Require"
CONNECTIONSTRINGS__HANGFIRE="Host=your-postgres-host;Database=FlightBookingHangfire_Production;Username=flightbooking_app;Password=your-secure-password;SSL Mode=Require"
CONNECTIONSTRINGS__REDIS="your-redis-connection-string"

# JWT Configuration
JWT__SECRETKEY="your-256-bit-secret-key-here"
JWT__ISSUER="FlightBookingAPI"
JWT__AUDIENCE="FlightBookingClients"
JWT__ACCESSTOKENEXPIRATIONMINUTES=60
JWT__REFRESHTOKENEXPIRATIONDAYS=30

# Email Configuration (SendGrid recommended for production)
EMAILSETTINGS__PROVIDER="SendGrid"
EMAILSETTINGS__SENDGRID__APIKEY="your-sendgrid-api-key"
EMAILSETTINGS__FROMEMAIL="noreply@yourdomain.com"
EMAILSETTINGS__FROMNAME="Flight Booking System"

# Analytics Configuration
ANALYTICS__CACHEPROVIDER="Redis"
ANALYTICS__CACHEEXPIRATIONMINUTES=60
ANALYTICS__MAXEXPORTROWS=100000
ANALYTICS__DEFAULTDATERANGE=30

# Hangfire Configuration
HANGFIRE__CONNECTIONSTRING="Host=your-postgres-host;Database=FlightBookingHangfire_Production;Username=flightbooking_app;Password=your-secure-password;SSL Mode=Require"
HANGFIRE__SCHEMANAME="hangfire"
HANGFIRE__DASHBOARDPATH="/admin/jobs"

# Security Configuration
SECURITY__ALLOWEDORIGINS="https://yourdomain.com,https://www.yourdomain.com"
SECURITY__REQUIREHTTPS=true
SECURITY__ENABLEHSTS=true

# Logging Configuration
SERILOG__MINIMUMLEVEL="Information"
SERILOG__WRITETO__0__NAME="Console"
SERILOG__WRITETO__1__NAME="File"
SERILOG__WRITETO__1__ARGS__PATH="/var/log/flightbooking/app-.log"
SERILOG__WRITETO__1__ARGS__ROLLINGINTERVAL="Day"
SERILOG__WRITETO__1__ARGS__RETAINEDFILECOUNT=30

# Performance Configuration
ASPNETCORE_ENVIRONMENT="Production"
ASPNETCORE_URLS="http://+:5000"
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
```

### appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "yourdomain.com,www.yourdomain.com",
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 1000,
      "MaxConcurrentUpgradedConnections": 1000,
      "MaxRequestBodySize": 10485760,
      "KeepAliveTimeout": "00:02:00",
      "RequestHeadersTimeout": "00:00:30"
    }
  },
  "HealthChecks": {
    "UI": {
      "Enabled": true,
      "Path": "/health-ui"
    }
  },
  "RateLimiting": {
    "EnableRateLimiting": true,
    "GlobalPolicy": {
      "PermitLimit": 1000,
      "Window": "00:01:00"
    }
  }
}
```

## Database Setup

### PostgreSQL Production Setup

#### 1. Create Production Databases
```sql
-- Connect as superuser
CREATE DATABASE "FlightBookingDb_Production";
CREATE DATABASE "FlightBookingHangfire_Production";

-- Create application user
CREATE USER flightbooking_app WITH PASSWORD 'your-secure-password';

-- Grant permissions
GRANT CONNECT ON DATABASE "FlightBookingDb_Production" TO flightbooking_app;
GRANT CONNECT ON DATABASE "FlightBookingHangfire_Production" TO flightbooking_app;

-- Connect to main database and grant schema permissions
\c "FlightBookingDb_Production"
GRANT USAGE ON SCHEMA public TO flightbooking_app;
GRANT CREATE ON SCHEMA public TO flightbooking_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO flightbooking_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO flightbooking_app;

-- Set default privileges for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO flightbooking_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO flightbooking_app;

-- Repeat for Hangfire database
\c "FlightBookingHangfire_Production"
GRANT USAGE ON SCHEMA public TO flightbooking_app;
GRANT CREATE ON SCHEMA public TO flightbooking_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO flightbooking_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO flightbooking_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO flightbooking_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO flightbooking_app;
```

#### 2. PostgreSQL Configuration Optimization
```ini
# postgresql.conf for production
shared_buffers = 2GB                       # 25% of total RAM
effective_cache_size = 6GB                 # 75% of total RAM
work_mem = 16MB                            # Per connection work memory
maintenance_work_mem = 512MB               # For maintenance operations
checkpoint_completion_target = 0.9        # Spread checkpoints
wal_buffers = 64MB                         # WAL buffer size
default_statistics_target = 100           # Statistics target
random_page_cost = 1.1                    # For SSD storage
effective_io_concurrency = 200            # For SSD storage
max_connections = 200                      # Adjust based on load
shared_preload_libraries = 'pg_stat_statements'

# Enable query performance monitoring
pg_stat_statements.max = 10000
pg_stat_statements.track = all
```

### Redis Production Setup

#### Redis Configuration
```ini
# redis.conf for production
maxmemory 2gb
maxmemory-policy allkeys-lru
save 900 1
save 300 10
save 60 10000
appendonly yes
appendfsync everysec
tcp-keepalive 300
timeout 0
tcp-backlog 511
databases 16
```

## Application Deployment

### Docker Deployment (Recommended)

#### 1. Create Production Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Api/FlightBooking.Api/FlightBooking.Api.csproj", "src/Api/FlightBooking.Api/"]
COPY ["src/Application/FlightBooking.Application/FlightBooking.Application.csproj", "src/Application/FlightBooking.Application/"]
COPY ["src/Infrastructure/FlightBooking.Infrastructure/FlightBooking.Infrastructure.csproj", "src/Infrastructure/FlightBooking.Infrastructure/"]
COPY ["src/Domain/FlightBooking.Domain/FlightBooking.Domain.csproj", "src/Domain/FlightBooking.Domain/"]
COPY ["src/Contracts/FlightBooking.Contracts/FlightBooking.Contracts.csproj", "src/Contracts/FlightBooking.Contracts/"]

RUN dotnet restore "src/Api/FlightBooking.Api/FlightBooking.Api.csproj"
COPY . .
WORKDIR "/src/src/Api/FlightBooking.Api"
RUN dotnet build "FlightBooking.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlightBooking.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "FlightBooking.Api.dll"]
```

#### 2. Docker Compose for Production
```yaml
version: '3.8'

services:
  flightbooking-api:
    build: .
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - CONNECTIONSTRINGS__DEFAULTCONNECTION=${DB_CONNECTION_STRING}
      - CONNECTIONSTRINGS__HANGFIRE=${HANGFIRE_CONNECTION_STRING}
      - CONNECTIONSTRINGS__REDIS=${REDIS_CONNECTION_STRING}
      - JWT__SECRETKEY=${JWT_SECRET_KEY}
      - EMAILSETTINGS__SENDGRID__APIKEY=${SENDGRID_API_KEY}
    depends_on:
      - postgres
      - redis
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  flightbooking-worker:
    build: .
    command: ["dotnet", "FlightBooking.Workers.dll"]
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - CONNECTIONSTRINGS__DEFAULTCONNECTION=${DB_CONNECTION_STRING}
      - CONNECTIONSTRINGS__HANGFIRE=${HANGFIRE_CONNECTION_STRING}
      - CONNECTIONSTRINGS__REDIS=${REDIS_CONNECTION_STRING}
    depends_on:
      - postgres
      - redis
    restart: unless-stopped

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - flightbooking-api
    restart: unless-stopped
```

### Nginx Configuration

```nginx
events {
    worker_connections 1024;
}

http {
    upstream flightbooking_api {
        server flightbooking-api:5000;
    }

    # Rate limiting
    limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;
    limit_req_zone $binary_remote_addr zone=auth:10m rate=5r/s;

    server {
        listen 80;
        server_name yourdomain.com www.yourdomain.com;
        return 301 https://$server_name$request_uri;
    }

    server {
        listen 443 ssl http2;
        server_name yourdomain.com www.yourdomain.com;

        ssl_certificate /etc/nginx/ssl/cert.pem;
        ssl_certificate_key /etc/nginx/ssl/key.pem;
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384:DHE-RSA-AES256-GCM-SHA384;
        ssl_prefer_server_ciphers off;

        # Security headers
        add_header X-Frame-Options DENY;
        add_header X-Content-Type-Options nosniff;
        add_header X-XSS-Protection "1; mode=block";
        add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload";

        # API endpoints
        location /api/ {
            limit_req zone=api burst=20 nodelay;
            proxy_pass http://flightbooking_api;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_connect_timeout 30s;
            proxy_send_timeout 30s;
            proxy_read_timeout 30s;
        }

        # Auth endpoints with stricter rate limiting
        location /api/auth/ {
            limit_req zone=auth burst=10 nodelay;
            proxy_pass http://flightbooking_api;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        # Health check endpoint
        location /health {
            proxy_pass http://flightbooking_api;
            access_log off;
        }

        # Admin endpoints (restrict access)
        location /admin/ {
            allow 10.0.0.0/8;
            allow 172.16.0.0/12;
            allow 192.168.0.0/16;
            deny all;
            proxy_pass http://flightbooking_api;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }
    }
}
```

## Deployment Steps

### 1. Pre-deployment Checklist

- [ ] Environment variables configured
- [ ] Database servers provisioned and configured
- [ ] Redis cache provisioned and configured
- [ ] SSL certificates obtained and configured
- [ ] DNS records configured
- [ ] Monitoring and logging setup
- [ ] Backup strategy implemented
- [ ] Security groups/firewall rules configured

### 2. Database Migration

```bash
# Run migrations on production database
dotnet ef database update --project src/Infrastructure/FlightBooking.Infrastructure --startup-project src/Api/FlightBooking.Api --configuration Release

# Verify migration success
dotnet run --project src/Api/FlightBooking.Api --configuration Release --urls "http://localhost:5000" &
curl http://localhost:5000/health
```

### 3. Application Deployment

```bash
# Build and deploy with Docker
docker build -t flightbooking-api:latest .
docker tag flightbooking-api:latest your-registry/flightbooking-api:latest
docker push your-registry/flightbooking-api:latest

# Deploy to production
docker-compose -f docker-compose.prod.yml up -d

# Verify deployment
curl https://yourdomain.com/health
```

### 4. Post-deployment Verification

```bash
# Health checks
curl https://yourdomain.com/health
curl https://yourdomain.com/api/health

# API functionality test
curl -X POST https://yourdomain.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","firstName":"Test","lastName":"User","password":"TestPassword123!","confirmPassword":"TestPassword123!"}'

# Database connectivity
curl https://yourdomain.com/api/health/database

# Redis connectivity
curl https://yourdomain.com/api/health/redis
```

## Monitoring & Maintenance

### Application Monitoring

#### Health Checks
The system includes comprehensive health checks:
- Database connectivity
- Redis connectivity
- Disk space
- Memory usage
- Response time

#### Logging
- **Application Logs**: Structured logging with Serilog
- **Access Logs**: Nginx access logs
- **Error Logs**: Application and system error logs
- **Audit Logs**: Complete audit trail in database

#### Metrics Collection
- **Performance Metrics**: Response times, throughput
- **Business Metrics**: Bookings, revenue, user activity
- **System Metrics**: CPU, memory, disk usage
- **Database Metrics**: Query performance, connection pool

### Backup Strategy

#### Database Backups
```bash
# Daily full backup
pg_dump -h your-postgres-host -U flightbooking_app -d FlightBookingDb_Production | gzip > /backups/daily/flightbooking_$(date +%Y%m%d).sql.gz

# Weekly backup with retention
find /backups/weekly -name "*.sql.gz" -mtime +28 -delete
pg_dump -h your-postgres-host -U flightbooking_app -d FlightBookingDb_Production | gzip > /backups/weekly/flightbooking_$(date +%Y%m%d).sql.gz
```

#### Application Backups
- Configuration files
- SSL certificates
- Application logs
- Custom scripts

### Security Considerations

#### Network Security
- Use VPC/Virtual Networks
- Configure security groups/NSGs
- Enable DDoS protection
- Use Web Application Firewall (WAF)

#### Application Security
- Keep dependencies updated
- Regular security scans
- Implement proper authentication
- Use HTTPS everywhere
- Secure API endpoints

#### Database Security
- Use managed database services
- Enable encryption at rest
- Use SSL/TLS for connections
- Regular security updates
- Implement proper access controls

### Scaling Considerations

#### Horizontal Scaling
- Load balancer configuration
- Multiple application instances
- Database read replicas
- Redis clustering

#### Vertical Scaling
- Monitor resource usage
- Scale up when needed
- Optimize database queries
- Implement caching strategies

## Troubleshooting Common Issues

### Database Connection Issues
```bash
# Test database connectivity
psql -h your-postgres-host -U flightbooking_app -d FlightBookingDb_Production -c "SELECT 1;"

# Check connection pool status
SELECT * FROM pg_stat_activity WHERE datname = 'FlightBookingDb_Production';

# Verify SSL connection
SELECT ssl_is_used();
```

### Redis Connection Issues
```bash
# Test Redis connectivity
redis-cli -h your-redis-host ping

# Check Redis memory usage
redis-cli -h your-redis-host info memory

# Monitor Redis connections
redis-cli -h your-redis-host info clients
```

### Application Performance Issues
```bash
# Check application logs
docker logs flightbooking-api --tail 100

# Monitor resource usage
docker stats flightbooking-api

# Check health endpoints
curl https://yourdomain.com/health
curl https://yourdomain.com/health/detailed
```

## Disaster Recovery

### Backup Verification
```bash
# Test backup restoration
pg_restore --dry-run backup_file.sql

# Verify backup integrity
pg_dump --schema-only FlightBookingDb_Production | diff - schema_backup.sql
```

### Recovery Procedures
1. **Database Recovery**
   - Stop application services
   - Restore database from backup
   - Verify data integrity
   - Restart services

2. **Application Recovery**
   - Deploy previous stable version
   - Verify configuration
   - Test critical functionality
   - Monitor for issues

### High Availability Setup
- **Database Clustering**: PostgreSQL streaming replication
- **Load Balancing**: Multiple application instances
- **Failover Procedures**: Automated failover configuration
- **Monitoring**: 24/7 monitoring and alerting

This deployment guide provides a comprehensive approach to deploying the Flight Booking Management System in production environments with proper security, monitoring, scalability, and disaster recovery considerations.
