# BARQ Platform Quick Start Guide

## Prerequisites
- .NET 8 SDK
- Node.js 18+ with npm/pnpm
- SQL Server (LocalDB for development)
- Docker (optional, for containerized development)

## 🚀 60-Minute Setup to First E2E Run

### Step 1: Clone and Setup (5 minutes)
```bash
git clone https://github.com/Alsairy/Barq_last.git
cd Barq_last

# Install backend dependencies
cd Backend
dotnet restore

# Install frontend dependencies  
cd ../Frontend/barq-frontend
npm install
```

### Step 2: Database Setup (10 minutes)
```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Create and migrate database
cd ../../Backend
dotnet ef database update --project src/BARQ.Infrastructure --startup-project src/BARQ.API

# Verify database created
sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT name FROM sys.databases WHERE name = 'BARQ_DB'"
```

### Step 3: Configuration (5 minutes)
```bash
# Copy environment template
cp .env.example .env

# Update connection string in appsettings.Development.json if needed
# Default LocalDB connection should work out of the box
```

### Step 4: Start Backend API (10 minutes)
```bash
cd Backend/src/BARQ.API
dotnet run

# Verify API is running
curl http://localhost:5000/health/live
curl http://localhost:5000/health/ready

# Check Swagger UI at http://localhost:5000/swagger
```

### Step 5: Start Frontend (10 minutes)
```bash
# In new terminal
cd Frontend/barq-frontend
npm run dev

# Frontend available at http://localhost:5173
```

### Step 6: Run E2E Tests (15 minutes)
```bash
# Install Playwright browsers
npx playwright install

# Run smoke tests
npm run test:e2e

# Run comprehensive journey tests
npx playwright test tests/journeys.comprehensive.e2e.spec.ts
```

### Step 7: Verify Full Stack (5 minutes)
```bash
# Test API endpoints
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Test frontend features
# 1. Navigate to http://localhost:5173
# 2. Login with admin/admin123
# 3. Create a test task
# 4. Verify task appears in dashboard
# 5. Check notifications work
```

## 🔧 Development Workflow

### Backend Development
```bash
# Run with hot reload
dotnet watch run --project Backend/src/BARQ.API

# Run tests
dotnet test Backend/tests/

# Add migration
dotnet ef migrations add MigrationName --project Backend/src/BARQ.Infrastructure --startup-project Backend/src/BARQ.API
```

### Frontend Development
```bash
# Development server with hot reload
npm run dev

# Build for production
npm run build

# Run tests
npm run test
npm run test:e2e
```

### Database Operations
```bash
# Reset database
dotnet ef database drop --project Backend/src/BARQ.Infrastructure --startup-project Backend/src/BARQ.API
dotnet ef database update --project Backend/src/BARQ.Infrastructure --startup-project Backend/src/BARQ.API

# View migration history
dotnet ef migrations list --project Backend/src/BARQ.Infrastructure --startup-project Backend/src/BARQ.API
```

## 🧪 Testing Strategy

### Unit Tests
```bash
# Backend unit tests
dotnet test Backend/tests/BARQ.UnitTests/

# Frontend unit tests
cd Frontend/barq-frontend
npm run test
```

### Integration Tests
```bash
# API integration tests
dotnet test Backend/tests/BARQ.IntegrationTests/

# E2E tests
cd Frontend/barq-frontend
npm run test:e2e
```

### Performance Tests
```bash
# Load testing
dotnet test Backend/tests/BARQ.PerformanceTests/
```

## 🐛 Troubleshooting

### Common Issues

**Database Connection Failed**
```bash
# Check SQL Server is running
sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT @@VERSION"

# Recreate LocalDB instance
sqllocaldb delete MSSQLLocalDB
sqllocaldb create MSSQLLocalDB
```

**Frontend Build Errors**
```bash
# Clear node modules and reinstall
rm -rf node_modules package-lock.json
npm install

# Clear build cache
npm run clean
```

**API Not Starting**
```bash
# Check port availability
netstat -an | grep :5000

# Verify appsettings.json configuration
cat Backend/src/BARQ.API/appsettings.Development.json
```

**E2E Tests Failing**
```bash
# Update Playwright browsers
npx playwright install --with-deps

# Run tests in headed mode for debugging
npx playwright test --headed

# Generate test report
npx playwright show-report
```

### Health Check Endpoints
- `GET /health/live` - Basic liveness check
- `GET /health/ready` - Readiness check (includes DB)
- `GET /health/flowable` - BPM engine status
- `GET /health/ai` - AI provider connectivity

### Log Locations
- **Backend**: Console output and `logs/` directory
- **Frontend**: Browser console and network tab
- **Database**: SQL Server logs via SQL Server Management Studio

## 📚 Next Steps

After successful setup:
1. Review [Architecture Documentation](../README.md)
2. Read [API Documentation](../api/README.md)
3. Check [Deployment Guide](../deployment/README.md)
4. Explore [ADR Documents](../adrs/)

## 🆘 Getting Help

- **Issues**: Create GitHub issue with reproduction steps
- **Questions**: Check existing documentation first
- **Contributions**: Follow the contribution guidelines

---

**Setup Time**: ~60 minutes  
**Last Updated**: 2024-01-01  
**Tested On**: Windows 11, macOS 14, Ubuntu 22.04
