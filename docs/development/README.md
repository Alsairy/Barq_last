# BARQ Development Guide

## Overview
This guide provides comprehensive information for developers working on the BARQ enterprise AI orchestration platform.

## Getting Started

### Prerequisites
- **.NET 8 SDK**
- **Node.js 18+ with pnpm**
- **SQL Server 2019+ or SQL Server Express**
- **Redis 6.0+**
- **Docker & Docker Compose**
- **Git**
- **Visual Studio 2022 or VS Code**

### Initial Setup
```bash
# Clone repository
git clone https://github.com/Alsairy/Barq_last.git
cd Barq_last

# Copy environment template
cp .env.example .env

# Install backend dependencies
cd Backend
dotnet restore

# Install frontend dependencies
cd ../Frontend/barq-frontend
pnpm install

# Start development environment
cd ../..
docker-compose -f docker-compose.dev.yml up -d
```

### Database Setup
```bash
# Apply migrations
cd Backend
dotnet ef database update --project src/BARQ.Infrastructure --startup-project src/BARQ.API

# Seed development data
dotnet run --project src/BARQ.API -- --seed-data
```

## Architecture Overview

### Backend Architecture
```
┌─────────────────┐
│   BARQ.API      │  ← Controllers, Middleware, Configuration
├─────────────────┤
│ BARQ.Application│  ← Services, Interfaces, Business Logic
├─────────────────┤
│ BARQ.Infrastructure│ ← Data Access, External Services
├─────────────────┤
│   BARQ.Core     │  ← Entities, DTOs, Interfaces
└─────────────────┘
```

### Frontend Architecture
```
┌─────────────────┐
│   Components    │  ← UI Components (Radix UI)
├─────────────────┤
│   Features      │  ← Feature-specific components
├─────────────────┤
│   Services      │  ← API calls, utilities
├─────────────────┤
│   Store         │  ← Redux Toolkit state management
└─────────────────┘
```

## Development Workflow

### Branch Strategy
- **main**: Production-ready code
- **develop**: Integration branch
- **feature/**: Feature development
- **hotfix/**: Critical fixes
- **release/**: Release preparation

### Naming Conventions
```bash
# Branch naming
feature/task-management-ui
hotfix/auth-cookie-fix
release/v2.1.0

# Commit messages
feat(auth): implement cookie-based authentication
fix(api): resolve task assignment bug
docs(readme): update installation instructions
```

### Code Style

#### Backend (.NET)
```csharp
// Use PascalCase for public members
public class TaskService : ITaskService
{
    private readonly IRepository<Task> _taskRepository;
    
    public async Task<TaskDto> CreateTaskAsync(CreateTaskRequest request)
    {
        // Implementation
    }
}

// Use camelCase for private fields
private readonly ILogger<TaskService> _logger;

// Use explicit types when not obvious
var tasks = new List<TaskDto>();
TaskDto task = await GetTaskAsync(id);
```

#### Frontend (TypeScript/React)
```typescript
// Use PascalCase for components
export const TaskManagementPanel: React.FC<TaskManagementPanelProps> = ({
  tasks,
  onTaskSelect
}) => {
  // Use camelCase for variables and functions
  const [selectedTask, setSelectedTask] = useState<Task | null>(null);
  
  const handleTaskClick = useCallback((task: Task) => {
    setSelectedTask(task);
    onTaskSelect?.(task);
  }, [onTaskSelect]);
  
  return (
    <div className="task-management-panel">
      {/* Implementation */}
    </div>
  );
};

// Use kebab-case for CSS classes
.task-management-panel {
  display: flex;
  flex-direction: column;
}
```

## Testing Strategy

### Backend Testing
```csharp
// Unit Tests
[Fact]
public async Task CreateTask_WithValidData_ReturnsTaskDto()
{
    // Arrange
    var request = new CreateTaskRequest { Title = "Test Task" };
    var mockRepo = new Mock<IRepository<Task>>();
    var service = new TaskService(mockRepo.Object);
    
    // Act
    var result = await service.CreateTaskAsync(request);
    
    // Assert
    result.Should().NotBeNull();
    result.Title.Should().Be("Test Task");
}

// Integration Tests
[Fact]
public async Task CreateTask_EndToEnd_ReturnsCreatedTask()
{
    // Arrange
    using var factory = new TestWebApplicationFactory<Program>();
    var client = factory.CreateClient();
    
    // Act
    var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest
    {
        Title = "Integration Test Task"
    });
    
    // Assert
    response.Should().BeSuccessful();
    var task = await response.Content.ReadFromJsonAsync<TaskDto>();
    task.Title.Should().Be("Integration Test Task");
}
```

### Frontend Testing
```typescript
// Component Tests
describe('TaskManagementPanel', () => {
  it('renders tasks correctly', () => {
    const tasks = [
      { id: '1', title: 'Task 1', status: 'active' },
      { id: '2', title: 'Task 2', status: 'completed' }
    ];
    
    render(<TaskManagementPanel tasks={tasks} />);
    
    expect(screen.getByText('Task 1')).toBeInTheDocument();
    expect(screen.getByText('Task 2')).toBeInTheDocument();
  });
  
  it('calls onTaskSelect when task is clicked', () => {
    const onTaskSelect = jest.fn();
    const tasks = [{ id: '1', title: 'Task 1', status: 'active' }];
    
    render(<TaskManagementPanel tasks={tasks} onTaskSelect={onTaskSelect} />);
    
    fireEvent.click(screen.getByText('Task 1'));
    
    expect(onTaskSelect).toHaveBeenCalledWith(tasks[0]);
  });
});

// E2E Tests
describe('Task Management Flow', () => {
  it('should create and complete a task', async () => {
    await page.goto('/tasks');
    
    // Create task
    await page.click('[data-testid="create-task-button"]');
    await page.fill('[data-testid="task-title"]', 'E2E Test Task');
    await page.click('[data-testid="save-task-button"]');
    
    // Verify task appears
    await expect(page.locator('text=E2E Test Task')).toBeVisible();
    
    // Complete task
    await page.click('[data-testid="complete-task-button"]');
    await expect(page.locator('[data-testid="task-status"]')).toHaveText('Completed');
  });
});
```

## Database Development

### Entity Framework Migrations
```bash
# Create migration
dotnet ef migrations add MigrationName --project src/BARQ.Infrastructure --startup-project src/BARQ.API

# Update database
dotnet ef database update --project src/BARQ.Infrastructure --startup-project src/BARQ.API

# Remove last migration
dotnet ef migrations remove --project src/BARQ.Infrastructure --startup-project src/BARQ.API

# Generate SQL script
dotnet ef migrations script --project src/BARQ.Infrastructure --startup-project src/BARQ.API
```

### Entity Configuration
```csharp
public class TaskConfiguration : IEntityTypeConfiguration<Task>
{
    public void Configure(EntityTypeBuilder<Task> builder)
    {
        builder.ToTable("Tasks");
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(t => t.Description)
            .HasMaxLength(2000);
            
        builder.HasOne(t => t.AssignedUser)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasQueryFilter(t => !t.IsDeleted);
        
        builder.HasIndex(t => new { t.TenantId, t.Status });
    }
}
```

## API Development

### Controller Structure
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;
    
    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskDto>>> GetTasks(
        [FromQuery] ListRequest request)
    {
        try
        {
            var result = await _taskService.GetTasksAsync(request);
            return Ok(ApiResponse.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tasks");
            return StatusCode(500, ApiResponse.Error("Internal server error"));
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<TaskDto>> CreateTask(
        [FromBody] CreateTaskRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.Error("Invalid request data", ModelState));
        }
        
        try
        {
            var task = await _taskService.CreateTaskAsync(request);
            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, 
                ApiResponse.Success(task));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiResponse.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return StatusCode(500, ApiResponse.Error("Internal server error"));
        }
    }
}
```

### Service Implementation
```csharp
public class TaskService : ITaskService
{
    private readonly IRepository<Task> _taskRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMapper _mapper;
    private readonly ILogger<TaskService> _logger;
    
    public TaskService(
        IRepository<Task> taskRepository,
        ITenantProvider tenantProvider,
        IMapper mapper,
        ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository;
        _tenantProvider = tenantProvider;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<PagedResult<TaskDto>> GetTasksAsync(ListRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        
        var query = _taskRepository.Query()
            .Where(t => t.TenantId == tenantId);
            
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(t => t.Title.Contains(request.SearchTerm) ||
                                   t.Description.Contains(request.SearchTerm));
        }
        
        var totalCount = await query.CountAsync();
        
        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();
            
        var taskDtos = _mapper.Map<List<TaskDto>>(tasks);
        
        return new PagedResult<TaskDto>
        {
            Items = taskDtos,
            Total = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
```

## Frontend Development

### Component Structure
```typescript
// TaskManagementPanel.tsx
interface TaskManagementPanelProps {
  className?: string;
  onTaskSelect?: (task: Task) => void;
}

export const TaskManagementPanel: React.FC<TaskManagementPanelProps> = ({
  className,
  onTaskSelect
}) => {
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  const { data, isLoading, error: queryError } = useQuery({
    queryKey: ['tasks'],
    queryFn: () => api.tasks.list()
  });
  
  const createTaskMutation = useMutation({
    mutationFn: api.tasks.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      toast.success('Task created successfully');
    },
    onError: (error) => {
      toast.error('Failed to create task');
    }
  });
  
  const handleCreateTask = useCallback(async (taskData: CreateTaskRequest) => {
    try {
      await createTaskMutation.mutateAsync(taskData);
    } catch (error) {
      console.error('Error creating task:', error);
    }
  }, [createTaskMutation]);
  
  if (isLoading) return <LoadingSpinner />;
  if (queryError) return <ErrorMessage error={queryError} />;
  
  return (
    <div className={cn("task-management-panel", className)}>
      <TaskList 
        tasks={data?.items || []} 
        onTaskSelect={onTaskSelect}
      />
      <CreateTaskDialog onSubmit={handleCreateTask} />
    </div>
  );
};
```

### State Management
```typescript
// store/slices/taskSlice.ts
interface TaskState {
  tasks: Task[];
  selectedTask: Task | null;
  loading: boolean;
  error: string | null;
}

const initialState: TaskState = {
  tasks: [],
  selectedTask: null,
  loading: false,
  error: null
};

export const taskSlice = createSlice({
  name: 'tasks',
  initialState,
  reducers: {
    setTasks: (state, action: PayloadAction<Task[]>) => {
      state.tasks = action.payload;
    },
    selectTask: (state, action: PayloadAction<Task>) => {
      state.selectedTask = action.payload;
    },
    updateTask: (state, action: PayloadAction<Task>) => {
      const index = state.tasks.findIndex(t => t.id === action.payload.id);
      if (index !== -1) {
        state.tasks[index] = action.payload;
      }
    }
  }
});

export const { setTasks, selectTask, updateTask } = taskSlice.actions;
export default taskSlice.reducer;
```

## Debugging

### Backend Debugging
```csharp
// Add logging
_logger.LogInformation("Processing task {TaskId} for tenant {TenantId}", 
    taskId, tenantId);

// Use debugger
System.Diagnostics.Debugger.Break();

// Add conditional compilation
#if DEBUG
    Console.WriteLine($"Debug: Processing {tasks.Count} tasks");
#endif
```

### Frontend Debugging
```typescript
// Console logging
console.log('Task data:', task);
console.group('API Call');
console.log('Request:', request);
console.log('Response:', response);
console.groupEnd();

// React DevTools
// Install React Developer Tools browser extension

// Redux DevTools
// Install Redux DevTools browser extension
```

## Performance Optimization

### Backend Optimization
```csharp
// Use async/await properly
public async Task<List<TaskDto>> GetTasksAsync()
{
    var tasks = await _repository.GetAllAsync();
    return _mapper.Map<List<TaskDto>>(tasks);
}

// Optimize database queries
var tasks = await _context.Tasks
    .Include(t => t.AssignedUser)
    .Where(t => t.TenantId == tenantId)
    .Select(t => new TaskDto
    {
        Id = t.Id,
        Title = t.Title,
        AssignedUserName = t.AssignedUser.DisplayName
    })
    .ToListAsync();

// Use caching
[ResponseCache(Duration = 300)]
public async Task<ActionResult<List<TaskDto>>> GetTasks()
{
    // Implementation
}
```

### Frontend Optimization
```typescript
// Use React.memo for expensive components
export const TaskList = React.memo<TaskListProps>(({ tasks, onTaskSelect }) => {
  return (
    <div>
      {tasks.map(task => (
        <TaskItem key={task.id} task={task} onSelect={onTaskSelect} />
      ))}
    </div>
  );
});

// Use useCallback for event handlers
const handleTaskSelect = useCallback((task: Task) => {
  setSelectedTask(task);
  onTaskSelect?.(task);
}, [onTaskSelect]);

// Use useMemo for expensive calculations
const filteredTasks = useMemo(() => {
  return tasks.filter(task => 
    task.title.toLowerCase().includes(searchTerm.toLowerCase())
  );
}, [tasks, searchTerm]);
```

## Security Guidelines

### Authentication & Authorization
```csharp
// Use proper authorization attributes
[Authorize(Roles = "Administrator")]
public async Task<ActionResult> DeleteUser(Guid id)
{
    // Implementation
}

// Validate tenant access
public async Task<TaskDto> GetTaskAsync(Guid id)
{
    var tenantId = _tenantProvider.GetTenantId();
    var task = await _repository.GetByIdAsync(id);
    
    if (task.TenantId != tenantId)
    {
        throw new UnauthorizedAccessException();
    }
    
    return _mapper.Map<TaskDto>(task);
}
```

### Input Validation
```csharp
public class CreateTaskRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string? Description { get; set; }
    
    [Required]
    public TaskPriority Priority { get; set; }
}
```

## Deployment

### Local Development
```bash
# Start all services
docker-compose -f docker-compose.dev.yml up -d

# Run backend only
cd Backend
dotnet run --project src/BARQ.API

# Run frontend only
cd Frontend/barq-frontend
pnpm dev
```

### Production Build
```bash
# Build backend
cd Backend
dotnet publish src/BARQ.API -c Release -o publish

# Build frontend
cd Frontend/barq-frontend
pnpm build
```

## Troubleshooting

### Common Issues

#### Database Connection Issues
```bash
# Check connection string
dotnet ef dbcontext info --project src/BARQ.Infrastructure --startup-project src/BARQ.API

# Test connection
sqlcmd -S localhost -U sa -P password -Q "SELECT 1"
```

#### Frontend Build Issues
```bash
# Clear node modules
rm -rf node_modules package-lock.json
pnpm install

# Check TypeScript errors
pnpm type-check
```

#### Authentication Issues
```bash
# Check JWT configuration
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName": "admin@barq.com", "password": "Admin@123456"}'
```

## Resources

### Documentation
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [React Documentation](https://reactjs.org/docs/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Redux Toolkit](https://redux-toolkit.js.org/)

### Tools
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [VS Code](https://code.visualstudio.com/)
- [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/)
- [Postman](https://www.postman.com/)

### Team Resources
- **Slack**: #barq-development
- **Wiki**: https://wiki.barq.com/development
- **Code Reviews**: GitHub Pull Requests
- **Issue Tracking**: GitHub Issues
