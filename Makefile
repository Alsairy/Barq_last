.PHONY: help dev-up dev-down build test clean migrate seed

help: ## Show this help message
	@echo 'Usage: make [target]'
	@echo ''
	@echo 'Targets:'
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  %-15s %s\n", $$1, $$2}' $(MAKEFILE_LIST)

dev-up: ## Start development environment (DB, Redis, Flowable, Mailhog)
	docker-compose up -d sqlserver redis flowable mailhog
	@echo "Waiting for services to be ready..."
	@sleep 30
	@echo "Development environment is ready!"
	@echo "- SQL Server: localhost:1433 (sa/YourStrong@Passw0rd)"
	@echo "- Redis: localhost:6379"
	@echo "- Flowable UI: http://localhost:8080 (admin/test)"
	@echo "- Flowable REST API: http://localhost:9999"
	@echo "- Mailhog UI: http://localhost:8025"

dev-down: ## Stop development environment
	docker-compose down

full-up: ## Start full stack (including API and Frontend)
	docker-compose --profile full-stack up -d

full-down: ## Stop full stack
	docker-compose --profile full-stack down

build: ## Build the backend solution
	cd Backend && dotnet build BARQ.sln

test: ## Run backend tests
	cd Backend && dotnet test BARQ.sln

test-frontend: ## Run frontend tests
	cd Frontend/barq-frontend && npm test

e2e: ## Run end-to-end tests
	cd Frontend/barq-frontend && npm run e2e

clean: ## Clean build artifacts
	cd Backend && dotnet clean BARQ.sln
	cd Frontend/barq-frontend && rm -rf dist node_modules/.cache

migrate: ## Run database migrations
	cd Backend/src/BARQ.API && dotnet ef database update

migrate-create: ## Create a new migration (usage: make migrate-create NAME=MigrationName)
	cd Backend/src/BARQ.API && dotnet ef migrations add $(NAME)

seed: ## Seed the database with initial data
	cd Backend/src/BARQ.API && dotnet run --seed-data

dev-cert: ## Generate development certificates
	dotnet dev-certs https --trust

logs: ## View logs from all services
	docker-compose logs -f

logs-api: ## View API logs
	docker-compose logs -f barq-api

logs-flowable: ## View Flowable logs
	docker-compose logs -f flowable

health: ## Check health of all services
	@echo "Checking service health..."
	@curl -s http://localhost:5000/health || echo "API: Not ready"
	@curl -s http://localhost:8080/flowable-ui/ > /dev/null && echo "Flowable: Ready" || echo "Flowable: Not ready"
	@redis-cli -h localhost ping > /dev/null && echo "Redis: Ready" || echo "Redis: Not ready"

install: ## Install all dependencies
	cd Backend && dotnet restore BARQ.sln
	cd Frontend/barq-frontend && npm install

format: ## Format code
	cd Backend && dotnet format BARQ.sln
	cd Frontend/barq-frontend && npm run format

lint: ## Run linting
	cd Frontend/barq-frontend && npm run lint

setup: install dev-cert dev-up migrate seed ## Complete development setup
	@echo "Development environment setup complete!"
	@echo "Run 'make dev-api' and 'make dev-frontend' to start development servers"

dev-api: ## Start API in development mode
	cd Backend/src/BARQ.API && dotnet run

dev-frontend: ## Start frontend in development mode
	cd Frontend/barq-frontend && npm run dev
