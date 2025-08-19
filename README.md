# BARQ - Enterprise AI Orchestration Platform

BARQ is a comprehensive enterprise AI orchestration platform that provides sophisticated workflow automation, business process management, and AI agent coordination capabilities.

## 🚀 Quick Start

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- Docker & Docker Compose
- SQL Server (or use Docker)

### Development Setup

1. **Clone and setup environment:**
   ```bash
   git clone <repository-url>
   cd Barq_last
   make setup
   ```

2. **Start development servers:**
   ```bash
   # Terminal 1: Start backend API
   make dev-api
   
   # Terminal 2: Start frontend
   make dev-frontend
   ```

3. **Access the application:**
   - Frontend: http://localhost:5173
   - API: http://localhost:5000
   - Swagger: http://localhost:5000/swagger
   - Flowable UI: http://localhost:8080 (admin/test)
   - Mailhog: http://localhost:8025

## 🏗️ Architecture

### Backend (.NET 8)
- **Clean Architecture** with separate layers
- **Entity Framework Core** with SQL Server
- **ASP.NET Core Identity** for authentication
- **Flowable BPM** integration for workflows
- **Comprehensive audit logging** and soft delete

### Frontend (React + TypeScript)
- **Manus-inspired three-panel layout**
- **Radix UI** components with Tailwind CSS
- **Redux Toolkit** + React Query for state management
- **Comprehensive testing** with Jest and Playwright

### Infrastructure
- **Docker Compose** for development environment
- **SQL Server** database
- **Redis** for caching
- **Flowable** for BPM workflows
- **Mailhog** for email testing

## 📁 Project Structure

```
Barq_last/
├── Backend/
│   ├── src/
│   │   ├── BARQ.API/          # Web API controllers and configuration
│   │   ├── BARQ.Application/   # Business logic and services
│   │   ├── BARQ.Core/         # Domain entities and DTOs
│   │   └── BARQ.Infrastructure/ # Data access and external services
│   └── BARQ.sln
├── Frontend/
│   └── barq-frontend/         # React TypeScript application
├── docker-compose.yml         # Development environment
├── Makefile                   # Development commands
└── README.md
```

## 🛠️ Development Commands

| Command | Description |
|---------|-------------|
| `make dev-up` | Start development environment (DB, Redis, Flowable) |
| `make dev-down` | Stop development environment |
| `make build` | Build backend solution |
| `make test` | Run backend tests |
| `make test-frontend` | Run frontend tests |
| `make e2e` | Run end-to-end tests |
| `make migrate` | Run database migrations |
| `make seed` | Seed database with initial data |
| `make health` | Check service health |
| `make logs` | View all service logs |

## 🗄️ Database Schema

The platform includes comprehensive entities:

- **Multi-tenancy**: Tenants, Users, Roles
- **AI Management**: AIProviders, AIAgents, TaskExecutions
- **Project Management**: Projects, Tasks, Documents
- **Workflow Engine**: Workflows, WorkflowInstances
- **Administration**: AdminConfigurations, AuditLogs
- **Notifications**: Notifications, SecurityEvents

## 🔧 Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection="Server=localhost;Database=BARQ_DB;..."

# JWT Authentication
Jwt__Key="your-secret-key"
Jwt__Issuer="BARQ"
Jwt__Audience="BARQ-Users"

# Flowable Integration
BARQ__FlowableApiUrl="http://localhost:9999"

# Email (Development)
BARQ__SmtpHost="localhost"
BARQ__SmtpPort="1025"
```

### Feature Flags

The platform uses feature flags for controlled rollouts:

```json
{
  "Features": {
    "Flowable": true,
    "AdvancedWorkflows": false,
    "AIOrchestration": true
  }
}
```

## 🧪 Testing

### Backend Testing
```bash
# Unit tests
make test

# Integration tests
cd Backend && dotnet test --filter Category=Integration
```

### Frontend Testing
```bash
# Unit tests
make test-frontend

# E2E tests
make e2e

# Watch mode
cd Frontend/barq-frontend && npm run test:watch
```

## 🚀 Deployment

### Docker Deployment
```bash
# Build and run full stack
make full-up

# Production deployment
docker-compose -f docker-compose.prod.yml up -d
```

### Manual Deployment
1. Build backend: `make build`
2. Build frontend: `cd Frontend/barq-frontend && npm run build`
3. Deploy to your hosting platform

## 📊 Monitoring & Health Checks

- **Health endpoint**: `/health`
- **Metrics**: Performance metrics collection
- **Audit logs**: Comprehensive activity tracking
- **Security events**: Security monitoring and alerts

## 🔐 Security

- **JWT Authentication** with role-based access control
- **Multi-tenant isolation** with tenant-scoped data
- **Audit logging** for all operations
- **Security event tracking** and monitoring
- **Input validation** and sanitization

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feat/your-feature`
3. Make your changes following the coding standards
4. Run tests: `make test && make test-frontend`
5. Commit your changes: `git commit -m "feat: add your feature"`
6. Push to the branch: `git push origin feat/your-feature`
7. Create a Pull Request

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

For support and questions:
- Create an issue in the repository
- Check the documentation in `/docs`
- Review the API documentation at `/swagger`

---

**BARQ Platform** - Empowering enterprises with intelligent workflow automation and AI orchestration.
