# BARQ Deployment Guide

## Overview
This guide covers deployment strategies for the BARQ enterprise AI orchestration platform across different environments.

## Deployment Architecture

### Production Environment
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Load Balancer │    │   Web Servers   │    │   API Servers   │
│   (Azure LB)    │────│   (Frontend)    │────│   (.NET Core)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                                        │
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   File Storage  │    │   Cache Layer   │    │   Database      │
│   (Azure Blob)  │    │   (Redis)       │    │   (SQL Server)  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                                        │
                       ┌─────────────────┐    ┌─────────────────┐
                       │   BPM Engine    │    │   Monitoring    │
                       │   (Flowable)    │    │   (App Insights)│
                       └─────────────────┘    └─────────────────┘
```

## Prerequisites

### Infrastructure Requirements
- **Compute**: 4+ CPU cores, 16GB+ RAM per API server
- **Database**: SQL Server 2019+ with 100GB+ storage
- **Cache**: Redis 6.0+ with 8GB+ memory
- **Storage**: Azure Blob Storage or S3-compatible storage
- **Network**: HTTPS/TLS 1.2+, CDN for static assets

### Software Requirements
- **.NET 8 Runtime**
- **Node.js 18+ with pnpm**
- **Docker & Docker Compose**
- **Kubernetes** (for container orchestration)
- **Nginx** (reverse proxy)

## Environment Configuration

### Environment Variables
```bash
# Database
DB_CONNECTION_STRING="Server=sql-server;Database=BARQ_DB;User Id=barq_user;Password=secure_password;TrustServerCertificate=true"

# Redis
REDIS_CONNECTION_STRING="localhost:6379"

# Authentication
JWT_SECRET_KEY="your-jwt-secret-key"
JWT_ISSUER="https://api.barq.com"
JWT_AUDIENCE="barq-users"

# External Services
OPENAI_API_KEY="sk-your-openai-key"
AZURE_OPENAI_ENDPOINT="https://your-openai.openai.azure.com/"
AZURE_OPENAI_API_KEY="your-azure-openai-key"

# File Storage
AZURE_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=barqstorage;AccountKey=..."

# Flowable BPM
FLOWABLE_BASE_URL="http://flowable:8080"
FLOWABLE_USERNAME="admin"
FLOWABLE_PASSWORD="test"

# Monitoring
APPLICATION_INSIGHTS_CONNECTION_STRING="InstrumentationKey=..."

# CORS
CORS_ALLOWED_ORIGINS="https://app.barq.com,https://staging.barq.com"

# Feature Flags
FEATURE_FLAGS_ENABLED="true"
COOKIE_AUTH_ENABLED="true"
CSRF_ENABLED="true"
```

## Docker Deployment

### Docker Compose (Development)
```yaml
version: '3.8'

services:
  api:
    build:
      context: ./Backend
      dockerfile: Dockerfile
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - DB_CONNECTION_STRING=Server=sql-server;Database=BARQ_DB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true
    depends_on:
      - sql-server
      - redis
      - flowable

  frontend:
    build:
      context: ./Frontend/barq-frontend
      dockerfile: Dockerfile
    ports:
      - "3000:80"
    environment:
      - REACT_APP_API_URL=http://localhost:5000

  sql-server:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
    ports:
      - "1433:1433"
    volumes:
      - sql_data:/var/opt/mssql

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

  flowable:
    image: flowable/flowable-rest:6.8.0
    ports:
      - "8080:8080"
    environment:
      - FLOWABLE_DATABASE_TYPE=mssql
      - FLOWABLE_DATABASE_URL=jdbc:sqlserver://sql-server:1433;databaseName=flowable
      - FLOWABLE_DATABASE_USERNAME=sa
      - FLOWABLE_DATABASE_PASSWORD=YourStrong@Passw0rd
    depends_on:
      - sql-server

volumes:
  sql_data:
  redis_data:
```

### Production Docker Compose
```yaml
version: '3.8'

services:
  api:
    image: barq/api:latest
    deploy:
      replicas: 3
      resources:
        limits:
          memory: 2G
          cpus: '1.0'
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DB_CONNECTION_STRING=${DB_CONNECTION_STRING}
      - REDIS_CONNECTION_STRING=${REDIS_CONNECTION_STRING}
    networks:
      - barq-network

  frontend:
    image: barq/frontend:latest
    deploy:
      replicas: 2
    networks:
      - barq-network

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    networks:
      - barq-network

networks:
  barq-network:
    driver: overlay
```

## Kubernetes Deployment

### Namespace
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: barq-production
```

### ConfigMap
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: barq-config
  namespace: barq-production
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  JWT_ISSUER: "https://api.barq.com"
  JWT_AUDIENCE: "barq-users"
  CORS_ALLOWED_ORIGINS: "https://app.barq.com"
```

### Secret
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: barq-secrets
  namespace: barq-production
type: Opaque
data:
  DB_CONNECTION_STRING: <base64-encoded-connection-string>
  JWT_SECRET_KEY: <base64-encoded-jwt-secret>
  OPENAI_API_KEY: <base64-encoded-openai-key>
```

### API Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: barq-api
  namespace: barq-production
spec:
  replicas: 3
  selector:
    matchLabels:
      app: barq-api
  template:
    metadata:
      labels:
        app: barq-api
    spec:
      containers:
      - name: api
        image: barq/api:latest
        ports:
        - containerPort: 80
        envFrom:
        - configMapRef:
            name: barq-config
        - secretRef:
            name: barq-secrets
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
```

### Service
```yaml
apiVersion: v1
kind: Service
metadata:
  name: barq-api-service
  namespace: barq-production
spec:
  selector:
    app: barq-api
  ports:
  - port: 80
    targetPort: 80
  type: ClusterIP
```

### Ingress
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: barq-ingress
  namespace: barq-production
  annotations:
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/use-regex: "true"
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
spec:
  tls:
  - hosts:
    - api.barq.com
    - app.barq.com
    secretName: barq-tls
  rules:
  - host: api.barq.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: barq-api-service
            port:
              number: 80
  - host: app.barq.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: barq-frontend-service
            port:
              number: 80
```

## Database Deployment

### SQL Server Setup
```sql
-- Create database
CREATE DATABASE BARQ_DB;
GO

-- Create application user
USE BARQ_DB;
CREATE LOGIN barq_app WITH PASSWORD = 'SecurePassword123!';
CREATE USER barq_app FOR LOGIN barq_app;

-- Grant permissions
ALTER ROLE db_datareader ADD MEMBER barq_app;
ALTER ROLE db_datawriter ADD MEMBER barq_app;
ALTER ROLE db_ddladmin ADD MEMBER barq_app;
GO
```

### Database Migration
```bash
# Apply migrations
dotnet ef database update --project Backend/src/BARQ.Infrastructure --startup-project Backend/src/BARQ.API

# Seed initial data
dotnet run --project Backend/src/BARQ.API -- --seed-data
```

## Monitoring Setup

### Application Insights
```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...",
    "EnableAdaptiveSampling": true,
    "EnablePerformanceCounterCollectionModule": true,
    "EnableQuickPulseMetricStream": true
  }
}
```

### Health Checks
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString)
    .AddRedis(redisConnectionString)
    .AddUrlGroup(new Uri($"{flowableBaseUrl}/flowable-rest/service/management/engine"), "flowable");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

## Security Configuration

### SSL/TLS Setup
```nginx
server {
    listen 443 ssl http2;
    server_name api.barq.com;

    ssl_certificate /etc/nginx/ssl/barq.crt;
    ssl_certificate_key /etc/nginx/ssl/barq.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512;

    location / {
        proxy_pass http://barq-api;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Firewall Rules
```bash
# Allow HTTPS traffic
ufw allow 443/tcp

# Allow HTTP (redirect to HTTPS)
ufw allow 80/tcp

# Allow SSH (restrict to specific IPs)
ufw allow from 192.168.1.0/24 to any port 22

# Deny all other traffic
ufw default deny incoming
ufw default allow outgoing
```

## Backup Strategy

### Database Backup
```sql
-- Full backup (daily)
BACKUP DATABASE BARQ_DB 
TO DISK = '/backups/BARQ_DB_FULL_$(date +%Y%m%d).bak'
WITH COMPRESSION, CHECKSUM;

-- Differential backup (hourly)
BACKUP DATABASE BARQ_DB 
TO DISK = '/backups/BARQ_DB_DIFF_$(date +%Y%m%d_%H).bak'
WITH DIFFERENTIAL, COMPRESSION, CHECKSUM;

-- Transaction log backup (every 15 minutes)
BACKUP LOG BARQ_DB 
TO DISK = '/backups/BARQ_DB_LOG_$(date +%Y%m%d_%H%M).trn'
WITH COMPRESSION, CHECKSUM;
```

### File Storage Backup
```bash
# Azure Blob Storage backup
az storage blob sync \
  --source /app/uploads \
  --destination https://barqbackup.blob.core.windows.net/backups \
  --account-name barqbackup \
  --account-key $BACKUP_STORAGE_KEY
```

## Scaling Strategy

### Horizontal Scaling
```bash
# Scale API pods
kubectl scale deployment barq-api --replicas=5

# Scale frontend pods
kubectl scale deployment barq-frontend --replicas=3
```

### Auto-scaling
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: barq-api-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: barq-api
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

## Troubleshooting

### Common Issues

#### Database Connection Issues
```bash
# Test database connectivity
sqlcmd -S sql-server -U barq_app -P password -Q "SELECT 1"

# Check connection pool
kubectl logs deployment/barq-api | grep "connection"
```

#### High Memory Usage
```bash
# Check memory usage
kubectl top pods -n barq-production

# Analyze memory dumps
dotnet-dump collect -p <process-id>
dotnet-dump analyze <dump-file>
```

#### SSL Certificate Issues
```bash
# Check certificate expiration
openssl x509 -in /etc/nginx/ssl/barq.crt -text -noout | grep "Not After"

# Renew Let's Encrypt certificate
certbot renew --nginx
```

## Deployment Checklist

### Pre-Deployment
- [ ] Code review completed
- [ ] All tests passing
- [ ] Security scan passed
- [ ] Database backup completed
- [ ] Staging environment validated

### Deployment
- [ ] Database migrations applied
- [ ] Application deployed
- [ ] Health checks passing
- [ ] SSL certificates valid
- [ ] Monitoring configured

### Post-Deployment
- [ ] Smoke tests passed
- [ ] Performance metrics normal
- [ ] Error rates acceptable
- [ ] User acceptance testing completed
- [ ] Documentation updated

## Support

For deployment support:
- **DevOps Team**: devops@barq.com
- **Emergency**: +1-XXX-XXX-XXXX
- **Documentation**: [Internal Wiki](https://wiki.barq.com/deployment)
