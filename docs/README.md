# BARQ Enterprise AI Orchestration Platform

## Overview

BARQ is a world-class enterprise AI orchestration platform designed for Q3 2026 deployment. The platform provides comprehensive workflow automation, multi-tenant architecture, and enterprise-grade security features.

## Quick Start

### Prerequisites

- .NET 8 SDK
- Node.js 18+ with pnpm
- SQL Server 2019+
- Docker & Docker Compose
- Git

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/Alsairy/Barq_last.git
   cd Barq_last
   ```

2. **Environment Setup**
   ```bash
   cp .env.example .env
   # Edit .env with your configuration
   ```

3. **Database Setup**
   ```bash
   cd Backend
   dotnet ef database update --project src/BARQ.Infrastructure --startup-project src/BARQ.API
   ```

4. **Backend Setup**
   ```bash
   cd Backend
   dotnet restore
   dotnet build
   dotnet run --project src/BARQ.API
   ```

5. **Frontend Setup**
   ```bash
   cd Frontend/barq-frontend
   pnpm install
   pnpm dev
   ```

6. **Run E2E Tests**
   ```bash
   cd Backend
   dotnet test
   cd ../Frontend/barq-frontend
   pnpm test:e2e
   ```

### 60-Minute Onboarding Checklist

- [ ] Prerequisites installed
- [ ] Repository cloned
- [ ] Environment configured
- [ ] Database migrated
- [ ] Backend running on http://localhost:5000
- [ ] Frontend running on http://localhost:3000
- [ ] All tests passing
- [ ] E2E journey completed

## Architecture

### Core Components

- **Backend**: ASP.NET Core 8 with Clean Architecture
- **Frontend**: React 18 + TypeScript + Vite
- **Database**: SQL Server with Entity Framework Core
- **BPM**: Flowable integration via REST API
- **Authentication**: JWT with cookie dual-mode
- **Multi-tenancy**: Tenant-aware data isolation

### Key Features

- AI Provider abstraction with cost/latency telemetry
- SLA monitoring with escalation workflows
- File management with AV scanning
- Comprehensive audit logging
- Billing and quota management
- Real-time notifications
- Observability and health monitoring

## Documentation

- [Architecture Decision Records](./adrs/)
- [API Documentation](./api/)
- [Deployment Guide](./deployment/)
- [Runbooks](./runbooks/)
- [Development Guide](./development/)

## Support

For technical support, please refer to the [runbooks](./runbooks/) or contact the development team.
