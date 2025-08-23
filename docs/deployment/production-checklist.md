# BARQ Production Deployment Checklist

## Pre-Deployment Verification

### 1. Code Quality & Security
- [ ] All CI checks pass without `|| true` fallbacks
- [ ] No hardcoded secrets in codebase (verified with gitleaks)
- [ ] All placeholders and mock data removed
- [ ] Build warnings resolved (< 5 warnings acceptable)
- [ ] Security scan passes (no high/critical vulnerabilities)

### 2. Database Readiness
- [ ] All EF migrations tested on clean database
- [ ] Database indexes optimized for production load
- [ ] Backup and restore procedures tested
- [ ] Connection strings use environment variables
- [ ] Database user permissions follow least privilege

### 3. Application Configuration
- [ ] JWT signing keys properly configured
- [ ] CORS settings restricted to production domains
- [ ] Logging levels appropriate for production
- [ ] Health check endpoints responding
- [ ] Feature flags configured correctly

### 4. Infrastructure Preparation
- [ ] Production environment provisioned
- [ ] SSL certificates installed and valid
- [ ] Load balancer configured
- [ ] CDN configured for static assets
- [ ] Monitoring and alerting set up

### 5. Testing Validation
- [ ] All unit tests pass
- [ ] Integration tests pass
- [ ] E2E test journeys complete successfully:
  - [ ] Request → Approval → AI → QA → Complete
  - [ ] SLA Breach → Escalation
  - [ ] File Attach → AV Scan → Notify
  - [ ] Billing Cap → 402 → Upgrade Flow
- [ ] Performance tests meet SLA requirements
- [ ] Security penetration testing completed

## Deployment Process

### 1. Pre-Deployment Steps
```bash
# Verify production readiness
./scripts/verify-production-readiness.sh

# Create deployment tag
git tag -a v1.0.0 -m "BARQ v1.0.0 Production Release"
git push origin v1.0.0

# Build production images
docker build -t barq-api:v1.0.0 -f Backend/Dockerfile .
docker build -t barq-frontend:v1.0.0 -f Frontend/barq-frontend/Dockerfile Frontend/barq-frontend
```

### 2. Database Migration
```bash
# Backup current production database
az sql db export --name barq-prod --server barq-prod-server --admin-user $DB_ADMIN --admin-password $DB_PASSWORD --storage-key $STORAGE_KEY --storage-key-type StorageAccessKey --storage-uri "https://barqbackups.blob.core.windows.net/backups/pre-v1.0.0-$(date +%Y%m%d-%H%M%S).bacpac"

# Apply migrations
dotnet ef database update --project Backend/src/BARQ.Infrastructure --startup-project Backend/src/BARQ.API --connection-string "$PROD_CONNECTION_STRING"
```

### 3. Application Deployment
```bash
# Deploy to staging first
kubectl apply -f k8s/staging/

# Verify staging deployment
kubectl get pods -n barq-staging
curl https://staging-api.barq.platform/health/ready

# Run smoke tests against staging
cd Frontend/barq-frontend
PLAYWRIGHT_BASE_URL=https://staging.barq.platform npx playwright test tests/wiring.smoke.spec.ts

# Deploy to production
kubectl apply -f k8s/production/

# Verify production deployment
kubectl get pods -n barq-production
curl https://api.barq.platform/health/ready
```

### 4. Post-Deployment Verification
```bash
# Health check all endpoints
curl https://api.barq.platform/health/live
curl https://api.barq.platform/health/ready
curl https://api.barq.platform/health/flowable
curl https://api.barq.platform/health/ai

# Verify key functionality
curl -X POST https://api.barq.platform/auth/login -H "Content-Type: application/json" -d '{"email":"admin@barq.platform","password":"$ADMIN_PASSWORD"}'

# Check metrics and logs
kubectl logs deployment/barq-api -n barq-production --tail=100
```

## Rollback Procedures

### 1. Application Rollback
```bash
# Rollback Kubernetes deployment
kubectl rollout undo deployment/barq-api -n barq-production
kubectl rollout undo deployment/barq-frontend -n barq-production

# Verify rollback
kubectl rollout status deployment/barq-api -n barq-production
```

### 2. Database Rollback
```bash
# Restore from backup (if needed)
az sql db import --name barq-prod --server barq-prod-server --admin-user $DB_ADMIN --admin-password $DB_PASSWORD --storage-key $STORAGE_KEY --storage-key-type StorageAccessKey --storage-uri "https://barqbackups.blob.core.windows.net/backups/pre-v1.0.0-backup.bacpac"
```

## Monitoring & Alerting

### 1. Key Metrics to Monitor
- [ ] Application response time (< 500ms p95)
- [ ] Error rate (< 1%)
- [ ] Database connection pool utilization
- [ ] Memory and CPU usage
- [ ] Disk space utilization
- [ ] SSL certificate expiration

### 2. Critical Alerts
- [ ] Application down/unhealthy
- [ ] Database connection failures
- [ ] High error rates
- [ ] Performance degradation
- [ ] Security events
- [ ] Quota violations

## Post-Deployment Tasks

### 1. Documentation Updates
- [ ] Update deployment documentation
- [ ] Record deployment notes and issues
- [ ] Update runbooks with any new procedures
- [ ] Notify stakeholders of successful deployment

### 2. Monitoring Setup
- [ ] Verify all alerts are working
- [ ] Check dashboard displays
- [ ] Test incident response procedures
- [ ] Schedule first post-deployment review

## Emergency Contacts

- **On-call Engineer**: +1-555-0123
- **Database Administrator**: dba@company.com
- **Security Team**: security@company.com
- **DevOps Team**: devops@company.com

## Sign-off

- [ ] **Development Lead**: _________________ Date: _______
- [ ] **QA Lead**: _________________ Date: _______
- [ ] **Security Lead**: _________________ Date: _______
- [ ] **Operations Lead**: _________________ Date: _______
- [ ] **Product Owner**: _________________ Date: _______

---

**Deployment Date**: _______________
**Deployed Version**: v1.0.0
**Deployment Lead**: _______________
**Rollback Tested**: [ ] Yes [ ] No
