using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BARQ.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly BarqDbContext _context;

        public TaskService(BarqDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<TaskDto>> GetTasksAsync(Guid tenantId, TaskListRequest request)
        {
            var query = _context.Tasks
                .Where(t => t.TenantId == tenantId)
                .AsQueryable();

            if (request.ProjectId.HasValue)
                query = query.Where(t => t.ProjectId == request.ProjectId);

            if (request.AssignedToId.HasValue)
                query = query.Where(t => t.AssignedToId == request.AssignedToId);

            if (!string.IsNullOrEmpty(request.Status))
                query = query.Where(t => t.Status == request.Status);

            if (!string.IsNullOrEmpty(request.Priority))
                query = query.Where(t => t.Priority == request.Priority);

            if (!string.IsNullOrEmpty(request.TaskType))
                query = query.Where(t => t.TaskType == request.TaskType);

            if (request.DueDateFrom.HasValue)
                query = query.Where(t => t.DueDate >= request.DueDateFrom);

            if (request.DueDateTo.HasValue)
                query = query.Where(t => t.DueDate <= request.DueDateTo);

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(t => t.Name.Contains(request.SearchTerm) ||
                                        (t.Description != null && t.Description.Contains(request.SearchTerm)) ||
                                        (t.Requirements != null && t.Requirements.Contains(request.SearchTerm)));
            }

            if (!string.IsNullOrEmpty(request.Tags))
                query = query.Where(t => t.Tags != null && t.Tags.Contains(request.Tags));

            var totalCount = await query.CountAsync();

            query = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                "status" => request.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
                "priority" => request.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
                "duedate" => request.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
                "progress" => request.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(t => t.ProgressPercentage) : query.OrderBy(t => t.ProgressPercentage),
                _ => request.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt)
            };

            var tasks = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Include(t => t.AssignedTo)
                .Include(t => t.Project)
                .Include(t => t.ParentTask)
                .Select(t => new TaskDto
                {
                    Id = t.Id,
                    TenantId = t.TenantId,
                    Name = t.Name,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    TaskType = t.TaskType,
                    AssignedToId = t.AssignedToId,
                    AssignedToName = t.AssignedTo != null ? $"{t.AssignedTo.FirstName} {t.AssignedTo.LastName}" : null,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project != null ? t.Project.Name : null,
                    ParentTaskId = t.ParentTaskId,
                    ParentTaskName = t.ParentTask != null ? t.ParentTask.Name : null,
                    DueDate = t.DueDate,
                    StartDate = t.StartDate,
                    CompletedDate = t.CompletedDate,
                    EstimatedHours = t.EstimatedHours,
                    ActualHours = t.ActualHours,
                    ProgressPercentage = t.ProgressPercentage,
                    Requirements = t.Requirements,
                    AcceptanceCriteria = t.AcceptanceCriteria,
                    Notes = t.Notes,
                    Tags = t.Tags,
                    IsRecurring = t.IsRecurring,
                    RecurrencePattern = t.RecurrencePattern,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return new PagedResult<TaskDto>
            {
                Items = tasks,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<TaskDto?> GetTaskByIdAsync(Guid id)
        {
            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.Project)
                .Include(t => t.ParentTask)
                .Include(t => t.SubTasks)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return null;

            return new TaskDto
            {
                Id = task.Id,
                TenantId = task.TenantId,
                Name = task.Name,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                TaskType = task.TaskType,
                AssignedToId = task.AssignedToId,
                AssignedToName = task.AssignedTo != null ? $"{task.AssignedTo.FirstName} {task.AssignedTo.LastName}" : null,
                ProjectId = task.ProjectId,
                ProjectName = task.Project?.Name,
                ParentTaskId = task.ParentTaskId,
                ParentTaskName = task.ParentTask?.Name,
                DueDate = task.DueDate,
                StartDate = task.StartDate,
                CompletedDate = task.CompletedDate,
                EstimatedHours = task.EstimatedHours,
                ActualHours = task.ActualHours,
                ProgressPercentage = task.ProgressPercentage,
                Requirements = task.Requirements,
                AcceptanceCriteria = task.AcceptanceCriteria,
                Notes = task.Notes,
                Tags = task.Tags,
                IsRecurring = task.IsRecurring,
                RecurrencePattern = task.RecurrencePattern,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                SubTasks = task.SubTasks.Select(st => new TaskDto
                {
                    Id = st.Id,
                    Name = st.Name,
                    Status = st.Status,
                    Priority = st.Priority,
                    ProgressPercentage = st.ProgressPercentage,
                    DueDate = st.DueDate
                }).ToList()
            };
        }

        public async Task<TaskDto> CreateTaskAsync(Guid tenantId, CreateTaskRequest request)
        {
            var task = new Core.Entities.Task
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = request.Name,
                Description = request.Description,
                Status = request.Status,
                Priority = request.Priority,
                TaskType = request.TaskType,
                AssignedToId = request.AssignedToId,
                ProjectId = request.ProjectId,
                ParentTaskId = request.ParentTaskId,
                DueDate = request.DueDate,
                StartDate = request.StartDate,
                EstimatedHours = request.EstimatedHours,
                Requirements = request.Requirements,
                AcceptanceCriteria = request.AcceptanceCriteria,
                Notes = request.Notes,
                Tags = request.Tags,
                IsRecurring = request.IsRecurring,
                RecurrencePattern = request.RecurrencePattern,
                CreatedAt = DateTime.UtcNow,
                Version = 1
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return await GetTaskByIdAsync(task.Id) ?? throw new InvalidOperationException("Failed to retrieve created task");
        }

        public async Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskRequest request)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
                throw new ArgumentException("Task not found");

            task.Name = request.Name;
            task.Description = request.Description;
            task.Status = request.Status;
            task.Priority = request.Priority;
            task.AssignedToId = request.AssignedToId;
            task.DueDate = request.DueDate;
            task.StartDate = request.StartDate;
            task.CompletedDate = request.CompletedDate;
            task.EstimatedHours = request.EstimatedHours;
            task.ActualHours = request.ActualHours;
            task.ProgressPercentage = request.ProgressPercentage;
            task.Requirements = request.Requirements;
            task.AcceptanceCriteria = request.AcceptanceCriteria;
            task.Notes = request.Notes;
            task.Tags = request.Tags;
            task.IsRecurring = request.IsRecurring;
            task.RecurrencePattern = request.RecurrencePattern;
            task.UpdatedAt = DateTime.UtcNow;

            if (request.Status == "Completed" && task.CompletedDate == null)
                task.CompletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetTaskByIdAsync(task.Id) ?? throw new InvalidOperationException("Failed to retrieve updated task");
        }

        public async Task<bool> DeleteTaskAsync(Guid id)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return false;

            task.IsDeleted = true;
            task.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AssignTaskAsync(Guid taskId, Guid userId)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return false;

            task.AssignedToId = userId;
            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnassignTaskAsync(Guid taskId)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return false;

            task.AssignedToId = null;
            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateTaskStatusAsync(Guid taskId, string status)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return false;

            task.Status = status;
            task.UpdatedAt = DateTime.UtcNow;

            if (status == "Completed" && task.CompletedDate == null)
                task.CompletedDate = DateTime.UtcNow;
            else if (status != "Completed")
                task.CompletedDate = null;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateTaskProgressAsync(Guid taskId, decimal progressPercentage)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return false;

            task.ProgressPercentage = Math.Max(0, Math.Min(100, progressPercentage));
            task.UpdatedAt = DateTime.UtcNow;

            if (task.ProgressPercentage >= 100 && task.Status != "Completed")
            {
                task.Status = "Completed";
                task.CompletedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<TaskDto>> GetSubTasksAsync(Guid parentTaskId)
        {
            var subTasks = await _context.Tasks
                .Where(t => t.ParentTaskId == parentTaskId)
                .Include(t => t.AssignedTo)
                .Select(t => new TaskDto
                {
                    Id = t.Id,
                    TenantId = t.TenantId,
                    Name = t.Name,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    TaskType = t.TaskType,
                    AssignedToId = t.AssignedToId,
                    AssignedToName = t.AssignedTo != null ? $"{t.AssignedTo.FirstName} {t.AssignedTo.LastName}" : null,
                    DueDate = t.DueDate,
                    ProgressPercentage = t.ProgressPercentage,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return subTasks;
        }

        public async Task<List<TaskDto>> GetTasksByProjectAsync(Guid projectId)
        {
            var tasks = await _context.Tasks
                .Where(t => t.ProjectId == projectId)
                .Include(t => t.AssignedTo)
                .Select(t => new TaskDto
                {
                    Id = t.Id,
                    TenantId = t.TenantId,
                    Name = t.Name,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    TaskType = t.TaskType,
                    AssignedToId = t.AssignedToId,
                    AssignedToName = t.AssignedTo != null ? $"{t.AssignedTo.FirstName} {t.AssignedTo.LastName}" : null,
                    DueDate = t.DueDate,
                    ProgressPercentage = t.ProgressPercentage,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return tasks;
        }

        public async Task<List<TaskDto>> GetTasksByUserAsync(Guid userId)
        {
            var tasks = await _context.Tasks
                .Where(t => t.AssignedToId == userId)
                .Include(t => t.Project)
                .Select(t => new TaskDto
                {
                    Id = t.Id,
                    TenantId = t.TenantId,
                    Name = t.Name,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    TaskType = t.TaskType,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project != null ? t.Project.Name : null,
                    DueDate = t.DueDate,
                    ProgressPercentage = t.ProgressPercentage,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return tasks;
        }

        public async Task<BulkOperationResult> BulkDeleteTasksAsync(BulkDeleteRequest request)
        {
            var tasks = await _context.Tasks
                .Where(t => request.Ids.Contains(t.Id))
                .ToListAsync();

            var successCount = 0;
            var errors = new List<string>();

            foreach (var task in tasks)
            {
                try
                {
                    task.IsDeleted = true;
                    task.DeletedAt = DateTime.UtcNow;
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to delete task {task.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return new BulkOperationResult
            {
                SuccessCount = successCount,
                FailureCount = request.Ids.Count - successCount,
                Errors = errors
            };
        }

        public async Task<BulkOperationResult> BulkUpdateTaskStatusAsync(List<Guid> taskIds, string status)
        {
            var tasks = await _context.Tasks
                .Where(t => taskIds.Contains(t.Id))
                .ToListAsync();

            var successCount = 0;
            var errors = new List<string>();

            foreach (var task in tasks)
            {
                try
                {
                    task.Status = status;
                    task.UpdatedAt = DateTime.UtcNow;

                    if (status == "Completed" && task.CompletedDate == null)
                        task.CompletedDate = DateTime.UtcNow;
                    else if (status != "Completed")
                        task.CompletedDate = null;

                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to update task {task.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return new BulkOperationResult
            {
                SuccessCount = successCount,
                FailureCount = taskIds.Count - successCount,
                Errors = errors
            };
        }
    }
}
