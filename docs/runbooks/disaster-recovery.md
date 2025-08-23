# BARQ Disaster Recovery Runbook

## Overview
This runbook provides step-by-step procedures for recovering BARQ platform services in the event of various disaster scenarios.

## Recovery Time Objectives (RTO)
- **Critical Services**: 4 hours
- **Non-Critical Services**: 24 hours
- **Data Recovery Point Objective (RPO)**: 1 hour

## Disaster Scenarios

### 1. Database Failure

#### Symptoms
- Database connection errors
- 500 errors from API endpoints
- Health checks failing for `/health/ready`

#### Recovery Steps
1. **Assess Damage**
   ```bash
   # Check database connectivity
   sqlcmd -S <server> -U <user> -P <password> -Q "SELECT 1"
   
   # Check backup status
   SELECT TOP 5 * FROM msdb.dbo.backupset 
   WHERE database_name = 'BarqProd' 
   ORDER BY backup_start_date DESC
   ```

2. **Restore from Backup**
   ```bash
   # Stop API services
   kubectl scale deployment barq-api --replicas=0
   
   # Restore database
   RESTORE DATABASE BarqProd FROM DISK = '/backups/BarqProd_latest.bak'
   WITH REPLACE, RECOVERY
   
   # Verify data integrity
   DBCC CHECKDB('BarqProd')
   ```

3. **Restart Services**
   ```bash
   # Scale API back up
   kubectl scale deployment barq-api --replicas=3
   
   # Verify health
   curl -f https://api.barq.ai/health/ready
   ```

### 2. API Service Outage

#### Symptoms
- API endpoints returning 503/504 errors
- Frontend unable to load data
- High error rates in monitoring

#### Recovery Steps
1. **Check Service Status**
   ```bash
   kubectl get pods -l app=barq-api
   kubectl logs -l app=barq-api --tail=100
   ```

2. **Rolling Restart**
   ```bash
   kubectl rollout restart deployment/barq-api
   kubectl rollout status deployment/barq-api
   ```

3. **Scale if Needed**
   ```bash
   kubectl scale deployment barq-api --replicas=5
   ```

### 3. Complete Infrastructure Failure

#### Recovery Steps
1. **Activate DR Environment**
   ```bash
   # Switch DNS to DR region
   aws route53 change-resource-record-sets --hosted-zone-id Z123 \
     --change-batch file://dr-dns-change.json
   
   # Deploy to DR cluster
   kubectl config use-context barq-dr-cluster
   helm upgrade barq ./helm/barq --values values-dr.yaml
   ```

2. **Restore Data**
   ```bash
   # Restore from cross-region backup
   az sql db restore --dest-name BarqProd-DR \
     --source-database BarqProd \
     --time "2024-01-01T12:00:00Z"
   ```

3. **Verify Services**
   ```bash
   # Run health checks
   ./scripts/health-check-dr.sh
   
   # Verify data consistency
   ./scripts/data-integrity-check.sh
   ```

## Data Backup Procedures

### Automated Backups
- **Full Backup**: Daily at 2 AM UTC
- **Differential Backup**: Every 6 hours
- **Transaction Log Backup**: Every 15 minutes
- **Retention**: 30 days local, 90 days cross-region

### Manual Backup
```bash
# Create manual backup
BACKUP DATABASE BarqProd 
TO DISK = '/backups/BarqProd_manual_$(date +%Y%m%d_%H%M%S).bak'
WITH COMPRESSION, CHECKSUM

# Verify backup
RESTORE VERIFYONLY FROM DISK = '/backups/BarqProd_manual_*.bak'
```

## Communication Plan

### Incident Response Team
- **Incident Commander**: DevOps Lead
- **Technical Lead**: Backend Team Lead  
- **Communications**: Product Manager
- **Customer Success**: Support Team Lead

### Communication Channels
1. **Internal**: Slack #incident-response
2. **External**: Status page (status.barq.ai)
3. **Customers**: Email notifications for Tier 1 customers

### Status Page Updates
```bash
# Update status page
curl -X POST "https://api.statuspage.io/v1/pages/PAGE_ID/incidents" \
  -H "Authorization: OAuth TOKEN" \
  -d '{
    "incident": {
      "name": "Database connectivity issues",
      "status": "investigating",
      "impact_override": "major"
    }
  }'
```

## Post-Incident Procedures

### 1. Service Verification
- [ ] All health endpoints returning 200
- [ ] Database queries executing normally
- [ ] Frontend loading correctly
- [ ] Background jobs processing
- [ ] Monitoring alerts cleared

### 2. Data Integrity Checks
```bash
# Run comprehensive data validation
./scripts/post-incident-validation.sh

# Check for data loss
SELECT COUNT(*) FROM Tasks WHERE CreatedAt > 'INCIDENT_START_TIME'
SELECT COUNT(*) FROM AuditLogs WHERE Timestamp > 'INCIDENT_START_TIME'
```

### 3. Post-Mortem
- Schedule post-mortem meeting within 24 hours
- Document timeline of events
- Identify root cause
- Create action items for prevention
- Update runbooks based on lessons learned

## Emergency Contacts

### On-Call Rotation
- **Primary**: DevOps Engineer (24/7)
- **Secondary**: Backend Developer (business hours)
- **Escalation**: Engineering Manager

### Vendor Contacts
- **Azure Support**: +1-800-MICROSOFT
- **Database Vendor**: support@vendor.com
- **CDN Provider**: emergency@cdn.com

## Testing & Validation

### Monthly DR Tests
- [ ] Database restore test
- [ ] Failover to DR region
- [ ] Communication plan execution
- [ ] Recovery time measurement

### Quarterly Reviews
- [ ] Update contact information
- [ ] Review and update procedures
- [ ] Test backup integrity
- [ ] Validate monitoring alerts

---

**Last Updated**: 2024-01-01  
**Next Review**: 2024-04-01  
**Owner**: DevOps Team
