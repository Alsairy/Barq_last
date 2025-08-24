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

#### Preview Environment Deployment
```bash
# Deploy preview environment first for validation
gh workflow run deploy-preview.yml -f tag=v1.0.0

# Wait for preview deployment and E2E tests to pass
# Monitor workflow: gh run list --workflow=deploy-preview.yml

# Verify preview environment
curl https://api.barq-preview.tetco.sa/health/ready
curl https://barq-preview.tetco.sa/

# Run comprehensive E2E tests against preview
cd Frontend/barq-frontend
PLAYWRIGHT_BASE_URL=https://barq-preview.tetco.sa npx playwright test tests/journeys.e2e.spec.ts --reporter=html
```

#### Production Canary Deployment
```bash
# Phase 1: Deploy with 10% canary traffic
gh workflow run deploy-prod.yml -f tag=v1.0.0 -f canary_weight=10

# Monitor health endpoints for 15-30 minutes
curl https://api.barq.tetco.sa/health/ready
curl https://api.barq.tetco.sa/health/flowable
curl https://api.barq.tetco.sa/health/ai

# Check canary ingress status
kubectl get ingress barq-api-ingress-canary -n barq-production

# Phase 2: Increase to 50% if healthy (error rate < 1%, latency normal)
gh workflow run deploy-prod.yml -f tag=v1.0.0 -f canary_weight=50

# Monitor for 30-60 minutes
kubectl logs -f deployment/barq-api -n barq-production
kubectl top pods -n barq-production

# Phase 3: Complete rollout (removes canary ingress)
gh workflow run deploy-prod.yml -f tag=v1.0.0 -f canary_weight=100

# Verify canary ingress is removed
kubectl get ingress -n barq-production
```

#### Emergency Rollback Commands
```bash
# Emergency rollback to previous version
kubectl rollout undo deployment/barq-api -n barq-production
kubectl rollout undo deployment/barq-frontend -n barq-production

# Or rollback to specific revision
kubectl rollout history deployment/barq-api -n barq-production
kubectl rollout undo deployment/barq-api --to-revision=N -n barq-production

# Remove canary ingress to stop new version traffic
kubectl delete -f k8s/prod/api-ingress-canary.yaml
```

### 4. Post-Deployment Verification
```bash
# Health check all endpoints
curl https://api.barq.tetco.sa/health/live
curl https://api.barq.tetco.sa/health/ready
curl https://api.barq.tetco.sa/health/flowable
curl https://api.barq.tetco.sa/health/ai

# Verify key functionality
curl -X POST https://api.barq.tetco.sa/auth/login -H "Content-Type: application/json" -d '{"email":"admin@barq.tetco.sa","password":"$ADMIN_PASSWORD"}'

# Run production smoke tests
cd Frontend/barq-frontend
PLAYWRIGHT_BASE_URL=https://barq.tetco.sa npx playwright test tests/wiring.smoke.spec.ts --reporter=line

# Check metrics and logs
kubectl logs deployment/barq-api -n barq-production --tail=100

# Verify DNS and TLS
nslookup barq.tetco.sa
nslookup api.barq.tetco.sa
openssl s_client -connect api.barq.tetco.sa:443 -servername api.barq.tetco.sa < /dev/null
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
