
PRINT 'BARQ Database Schema Audit Report'
PRINT '================================='
PRINT ''

PRINT 'Required Tables Check:'
PRINT '---------------------'

DECLARE @RequiredTables TABLE (TableName NVARCHAR(128))
INSERT INTO @RequiredTables VALUES 
    ('SlaPolicy'), ('SlaViolation'), ('EscalationAction'), ('EscalationRule'),
    ('FileAttachment'), ('FileAttachmentAccess'), ('FileQuarantine'),
    ('Notification'), ('NotificationPreference'), ('NotificationHistory'),
    ('AuditLog'), ('AuditReport'), ('ReportTemplate'),
    ('BillingPlan'), ('TenantSubscription'), ('Invoice'), ('InvoiceLineItem'), ('UsageRecord'), ('UsageQuota'),
    ('FeatureFlag'), ('FeatureFlagHistory'), ('TenantState'), ('TenantStateHistory'),
    ('ImpersonationSession'), ('ImpersonationAction'), ('SystemHealth'),
    ('Translation'), ('Language'), ('UserLanguagePreference'),
    ('AccessibilityAudit'), ('AccessibilityIssue'),
    ('TechnologyConstraint'), ('TemplateValidation'), ('ConstraintViolation'),
    ('BusinessCalendar'), ('EmailTemplate')

SELECT 
    rt.TableName,
    CASE WHEN t.TABLE_NAME IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END AS Status
FROM @RequiredTables rt
LEFT JOIN INFORMATION_SCHEMA.TABLES t ON rt.TableName = t.TABLE_NAME
ORDER BY Status DESC, rt.TableName

PRINT ''
PRINT 'SLA Hot Path Indexes Check:'
PRINT '--------------------------'

SELECT 
    i.name AS IndexName,
    t.name AS TableName,
    CASE WHEN i.name IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END AS Status
FROM (
    SELECT 'IX_SlaViolation_TenantId_TaskId_SlaPolicyId' AS ExpectedIndex, 'SlaViolation' AS TableName
    UNION ALL
    SELECT 'IX_SlaViolation_CreatedAt', 'SlaViolation'
    UNION ALL
    SELECT 'IX_EscalationAction_SlaViolationId', 'EscalationAction'
    UNION ALL
    SELECT 'IX_Task_TenantId_Priority', 'Task'
    UNION ALL
    SELECT 'IX_Task_TenantId_Status', 'Task'
) expected
LEFT JOIN sys.tables t ON expected.TableName = t.name
LEFT JOIN sys.indexes i ON t.object_id = i.object_id AND expected.ExpectedIndex = i.name
ORDER BY Status DESC, TableName, IndexName

PRINT ''
PRINT 'Foreign Key Constraints Check:'
PRINT '-----------------------------'

SELECT 
    fk.name AS ForeignKeyName,
    tp.name AS ParentTable,
    tr.name AS ReferencedTable,
    fk.delete_referential_action_desc AS DeleteAction,
    CASE 
        WHEN fk.delete_referential_action_desc = 'CASCADE' AND tr.name IN ('Tenant', 'ApplicationUser') THEN 'CORRECT'
        WHEN fk.delete_referential_action_desc = 'NO_ACTION' AND tr.name NOT IN ('Tenant', 'ApplicationUser') THEN 'CORRECT'
        ELSE 'REVIEW_NEEDED'
    END AS Assessment
FROM sys.foreign_keys fk
INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id
INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
WHERE tp.name IN (
    SELECT TableName FROM @RequiredTables
)
ORDER BY Assessment DESC, ParentTable, ReferencedTable

PRINT ''
PRINT 'Uniqueness Constraints Check:'
PRINT '----------------------------'

SELECT 
    i.name AS IndexName,
    t.name AS TableName,
    i.is_unique AS IsUnique,
    CASE WHEN i.is_unique = 1 THEN 'CORRECT' ELSE 'MISSING_UNIQUE' END AS Status
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE (
    (t.name = 'SlaViolation' AND i.name LIKE '%TenantId%TaskId%SlaPolicyId%') OR
    (t.name = 'UserLanguagePreference' AND i.name LIKE '%UserId%LanguageId%') OR
    (t.name = 'NotificationPreference' AND i.name LIKE '%UserId%NotificationType%') OR
    (t.name = 'TenantSubscription' AND i.name LIKE '%TenantId%PlanId%')
)
ORDER BY Status DESC, TableName

PRINT ''
PRINT 'Seed/Mock Data Detection:'
PRINT '------------------------'

DECLARE @MockDataFound BIT = 0

IF EXISTS (SELECT 1 FROM ApplicationUser WHERE Email LIKE '%test%' OR Email LIKE '%mock%' OR Email LIKE '%dummy%')
BEGIN
    PRINT 'WARNING: Test user accounts found in ApplicationUser table'
    SET @MockDataFound = 1
END

IF EXISTS (SELECT 1 FROM Tenant WHERE Name LIKE '%test%' OR Name LIKE '%mock%' OR Name LIKE '%dummy%')
BEGIN
    PRINT 'WARNING: Test tenant records found in Tenant table'
    SET @MockDataFound = 1
END

IF EXISTS (SELECT 1 FROM SlaPolicy WHERE Name LIKE '%test%' OR Name LIKE '%mock%' OR Description LIKE '%dummy%')
BEGIN
    PRINT 'WARNING: Test SLA policies found in SlaPolicy table'
    SET @MockDataFound = 1
END

IF EXISTS (SELECT 1 FROM BillingPlan WHERE Name LIKE '%test%' OR Name LIKE '%mock%' OR Description LIKE '%dummy%')
BEGIN
    PRINT 'WARNING: Test billing plans found in BillingPlan table'
    SET @MockDataFound = 1
END

IF @MockDataFound = 0
BEGIN
    PRINT 'No obvious test/mock data patterns detected'
END

PRINT ''
PRINT 'Database Audit Complete'
PRINT '======================'
