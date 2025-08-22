# Rolling Deployment Runbook

## Overview
This runbook provides procedures for performing rolling deployments of the BARQ platform with zero downtime.

## Pre-Deployment Checklist

### Code Quality Gates
- [ ] All tests passing (unit, integration, E2E)
- [ ] Code review approved
- [ ] Security scan passed (Gitleaks, CodeQL)
- [ ] Performance tests passed
- [ ] Database migrations tested

### Infrastructure Readiness
- [ ] Staging environment validated
- [ ] Database backup completed
- [ ] Monitoring systems operational
- [ ] Rollback plan prepared
- [ ] Team notification sent

## Deployment Procedure

### Phase 1: Pre-Deployment (T-30 minutes)

#### 1. Backup Current State
```bash
# Backup database
sqlcmd -S production-sql -Q "BACKUP DATABASE BARQ_DB TO DISK = '/backups/BARQ_DB_$(date +%Y%m%d_%H%M%S).bak'"

# Tag current release
git tag -a v$(date +%Y.%m.%d.%H%M) -m "Pre-deployment backup"
git push origin --tags

# Export current configuration
kubectl get configmap barq-config -o yaml > backup/configmap-$(date +%Y%m%d_%H%M%S).yaml
kubectl get secret barq-secrets -o yaml > backup/secrets-$(date +%Y%m%d_%H%M%S).yaml
```

#### 2. Validate Staging Environment
```bash
# Run smoke tests on staging
./scripts/staging-smoke-test.sh

# Verify database migrations
kubectl exec -it staging-api -- dotnet ef database update --dry-run

# Test critical user journeys
./scripts/e2e-critical-paths.sh --env=staging
```

### Phase 2: Database Migration (T-15 minutes)

#### 1. Apply Database Changes
```bash
# Connect to production database
kubectl port-forward svc/sql-server 1433:1433 &

# Apply migrations with backup
dotnet ef database update --project src/BARQ.Infrastructure --startup-project src/BARQ.API \
  --connection "Server=localhost,1433;Database=BARQ_DB;User Id=sa;Password=$DB_PASSWORD;TrustServerCertificate=true"

# Verify migration success
dotnet ef migrations list --project src/BARQ.Infrastructure --startup-project src/BARQ.API
```

#### 2. Validate Database State
```bash
# Check database integrity
sqlcmd -S localhost,1433 -d BARQ_DB -Q "DBCC CHECKDB('BARQ_DB') WITH NO_INFOMSGS"

# Verify critical data
sqlcmd -S localhost,1433 -d BARQ_DB -Q "SELECT COUNT(*) FROM Tenants; SELECT COUNT(*) FROM Users;"

# Test database connectivity
./scripts/db-connectivity-test.sh
```

### Phase 3: Application Deployment (T-0)

#### 1. Rolling Update Backend
```bash
# Update container image
kubectl set image deployment/barq-api barq-api=barq/api:v2.1.0

# Monitor rollout
kubectl rollout status deployment/barq-api --timeout=600s

# Verify new pods are healthy
kubectl get pods -l app=barq-api
kubectl logs -l app=barq-api --tail=50
```

#### 2. Health Check Validation
```bash
# Wait for health checks to pass
for i in {1..30}; do
  if curl -f https://api.barq.com/health/ready; then
    echo "Health check passed"
    break
  fi
  echo "Waiting for health check... ($i/30)"
  sleep 10
done

# Verify all endpoints
./scripts/endpoint-health-check.sh
```

#### 3. Frontend Deployment
```bash
# Build and deploy frontend
cd Frontend/barq-frontend
pnpm build

# Upload to CDN
aws s3 sync dist/ s3://barq-frontend-prod --delete

# Invalidate CDN cache
aws cloudfront create-invalidation --distribution-id E1234567890 --paths "/*"
```

### Phase 4: Verification (T+15 minutes)

#### 1. Functional Testing
```bash
# Run critical path tests
./scripts/production-smoke-test.sh

# Test authentication flows
./scripts/test-auth-production.sh

# Verify AI integrations
./scripts/test-ai-providers.sh

# Test BPM workflows
./scripts/test-flowable-integration.sh
```

#### 2. Performance Validation
```bash
# Check response times
./scripts/performance-baseline-check.sh

# Monitor error rates
kubectl logs -l app=barq-api | grep -i error | tail -20

# Verify database performance
sqlcmd -S production-sql -Q "SELECT * FROM sys.dm_exec_query_stats ORDER BY total_elapsed_time DESC"
```

#### 3. User Acceptance Testing
```bash
# Test critical user journeys
./scripts/e2e-production-validation.sh

# Verify tenant isolation
./scripts/test-tenant-isolation.sh

# Test file upload/download
./scripts/test-file-operations.sh
```

## Rollback Procedures

### Immediate Rollback (Critical Issues)
```bash
# Rollback application deployment
kubectl rollout undo deployment/barq-api

# Verify rollback success
kubectl rollout status deployment/barq-api

# Check application health
curl https://api.barq.com/health/ready
```

### Database Rollback (If Required)
```bash
# Stop application traffic
kubectl scale deployment/barq-api --replicas=0

# Restore database from backup
sqlcmd -S production-sql -Q "RESTORE DATABASE BARQ_DB FROM DISK = '/backups/BARQ_DB_20240101_120000.bak' WITH REPLACE"

# Restart application with previous version
kubectl set image deployment/barq-api barq-api=barq/api:v2.0.0
kubectl scale deployment/barq-api --replicas=3
```

### Frontend Rollback
```bash
# Revert to previous frontend version
aws s3 sync s3://barq-frontend-backup/v2.0.0/ s3://barq-frontend-prod --delete

# Invalidate CDN cache
aws cloudfront create-invalidation --distribution-id E1234567890 --paths "/*"
```

## Post-Deployment Activities

### Immediate (T+30 minutes)
1. **Monitor system health**
   ```bash
   # Check error rates
   kubectl logs -l app=barq-api | grep -i error | wc -l
   
   # Monitor response times
   curl -w "@curl-format.txt" -s -o /dev/null https://api.barq.com/health/ready
   
   # Check database performance
   ./scripts/db-performance-check.sh
   ```

2. **Validate business metrics**
   ```bash
   # Check user activity
   ./scripts/user-activity-check.sh
   
   # Verify transaction processing
   ./scripts/transaction-health-check.sh
   
   # Monitor AI provider usage
   ./scripts/ai-usage-metrics.sh
   ```

### Follow-up (T+2 hours)
1. **Performance analysis**
2. **Error rate review**
3. **User feedback collection**
4. **Documentation updates**

### Long-term (T+24 hours)
1. **Deployment retrospective**
2. **Process improvements**
3. **Automation enhancements**
4. **Team feedback session**

## Monitoring and Alerting

### Key Metrics to Monitor
- **Response Time**: < 500ms for 95th percentile
- **Error Rate**: < 0.1% for critical endpoints
- **Database Performance**: Query time < 100ms average
- **Memory Usage**: < 80% of allocated resources
- **CPU Usage**: < 70% of allocated resources

### Alert Thresholds
- **Critical**: Error rate > 1%, Response time > 2s
- **Warning**: Error rate > 0.5%, Response time > 1s
- **Info**: Deployment started/completed

## Emergency Contacts

### Deployment Team
- **Lead Engineer**: +1-XXX-XXX-XXXX
- **DevOps Engineer**: +1-XXX-XXX-XXXX
- **Database Administrator**: +1-XXX-XXX-XXXX

### Escalation
- **Engineering Manager**: +1-XXX-XXX-XXXX
- **VP Engineering**: +1-XXX-XXX-XXXX
- **CTO**: +1-XXX-XXX-XXXX

## Tools and Scripts

### Deployment Scripts
- `./scripts/deploy.sh` - Main deployment script
- `./scripts/rollback.sh` - Emergency rollback script
- `./scripts/health-check.sh` - Comprehensive health check

### Monitoring Tools
- **Application Insights**: Real-time monitoring
- **Grafana**: Performance dashboards
- **PagerDuty**: Alert management
- **Slack**: Team notifications

## Success Criteria

### Deployment Success
- [ ] All health checks passing
- [ ] Error rate < 0.1%
- [ ] Response time within SLA
- [ ] All critical features functional
- [ ] No data loss or corruption
- [ ] User experience maintained

### Rollback Success
- [ ] System restored to previous state
- [ ] All functionality working
- [ ] Data integrity maintained
- [ ] Performance restored
- [ ] Users can access system normally
