# BARQ Platform Onboarding Guide

## Quick Start (< 60 minutes to first E2E run)

### Prerequisites
- .NET 8 SDK
- Node.js 18+ with npm
- SQL Server (LocalDB or Docker)
- Git

### 1. Clone and Setup (5 minutes)
```bash
git clone https://github.com/Alsairy/Barq_last.git
cd Barq_last
```

### 2. Backend Setup (15 minutes)
```bash
cd Backend

# Restore packages
dotnet restore BARQ.sln

# Setup database
dotnet ef database update --project src/BARQ.Infrastructure --startup-project src/BARQ.API

# Build solution
dotnet build BARQ.sln -c Release

# Run API (will start on https://localhost:5001)
dotnet run --project src/BARQ.API/BARQ.API.csproj
```

### 3. Frontend Setup (10 minutes)
```bash
cd Frontend/barq-frontend

# Install dependencies
npm install

# Build frontend
npm run build

# Start development server (will start on http://localhost:5173)
npm run dev
```

### 4. Verify Health Endpoints (2 minutes)
```bash
curl https://localhost:5001/health/live
curl https://localhost:5001/health/ready
curl https://localhost:5001/health/flowable
curl https://localhost:5001/health/ai
```

### 5. Run E2E Tests (15 minutes)
```bash
cd Frontend/barq-frontend

# Install Playwright browsers
npx playwright install

# Run smoke tests
npx playwright test tests/wiring.smoke.spec.ts

# Run full E2E journey tests
npx playwright test tests/journeys.e2e.spec.ts
```

### 6. Access the Application (5 minutes)
1. Open browser to http://localhost:5173
2. Register new account or use seeded admin:
   - Email: admin@barq.local
   - Password: Admin123!
3. Create a test task
4. Verify multi-tenant functionality
5. Test file upload and AI integration

### 7. Development Workflow (8 minutes)
```bash
# Run backend tests
cd Backend
dotnet test

# Run frontend tests
cd Frontend/barq-frontend
npm test

# Run linting
npm run lint

# Check for security issues
npm audit

# Run full audit suite
python3 scripts/backend_audit.py --src Backend --out audit/audit_backend.csv
python3 scripts/placeholder_sweep.py Backend Frontend audit/audit_placeholders.csv
node scripts/frontend_audit.js --src Frontend/barq-frontend --out audit/audit_frontend.csv
```

## Architecture Overview

### Backend (.NET 8)
- **API Layer**: ASP.NET Core Web API with Swagger
- **Application Layer**: Business logic and services
- **Infrastructure Layer**: Data access with Entity Framework Core
- **Core Layer**: Domain entities and DTOs

### Frontend (React + TypeScript)
- **Vite**: Build tool and dev server
- **React Query**: Server state management
- **Redux Toolkit**: Client state management
- **Radix UI**: Component library
- **Playwright**: E2E testing

### Database (SQL Server)
- Multi-tenant architecture with tenant isolation
- Comprehensive audit logging
- Soft delete with recycle bin functionality
- Performance optimized with proper indexing

### Key Features
- **AI Orchestration**: Multi-provider AI integration (OpenAI, Azure)
- **Workflow Engine**: Flowable BPM integration
- **SLA Management**: Automated monitoring and escalation
- **File Management**: Upload, virus scanning, quarantine
- **Billing & Quotas**: Usage tracking and plan management
- **Multi-tenancy**: Complete data isolation
- **Observability**: Structured logging, metrics, health checks

## Common Issues & Solutions

### Database Connection Issues
```bash
# Check SQL Server is running
sqlcmd -S localhost -E -Q "SELECT 1"

# Reset database
dotnet ef database drop --force --project src/BARQ.Infrastructure --startup-project src/BARQ.API
dotnet ef database update --project src/BARQ.Infrastructure --startup-project src/BARQ.API
```

### Frontend Build Issues
```bash
# Clear node modules and reinstall
rm -rf node_modules package-lock.json
npm install

# Clear build cache
rm -rf dist .vite
npm run build
```

### Port Conflicts
- Backend: Change ASPNETCORE_URLS in launchSettings.json
- Frontend: Change port in vite.config.ts

## Next Steps
- Review [Architecture Decision Records](../adrs/)
- Read [Deployment Guide](../deployment/README.md)
- Check [API Documentation](../api/README.md)
- Study [Runbooks](../runbooks/) for operations

## Support
- Internal Wiki: [BARQ Documentation](../README.md)
- Issue Tracking: GitHub Issues
- Team Chat: #barq-platform
