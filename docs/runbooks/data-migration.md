# Data Migration Runbook

## Overview
This runbook provides procedures for safely migrating data in the BARQ platform, including tenant data, user information, and system configurations.

## Migration Types

### Schema Migrations
- **Entity Framework Migrations**: Automated schema changes
- **Manual Schema Updates**: Complex structural changes
- **Index Management**: Performance optimization

### Data Migrations
- **Tenant Data Migration**: Moving tenant-specific data
- **User Data Migration**: User profile and preference updates
- **Configuration Migration**: System and admin settings

### System Migrations
- **Database Upgrades**: SQL Server version updates
- **Platform Migrations**: Moving between environments
- **Backup/Restore Operations**: Data recovery procedures

## Pre-Migration Checklist

### Planning Phase
- [ ] Migration plan documented and reviewed
- [ ] Rollback strategy defined
- [ ] Downtime window scheduled
- [ ] Stakeholders notified
- [ ] Backup strategy confirmed

### Technical Validation
- [ ] Migration scripts tested on staging
- [ ] Data integrity checks prepared
- [ ] Performance impact assessed
- [ ] Monitoring alerts configured
- [ ] Emergency contacts available

## Schema Migration Procedures

### Entity Framework Migrations

#### 1. Create Migration
```bash
# Generate new migration
dotnet ef migrations add MigrationName --project src/BARQ.Infrastructure --startup-project src/BARQ.API

# Review generated migration
code src/BARQ.Infrastructure/Migrations/[timestamp]_MigrationName.cs
```

#### 2. Validate Migration
```bash
# Test on staging database
dotnet ef database update --project src/BARQ.Infrastructure --startup-project src/BARQ.API \
  --connection "Server=staging-sql;Database=BARQ_STAGING;..."

# Verify schema changes
sqlcmd -S staging-sql -d BARQ_STAGING -Q "SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE 'New%'"
```

#### 3. Production Deployment
```bash
# Backup production database
sqlcmd -S production-sql -Q "BACKUP DATABASE BARQ_DB TO DISK = '/backups/pre_migration_$(date +%Y%m%d_%H%M%S).bak'"

# Apply migration with monitoring
dotnet ef database update --project src/BARQ.Infrastructure --startup-project src/BARQ.API \
  --connection "Server=production-sql;Database=BARQ_DB;..." --verbose

# Verify migration success
dotnet ef migrations list --project src/BARQ.Infrastructure --startup-project src/BARQ.API
```

### Manual Schema Updates

#### 1. Complex Structural Changes
```sql
-- Example: Adding computed columns with complex logic
BEGIN TRANSACTION;

-- Add new column
ALTER TABLE Tasks ADD ComputedPriority AS (
  CASE 
    WHEN DueDate < GETDATE() THEN 'Overdue'
    WHEN DueDate < DATEADD(day, 1, GETDATE()) THEN 'Urgent'
    WHEN DueDate < DATEADD(day, 7, GETDATE()) THEN 'High'
    ELSE 'Normal'
  END
);

-- Create index on computed column
CREATE INDEX IX_Tasks_ComputedPriority ON Tasks (ComputedPriority);

-- Verify changes
SELECT TOP 10 TaskId, DueDate, ComputedPriority FROM Tasks;

COMMIT TRANSACTION;
```

#### 2. Index Management
```sql
-- Analyze index usage
SELECT 
    i.name AS IndexName,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats s ON i.object_id = s.object_id AND i.index_id = s.index_id
WHERE OBJECT_NAME(i.object_id) = 'Tasks';

-- Create performance indexes
CREATE INDEX IX_Tasks_TenantId_Status_DueDate ON Tasks (TenantId, Status, DueDate)
INCLUDE (Title, AssignedUserId);

-- Drop unused indexes
DROP INDEX IX_Tasks_OldIndex ON Tasks;
```

## Data Migration Procedures

### Tenant Data Migration

#### 1. Tenant Consolidation
```sql
-- Migrate data from one tenant to another
BEGIN TRANSACTION;

-- Update task ownership
UPDATE Tasks 
SET TenantId = @TargetTenantId 
WHERE TenantId = @SourceTenantId;

-- Update user associations
UPDATE Users 
SET TenantId = @TargetTenantId 
WHERE TenantId = @SourceTenantId;

-- Update audit logs
UPDATE AuditLogs 
SET TenantId = @TargetTenantId 
WHERE TenantId = @SourceTenantId;

-- Verify data integrity
SELECT COUNT(*) FROM Tasks WHERE TenantId = @SourceTenantId; -- Should be 0
SELECT COUNT(*) FROM Tasks WHERE TenantId = @TargetTenantId; -- Should include migrated data

COMMIT TRANSACTION;
```

#### 2. Data Anonymization
```sql
-- Anonymize user data for compliance
UPDATE Users 
SET 
    Email = CONCAT('user', UserId, '@anonymized.com'),
    FirstName = 'Anonymous',
    LastName = 'User',
    PhoneNumber = NULL
WHERE TenantId = @TenantToAnonymize;

-- Clear sensitive audit data
UPDATE AuditLogs 
SET 
    OldValues = NULL,
    NewValues = NULL
WHERE TenantId = @TenantToAnonymize 
  AND CreatedAt < DATEADD(year, -2, GETDATE());
```

### User Data Migration

#### 1. User Profile Updates
```csharp
// C# migration script for user preferences
public async Task MigrateUserPreferences()
{
    var users = await _context.Users.ToListAsync();
    
    foreach (var user in users)
    {
        // Migrate old preference format to new format
        if (!string.IsNullOrEmpty(user.LegacyPreferences))
        {
            var oldPrefs = JsonSerializer.Deserialize<LegacyPreferences>(user.LegacyPreferences);
            var newPrefs = new UserPreferences
            {
                Theme = oldPrefs.DarkMode ? "dark" : "light",
                Language = oldPrefs.Locale ?? "en",
                Timezone = oldPrefs.TimeZone ?? "UTC",
                NotificationSettings = new NotificationSettings
                {
                    EmailEnabled = oldPrefs.EmailNotifications,
                    PushEnabled = oldPrefs.PushNotifications
                }
            };
            
            user.Preferences = JsonSerializer.Serialize(newPrefs);
            user.LegacyPreferences = null;
        }
    }
    
    await _context.SaveChangesAsync();
}
```

#### 2. Role Migration
```sql
-- Migrate from old role system to new role system
INSERT INTO UserRoles (UserId, RoleId, TenantId, CreatedAt)
SELECT 
    u.Id,
    r.Id,
    u.TenantId,
    GETDATE()
FROM Users u
CROSS JOIN Roles r
WHERE u.LegacyRole = 'Admin' AND r.Name = 'Administrator'
   OR u.LegacyRole = 'User' AND r.Name = 'StandardUser'
   OR u.LegacyRole = 'Manager' AND r.Name = 'ProjectManager';

-- Remove legacy role column
ALTER TABLE Users DROP COLUMN LegacyRole;
```

## Large-Scale Migration Procedures

### Batch Processing
```csharp
public async Task MigrateLargeDataset()
{
    const int batchSize = 1000;
    int offset = 0;
    int totalProcessed = 0;
    
    while (true)
    {
        var batch = await _context.LegacyData
            .Skip(offset)
            .Take(batchSize)
            .ToListAsync();
            
        if (!batch.Any()) break;
        
        foreach (var item in batch)
        {
            // Transform data
            var newItem = TransformLegacyItem(item);
            _context.NewData.Add(newItem);
        }
        
        await _context.SaveChangesAsync();
        
        totalProcessed += batch.Count;
        offset += batchSize;
        
        // Log progress
        _logger.LogInformation($"Processed {totalProcessed} records");
        
        // Prevent timeout
        await Task.Delay(100);
    }
}
```

### Parallel Processing
```csharp
public async Task MigrateInParallel()
{
    var tenantIds = await _context.Tenants.Select(t => t.Id).ToListAsync();
    
    var tasks = tenantIds.Select(async tenantId =>
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BarqDbContext>();
        
        await MigrateTenantData(context, tenantId);
    });
    
    await Task.WhenAll(tasks);
}
```

## Data Validation Procedures

### Integrity Checks
```sql
-- Verify referential integrity
SELECT 'Tasks with invalid TenantId' AS Issue, COUNT(*) AS Count
FROM Tasks t
LEFT JOIN Tenants tn ON t.TenantId = tn.Id
WHERE tn.Id IS NULL

UNION ALL

SELECT 'Users with invalid TenantId' AS Issue, COUNT(*) AS Count
FROM Users u
LEFT JOIN Tenants tn ON u.TenantId = tn.Id
WHERE tn.Id IS NULL

UNION ALL

SELECT 'Tasks with invalid AssignedUserId' AS Issue, COUNT(*) AS Count
FROM Tasks t
LEFT JOIN Users u ON t.AssignedUserId = u.Id
WHERE t.AssignedUserId IS NOT NULL AND u.Id IS NULL;
```

### Data Consistency Checks
```sql
-- Check for duplicate data
SELECT Email, COUNT(*) as DuplicateCount
FROM Users
GROUP BY Email
HAVING COUNT(*) > 1;

-- Verify audit trail completeness
SELECT 
    'Missing audit logs' AS Issue,
    COUNT(*) AS Count
FROM Tasks t
LEFT JOIN AuditLogs a ON a.EntityId = t.Id AND a.EntityType = 'Task'
WHERE a.Id IS NULL AND t.CreatedAt > DATEADD(day, -30, GETDATE());
```

### Performance Validation
```sql
-- Check query performance after migration
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Test critical queries
SELECT COUNT(*) FROM Tasks WHERE TenantId = @TestTenantId;
SELECT * FROM Users WHERE Email = @TestEmail;
SELECT * FROM AuditLogs WHERE EntityId = @TestEntityId ORDER BY CreatedAt DESC;

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
```

## Rollback Procedures

### Automated Rollback
```bash
# Restore from backup
sqlcmd -S production-sql -Q "RESTORE DATABASE BARQ_DB FROM DISK = '/backups/pre_migration_backup.bak' WITH REPLACE"

# Verify restoration
sqlcmd -S production-sql -d BARQ_DB -Q "SELECT COUNT(*) FROM Users; SELECT COUNT(*) FROM Tasks;"

# Restart application services
kubectl rollout restart deployment/barq-api
```

### Selective Rollback
```sql
-- Rollback specific changes
BEGIN TRANSACTION;

-- Restore specific table from backup
SELECT * INTO Tasks_Backup FROM Tasks;

RESTORE DATABASE BARQ_DB_TEMP FROM DISK = '/backups/pre_migration_backup.bak';

DELETE FROM Tasks WHERE CreatedAt > @MigrationStartTime;

INSERT INTO Tasks 
SELECT * FROM BARQ_DB_TEMP.dbo.Tasks 
WHERE CreatedAt > @MigrationStartTime;

DROP DATABASE BARQ_DB_TEMP;

COMMIT TRANSACTION;
```

## Monitoring and Alerting

### Migration Progress Monitoring
```csharp
public class MigrationProgressMonitor
{
    public async Task MonitorProgress(string migrationId)
    {
        while (true)
        {
            var progress = await GetMigrationProgress(migrationId);
            
            _logger.LogInformation($"Migration {migrationId}: {progress.PercentComplete}% complete");
            
            if (progress.IsComplete) break;
            if (progress.HasErrors) throw new MigrationException(progress.ErrorMessage);
            
            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
}
```

### Performance Monitoring
```sql
-- Monitor blocking and deadlocks during migration
SELECT 
    session_id,
    blocking_session_id,
    wait_type,
    wait_time,
    last_wait_type,
    command
FROM sys.dm_exec_requests
WHERE blocking_session_id > 0;
```

## Emergency Procedures

### Migration Failure Response
1. **Immediate Actions**
   - Stop migration process
   - Assess data integrity
   - Determine rollback necessity

2. **Communication**
   - Notify stakeholders
   - Update status page
   - Coordinate with teams

3. **Recovery**
   - Execute rollback plan
   - Verify system functionality
   - Investigate root cause

### Contact Information
- **Database Team**: db-team@barq.com
- **DevOps Team**: devops@barq.com
- **Emergency Hotline**: +1-XXX-XXX-XXXX

## Post-Migration Activities

### Validation Checklist
- [ ] Data integrity verified
- [ ] Performance benchmarks met
- [ ] Application functionality tested
- [ ] User acceptance testing completed
- [ ] Monitoring systems updated
- [ ] Documentation updated

### Cleanup Tasks
- [ ] Remove temporary tables
- [ ] Clean up backup files
- [ ] Update migration logs
- [ ] Archive old data
- [ ] Update system documentation
