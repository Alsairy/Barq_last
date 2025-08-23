#!/bin/bash
set -e

echo "🔍 BARQ Production Readiness Verification"
echo "========================================"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

FAILED_CHECKS=0

check_status() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✓${NC} $2"
    else
        echo -e "${RED}✗${NC} $2"
        FAILED_CHECKS=$((FAILED_CHECKS + 1))
    fi
}

echo "1. Backend Build Verification"
echo "-----------------------------"
cd Backend
dotnet build BARQ.sln -c Release --verbosity minimal > /dev/null 2>&1
check_status $? "Backend builds without errors"

echo ""
echo "2. Database Migration Verification"
echo "----------------------------------"
dotnet ef database update --project src/BARQ.Infrastructure --startup-project src/BARQ.API --no-build > /dev/null 2>&1
check_status $? "EF migrations apply cleanly"

echo ""
echo "3. Frontend Build Verification"
echo "------------------------------"
cd ../Frontend/barq-frontend
npm run build > /dev/null 2>&1
check_status $? "Frontend builds without errors"

echo ""
echo "4. Secrets Security Check"
echo "------------------------"
cd ../..
if command -v gitleaks &> /dev/null; then
    gitleaks detect --no-git > /dev/null 2>&1
    check_status $? "No hardcoded secrets detected"
else
    echo -e "${YELLOW}⚠${NC} Gitleaks not installed - skipping secrets check"
fi

echo ""
echo "5. Placeholder Sweep"
echo "-------------------"
python3 scripts/placeholder_sweep.py Backend Frontend /tmp/placeholder_check.csv > /dev/null 2>&1
PLACEHOLDER_COUNT=$(wc -l < /tmp/placeholder_check.csv)
if [ $PLACEHOLDER_COUNT -le 1 ]; then
    check_status 0 "No placeholders found in production code"
else
    check_status 1 "Found $((PLACEHOLDER_COUNT - 1)) placeholders in production code"
fi

echo ""
echo "6. Health Endpoints Check"
echo "------------------------"
cd Backend
nohup dotnet run --project src/BARQ.API/BARQ.API.csproj --no-build > /tmp/api.log 2>&1 &
API_PID=$!

sleep 10

curl -f http://localhost:5000/health/live > /dev/null 2>&1
check_status $? "Health live endpoint responds"

curl -f http://localhost:5000/health/ready > /dev/null 2>&1
check_status $? "Health ready endpoint responds"

kill $API_PID > /dev/null 2>&1 || true
cd ..

echo ""
echo "7. Documentation Completeness"
echo "-----------------------------"
[ -f "docs/onboarding/README.md" ]
check_status $? "Onboarding guide exists"

[ -f "docs/runbooks/incident-response.md" ]
check_status $? "Incident response runbook exists"

[ -f "docs/runbooks/key-rotation.md" ]
check_status $? "Key rotation runbook exists"

[ -f "docs/adrs/004-production-security.md" ]
check_status $? "Production security ADR exists"

echo ""
echo "8. CI Configuration Check"
echo "-------------------------"
if ! grep -q "|| true" .github/workflows/*.yml; then
    check_status 0 "No CI fallback tolerances found"
else
    check_status 1 "CI fallback tolerances still present"
fi

echo ""
echo "========================================"
if [ $FAILED_CHECKS -eq 0 ]; then
    echo -e "${GREEN}🎉 All production readiness checks passed!${NC}"
    echo -e "${GREEN}✓ BARQ is ready for v1.0.0 release${NC}"
    exit 0
else
    echo -e "${RED}❌ $FAILED_CHECKS production readiness checks failed${NC}"
    echo -e "${RED}✗ BARQ requires additional hardening before release${NC}"
    exit 1
fi
