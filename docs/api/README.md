# BARQ API Documentation

## Overview
The BARQ API provides comprehensive endpoints for managing AI orchestration workflows, multi-tenant operations, and enterprise features.

## Base URL
- **Production**: `https://api.barq.com`
- **Staging**: `https://staging-api.barq.com`
- **Development**: `http://localhost:5000`

## Authentication

### Cookie Authentication (Preferred)
```bash
# Login to get authentication cookie
curl -X POST https://api.barq.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName": "user@example.com", "email": "user@example.com", "password": "password"}' \
  -c cookies.txt

# Use cookie for subsequent requests
curl -X GET https://api.barq.com/api/tasks \
  -b cookies.txt
```

### Bearer Token Authentication
```bash
# Get JWT token
TOKEN=$(curl -X POST https://api.barq.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName": "user@example.com", "email": "user@example.com", "password": "password"}' \
  | jq -r '.token')

# Use token in Authorization header
curl -X GET https://api.barq.com/api/tasks \
  -H "Authorization: Bearer $TOKEN"
```

## Core Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/logout` - User logout
- `POST /api/auth/refresh` - Refresh token
- `POST /api/auth/change-password` - Change password

### Tasks
- `GET /api/tasks` - List tasks
- `POST /api/tasks` - Create task
- `GET /api/tasks/{id}` - Get task details
- `PUT /api/tasks/{id}` - Update task
- `DELETE /api/tasks/{id}` - Delete task
- `POST /api/tasks/{id}/assign` - Assign task
- `POST /api/tasks/{id}/complete` - Complete task

### Projects
- `GET /api/projects` - List projects
- `POST /api/projects` - Create project
- `GET /api/projects/{id}` - Get project details
- `PUT /api/projects/{id}` - Update project
- `DELETE /api/projects/{id}` - Delete project

### AI Orchestration
- `POST /api/ai/chat` - AI chat interaction
- `POST /api/ai/analyze` - AI analysis
- `GET /api/ai/providers` - List AI providers
- `POST /api/ai/providers/test` - Test AI provider

### Workflows
- `GET /api/workflows` - List workflows
- `POST /api/workflows` - Create workflow
- `POST /api/workflows/{id}/start` - Start workflow
- `GET /api/workflows/{id}/status` - Get workflow status

### Files
- `POST /api/files/upload` - Upload file
- `GET /api/files/{id}` - Download file
- `GET /api/files/{id}/metadata` - Get file metadata
- `DELETE /api/files/{id}` - Delete file

### Notifications
- `GET /api/notifications` - List notifications
- `POST /api/notifications/mark-read` - Mark as read
- `GET /api/notifications/preferences` - Get preferences
- `PUT /api/notifications/preferences` - Update preferences

### Admin
- `GET /api/admin/tenants` - List tenants
- `POST /api/admin/tenants` - Create tenant
- `GET /api/admin/users` - List users
- `POST /api/admin/users` - Create user
- `GET /api/admin/configuration` - Get configuration
- `PUT /api/admin/configuration` - Update configuration

## Request/Response Format

### Standard Response Format
```json
{
  "success": true,
  "data": {},
  "message": "Operation completed successfully",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### Error Response Format
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid input data",
    "details": {
      "field": "email",
      "reason": "Invalid email format"
    }
  },
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### Pagination Format
```json
{
  "success": true,
  "data": {
    "items": [],
    "total": 100,
    "page": 1,
    "pageSize": 20,
    "totalPages": 5
  }
}
```

## Status Codes

- `200 OK` - Request successful
- `201 Created` - Resource created
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource conflict
- `422 Unprocessable Entity` - Validation error
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server error

## Rate Limiting

### Limits
- **Authenticated Users**: 1000 requests per hour
- **Anonymous Users**: 100 requests per hour
- **Admin Operations**: 500 requests per hour

### Headers
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1640995200
```

## CORS Policy

### Allowed Origins
- `https://app.barq.com`
- `https://staging.barq.com`
- `http://localhost:3000` (development only)

### Allowed Methods
- `GET`, `POST`, `PUT`, `DELETE`, `OPTIONS`

### Allowed Headers
- `Content-Type`, `Authorization`, `X-XSRF-TOKEN`

## WebSocket Endpoints

### Real-time Notifications
```javascript
const ws = new WebSocket('wss://api.barq.com/ws/notifications');
ws.onmessage = (event) => {
  const notification = JSON.parse(event.data);
  console.log('New notification:', notification);
};
```

### Task Updates
```javascript
const ws = new WebSocket('wss://api.barq.com/ws/tasks');
ws.onmessage = (event) => {
  const update = JSON.parse(event.data);
  console.log('Task update:', update);
};
```

## SDK Examples

### JavaScript/TypeScript
```typescript
import { BarqClient } from '@barq/sdk';

const client = new BarqClient({
  baseUrl: 'https://api.barq.com',
  apiKey: 'your-api-key'
});

// Create a task
const task = await client.tasks.create({
  title: 'New Task',
  description: 'Task description',
  priority: 'high'
});

// List tasks
const tasks = await client.tasks.list({
  page: 1,
  pageSize: 20,
  status: 'active'
});
```

### C#
```csharp
using Barq.SDK;

var client = new BarqClient(new BarqClientOptions
{
    BaseUrl = "https://api.barq.com",
    ApiKey = "your-api-key"
});

// Create a task
var task = await client.Tasks.CreateAsync(new CreateTaskRequest
{
    Title = "New Task",
    Description = "Task description",
    Priority = TaskPriority.High
});

// List tasks
var tasks = await client.Tasks.ListAsync(new ListTasksRequest
{
    Page = 1,
    PageSize = 20,
    Status = TaskStatus.Active
});
```

## Webhook Events

### Task Events
- `task.created`
- `task.updated`
- `task.completed`
- `task.assigned`

### Workflow Events
- `workflow.started`
- `workflow.completed`
- `workflow.failed`

### Webhook Payload
```json
{
  "event": "task.created",
  "data": {
    "id": "task-id",
    "title": "Task Title",
    "status": "active"
  },
  "timestamp": "2024-01-01T00:00:00Z",
  "signature": "sha256=..."
}
```

## Testing

### Postman Collection
Download the [BARQ Postman Collection](./postman/BARQ-API.postman_collection.json) for easy API testing.

### OpenAPI Specification
The complete OpenAPI 3.0 specification is available at:
- **Swagger UI**: `https://api.barq.com/swagger`
- **JSON**: `https://api.barq.com/swagger/v1/swagger.json`
- **YAML**: `https://api.barq.com/swagger/v1/swagger.yaml`

## Support

For API support, please:
1. Check the [troubleshooting guide](./troubleshooting.md)
2. Review the [FAQ](./faq.md)
3. Contact support at api-support@barq.com
