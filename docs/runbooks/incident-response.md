# Incident Response Runbook

## Overview
This runbook provides step-by-step procedures for responding to incidents in the BARQ platform.

## Incident Classification

### Severity Levels
- **P0 (Critical)**: Complete service outage, data loss, security breach
- **P1 (High)**: Major functionality impaired, significant user impact
- **P2 (Medium)**: Minor functionality issues, limited user impact
- **P3 (Low)**: Cosmetic issues, no user impact

## Response Procedures

### P0 - Critical Incidents

#### Immediate Actions (0-15 minutes)
1. **Acknowledge the incident**
   ```bash
   # Check system health
   curl https://api.barq.com/health/live
   curl https://api.barq.com/health/ready
   ```

2. **Assess impact**
   - Check monitoring dashboards
   - Review error logs
   - Identify affected tenants/users

3. **Activate incident response team**
   - Notify on-call engineer
   - Create incident channel
   - Update status page

#### Investigation (15-60 minutes)
1. **Gather information**
   ```bash
   # Check application logs
   kubectl logs -f deployment/barq-api --tail=100
   
   # Check database connectivity
   sqlcmd -S server -d BARQ_DB -Q "SELECT 1"
   
   # Check Flowable BPM status
   curl https://flowable.barq.com/flowable-rest/service/management/engine
   ```

2. **Identify root cause**
   - Review recent deployments
   - Check infrastructure changes
   - Analyze error patterns

#### Resolution
1. **Implement fix**
   - Apply hotfix if available
   - Rollback if necessary
   - Scale resources if needed

2. **Verify resolution**
   ```bash
   # Run health checks
   ./scripts/health-check.sh
   
   # Test critical user journeys
   ./scripts/e2e-smoke-test.sh
   ```

### P1 - High Priority Incidents

#### Response Time: 1 hour
1. **Initial assessment** (0-15 minutes)
2. **Investigation** (15-45 minutes)
3. **Resolution** (45-60 minutes)

### Common Incident Scenarios

#### Database Connection Issues
```bash
# Check SQL Server status
systemctl status mssql-server

# Check connection pool
SELECT * FROM sys.dm_exec_connections WHERE session_id > 50

# Reset connection pool if needed
ALTER DATABASE BARQ_DB SET SINGLE_USER WITH ROLLBACK IMMEDIATE
ALTER DATABASE BARQ_DB SET MULTI_USER
```

#### High Memory Usage
```bash
# Check memory usage
kubectl top pods

# Check for memory leaks
dotnet-dump collect -p <process-id>
dotnet-dump analyze <dump-file>
```

#### Flowable BPM Issues
```bash
# Check Flowable logs
docker logs flowable-app

# Restart Flowable if needed
docker-compose restart flowable-app

# Verify BPM connectivity
curl -X GET "https://flowable.barq.com/flowable-rest/service/repository/deployments"
```

## Post-Incident Activities

### Immediate (0-24 hours)
1. **Document timeline**
2. **Notify stakeholders**
3. **Update status page**
4. **Create incident report**

### Follow-up (24-72 hours)
1. **Conduct post-mortem**
2. **Identify action items**
3. **Update runbooks**
4. **Implement preventive measures**

## Escalation Contacts

### Technical Escalation
- **Primary On-call**: +1-XXX-XXX-XXXX
- **Secondary On-call**: +1-XXX-XXX-XXXX
- **Engineering Manager**: +1-XXX-XXX-XXXX

### Business Escalation
- **Product Manager**: +1-XXX-XXX-XXXX
- **VP Engineering**: +1-XXX-XXX-XXXX

## Tools and Resources

### Monitoring
- **Application Monitoring**: Application Insights
- **Infrastructure Monitoring**: Azure Monitor
- **Log Aggregation**: Azure Log Analytics
- **Status Page**: status.barq.com

### Communication
- **Incident Channel**: #incident-response
- **Status Updates**: #status-updates
- **Engineering**: #engineering

## Recovery Procedures

### Database Recovery
```bash
# Restore from backup
RESTORE DATABASE BARQ_DB FROM DISK = 'backup_path'

# Verify data integrity
DBCC CHECKDB('BARQ_DB')
```

### Application Recovery
```bash
# Rolling restart
kubectl rollout restart deployment/barq-api

# Scale up if needed
kubectl scale deployment/barq-api --replicas=5
```

### Cache Recovery
```bash
# Clear Redis cache
redis-cli FLUSHALL

# Warm up cache
curl -X POST https://api.barq.com/admin/cache/warmup
```
