# Key Rotation Runbook

## Overview
This runbook provides procedures for rotating cryptographic keys and secrets in the BARQ platform.

## Key Types and Rotation Schedule

### JWT Signing Keys
- **Rotation Frequency**: Every 90 days
- **Emergency Rotation**: Within 4 hours of compromise

### Database Credentials
- **Rotation Frequency**: Every 180 days
- **Emergency Rotation**: Within 2 hours of compromise

### API Keys (External Services)
- **Rotation Frequency**: Every 365 days
- **Emergency Rotation**: Within 1 hour of compromise

### Encryption Keys
- **Rotation Frequency**: Every 365 days
- **Emergency Rotation**: Within 8 hours of compromise

## JWT Key Rotation

### Preparation
1. **Generate new key pair**
   ```bash
   # Generate new RSA key pair
   openssl genrsa -out jwt-private-new.key 2048
   openssl rsa -in jwt-private-new.key -pubout -out jwt-public-new.key
   ```

2. **Update key vault**
   ```bash
   # Store new keys in Azure Key Vault
   az keyvault secret set --vault-name barq-keyvault --name jwt-private-key-new --file jwt-private-new.key
   az keyvault secret set --vault-name barq-keyvault --name jwt-public-key-new --file jwt-public-new.key
   ```

### Deployment
1. **Update application configuration**
   ```json
   {
     "Authentication": {
       "Jwt": {
         "Keys": [
           {
             "KeyId": "key-new",
             "PrivateKey": "jwt-private-key-new",
             "PublicKey": "jwt-public-key-new"
           },
           {
             "KeyId": "key-old",
             "PrivateKey": "jwt-private-key-old",
             "PublicKey": "jwt-public-key-old"
           }
         ]
       }
     }
   }
   ```

2. **Deploy with dual key support**
   ```bash
   # Deploy to staging first
   kubectl apply -f k8s/staging/

   # Verify JWT validation works with both keys
   ./scripts/test-jwt-validation.sh

   # Deploy to production
   kubectl apply -f k8s/production/
   ```

3. **Switch to new key for signing**
   ```bash
   # Update configuration to use new key for signing
   kubectl patch configmap barq-config --patch '{"data":{"JWT_SIGNING_KEY_ID":"key-new"}}'

   # Restart application
   kubectl rollout restart deployment/barq-api
   ```

### Cleanup (After 24 hours)
1. **Remove old key**
   ```bash
   # Remove old key from configuration
   kubectl patch configmap barq-config --patch '{"data":{"JWT_KEYS":"[{\"KeyId\":\"key-new\",\"PrivateKey\":\"jwt-private-key-new\",\"PublicKey\":\"jwt-public-key-new\"}]"}}'

   # Deploy updated configuration
   kubectl rollout restart deployment/barq-api
   ```

2. **Verify and cleanup**
   ```bash
   # Verify old tokens are rejected
   ./scripts/test-old-jwt-rejection.sh

   # Remove old keys from vault
   az keyvault secret delete --vault-name barq-keyvault --name jwt-private-key-old
   az keyvault secret delete --vault-name barq-keyvault --name jwt-public-key-old
   ```

## Database Credential Rotation

### SQL Server Credentials
1. **Create new login**
   ```sql
   -- Create new login with strong password
   CREATE LOGIN barq_app_new WITH PASSWORD = 'NewStrongPassword123!';
   
   -- Grant same permissions as old user
   USE BARQ_DB;
   CREATE USER barq_app_new FOR LOGIN barq_app_new;
   ALTER ROLE db_datareader ADD MEMBER barq_app_new;
   ALTER ROLE db_datawriter ADD MEMBER barq_app_new;
   ALTER ROLE db_ddladmin ADD MEMBER barq_app_new;
   ```

2. **Update connection strings**
   ```bash
   # Update connection string in Key Vault
   az keyvault secret set --vault-name barq-keyvault --name db-connection-string \
     --value "Server=sql-server;Database=BARQ_DB;User Id=barq_app_new;Password=NewStrongPassword123!;TrustServerCertificate=true"
   ```

3. **Deploy and verify**
   ```bash
   # Deploy with new connection string
   kubectl rollout restart deployment/barq-api

   # Verify database connectivity
   kubectl exec -it deployment/barq-api -- dotnet run --project tools/DbHealthCheck
   ```

4. **Cleanup old credentials**
   ```sql
   -- Remove old user and login
   USE BARQ_DB;
   DROP USER barq_app_old;
   DROP LOGIN barq_app_old;
   ```

## API Key Rotation

### OpenAI API Keys
1. **Generate new API key**
   - Log into OpenAI dashboard
   - Generate new API key
   - Note the key ID for tracking

2. **Update configuration**
   ```bash
   # Store new API key
   az keyvault secret set --vault-name barq-keyvault --name openai-api-key --value "sk-new-api-key"

   # Update application configuration
   kubectl patch secret barq-secrets --patch '{"data":{"OPENAI_API_KEY":"c2stbmV3LWFwaS1rZXk="}}'
   ```

3. **Deploy and test**
   ```bash
   # Restart services
   kubectl rollout restart deployment/barq-api

   # Test AI functionality
   curl -X POST https://api.barq.com/api/ai/test \
     -H "Authorization: Bearer $JWT_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"message": "Test AI integration"}'
   ```

4. **Revoke old key**
   - Log into OpenAI dashboard
   - Revoke the old API key
   - Verify no errors in application logs

## Emergency Rotation Procedures

### Immediate Response (0-30 minutes)
1. **Assess compromise scope**
2. **Revoke compromised credentials**
3. **Generate new credentials**
4. **Deploy emergency fix**

### Short-term (30 minutes - 4 hours)
1. **Implement proper rotation**
2. **Verify security**
3. **Monitor for abuse**
4. **Document incident**

### Follow-up (4-24 hours)
1. **Complete security audit**
2. **Update procedures**
3. **Notify stakeholders**
4. **Implement preventive measures**

## Verification Procedures

### Post-Rotation Checklist
- [ ] New credentials work correctly
- [ ] Old credentials are revoked
- [ ] No authentication errors in logs
- [ ] All services functioning normally
- [ ] Security monitoring updated
- [ ] Documentation updated
- [ ] Team notified

### Automated Testing
```bash
# Run comprehensive security tests
./scripts/security-test-suite.sh

# Verify credential rotation
./scripts/verify-key-rotation.sh

# Test all authentication flows
./scripts/test-auth-flows.sh
```

## Monitoring and Alerting

### Key Expiration Alerts
- **30 days before expiration**: Warning alert
- **7 days before expiration**: Critical alert
- **1 day before expiration**: Emergency alert

### Failed Rotation Alerts
- **Authentication failures**: Immediate alert
- **Service degradation**: Immediate alert
- **Rollback required**: Emergency alert

## Contact Information

### Security Team
- **Primary**: security@barq.com
- **Emergency**: +1-XXX-XXX-XXXX

### Operations Team
- **Primary**: ops@barq.com
- **Emergency**: +1-XXX-XXX-XXXX
