using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BARQ.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly BarqDbContext _context;

        public ProjectService(BarqDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ProjectDto>> GetProjectsAsync(Guid tenantId, ListRequest request)
        {
            var query = _context.Projects
                .Where(p => p.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(p => p.Name.Contains(request.SearchTerm) ||
                                        (p.Description != null && p.Description.Contains(request.SearchTerm)) ||
                                        (p.Objectives != null && p.Objectives.Contains(request.SearchTerm)));
            }

            var totalCount = await query.CountAsync();

            var projects = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Include(p => p.Owner)
                .Select(p => new ProjectDto
                {
                    Id = p.Id,
                    TenantId = p.TenantId,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    Priority = p.Priority,
                    OwnerId = p.OwnerId,
                    OwnerName = p.Owner != null ? $"{p.Owner.FirstName} {p.Owner.LastName}" : null,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    ActualStartDate = p.ActualStartDate,
                    ActualEndDate = p.ActualEndDate,
                    Budget = p.Budget,
                    ActualCost = p.ActualCost,
                    ProgressPercentage = p.ProgressPercentage,
                    Objectives = p.Objectives,
                    Scope = p.Scope,
                    Deliverables = p.Deliverables,
                    Stakeholders = p.Stakeholders,
                    Risks = p.Risks,
                    Tags = p.Tags,
                    IsTemplate = p.IsTemplate,
                    TemplateId = p.TemplateId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return new PagedResult<ProjectDto>
            {
                Items = projects,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<ProjectDto?> GetProjectByIdAsync(Guid id)
        {
            var project = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Tasks)
                .ThenInclude(t => t.AssignedTo)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return null;

            return new ProjectDto
            {
                Id = project.Id,
                TenantId = project.TenantId,
                Name = project.Name,
                Description = project.Description,
                Status = project.Status,
                Priority = project.Priority,
                OwnerId = project.OwnerId,
                OwnerName = project.Owner != null ? $"{project.Owner.FirstName} {project.Owner.LastName}" : null,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                ActualStartDate = project.ActualStartDate,
                ActualEndDate = project.ActualEndDate,
                Budget = project.Budget,
                ActualCost = project.ActualCost,
                ProgressPercentage = project.ProgressPercentage,
                Objectives = project.Objectives,
                Scope = project.Scope,
                Deliverables = project.Deliverables,
                Stakeholders = project.Stakeholders,
                Risks = project.Risks,
                Tags = project.Tags,
                IsTemplate = project.IsTemplate,
                TemplateId = project.TemplateId,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                Tasks = project.Tasks.Select(t => new TaskDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Status = t.Status,
                    Priority = t.Priority,
                    AssignedToId = t.AssignedToId,
                    AssignedToName = t.AssignedTo != null ? $"{t.AssignedTo.FirstName} {t.AssignedTo.LastName}" : null,
                    DueDate = t.DueDate,
                    ProgressPercentage = t.ProgressPercentage
                }).ToList()
            };
        }

        public async Task<ProjectDto> CreateProjectAsync(Guid tenantId, CreateProjectRequest request)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = request.Name,
                Description = request.Description,
                Status = request.Status,
                Priority = request.Priority,
                OwnerId = request.OwnerId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Budget = request.Budget,
                Objectives = request.Objectives,
                Scope = request.Scope,
                Deliverables = request.Deliverables,
                Stakeholders = request.Stakeholders,
                Risks = request.Risks,
                Tags = request.Tags,
                IsTemplate = request.IsTemplate,
                TemplateId = request.TemplateId,
                CreatedAt = DateTime.UtcNow,
                Version = 1
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return await GetProjectByIdAsync(project.Id) ?? throw new InvalidOperationException("Failed to retrieve created project");
        }

        public async Task<ProjectDto> UpdateProjectAsync(Guid id, UpdateProjectRequest request)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
                throw new ArgumentException("Project not found");

            project.Name = request.Name;
            project.Description = request.Description;
            project.Status = request.Status;
            project.Priority = request.Priority;
            project.OwnerId = request.OwnerId;
            project.StartDate = request.StartDate;
            project.EndDate = request.EndDate;
            project.ActualStartDate = request.ActualStartDate;
            project.ActualEndDate = request.ActualEndDate;
            project.Budget = request.Budget;
            project.ActualCost = request.ActualCost;
            project.ProgressPercentage = request.ProgressPercentage;
            project.Objectives = request.Objectives;
            project.Scope = request.Scope;
            project.Deliverables = request.Deliverables;
            project.Stakeholders = request.Stakeholders;
            project.Risks = request.Risks;
            project.Tags = request.Tags;
            project.UpdatedAt = DateTime.UtcNow;

            if (request.Status == "Completed" && project.ActualEndDate == null)
                project.ActualEndDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetProjectByIdAsync(project.Id) ?? throw new InvalidOperationException("Failed to retrieve updated project");
        }

        public async Task<bool> DeleteProjectAsync(Guid id)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return false;

            project.IsDeleted = true;
            project.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateProjectStatusAsync(Guid projectId, string status)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return false;

            project.Status = status;
            project.UpdatedAt = DateTime.UtcNow;

            if (status == "Completed" && project.ActualEndDate == null)
                project.ActualEndDate = DateTime.UtcNow;
            else if (status == "In Progress" && project.ActualStartDate == null)
                project.ActualStartDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateProjectProgressAsync(Guid projectId, decimal progressPercentage)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return false;

            project.ProgressPercentage = Math.Max(0, Math.Min(100, progressPercentage));
            project.UpdatedAt = DateTime.UtcNow;

            if (project.ProgressPercentage >= 100 && project.Status != "Completed")
            {
                project.Status = "Completed";
                project.ActualEndDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<ProjectDto>> GetProjectTemplatesAsync(Guid tenantId)
        {
            var templates = await _context.Projects
                .Where(p => p.TenantId == tenantId && p.IsTemplate)
                .Include(p => p.Owner)
                .Select(p => new ProjectDto
                {
                    Id = p.Id,
                    TenantId = p.TenantId,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    Priority = p.Priority,
                    OwnerId = p.OwnerId,
                    OwnerName = p.Owner != null ? $"{p.Owner.FirstName} {p.Owner.LastName}" : null,
                    Budget = p.Budget,
                    Objectives = p.Objectives,
                    Scope = p.Scope,
                    Deliverables = p.Deliverables,
                    Stakeholders = p.Stakeholders,
                    Risks = p.Risks,
                    Tags = p.Tags,
                    IsTemplate = p.IsTemplate,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return templates;
        }

        public async Task<ProjectDto> CreateProjectFromTemplateAsync(Guid tenantId, Guid templateId, string name)
        {
            var template = await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == templateId && p.IsTemplate);

            if (template == null)
                throw new ArgumentException("Template not found");

            var project = new Project
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = name,
                Description = template.Description,
                Status = "Planning",
                Priority = template.Priority,
                Budget = template.Budget,
                Objectives = template.Objectives,
                Scope = template.Scope,
                Deliverables = template.Deliverables,
                Stakeholders = template.Stakeholders,
                Risks = template.Risks,
                Tags = template.Tags,
                IsTemplate = false,
                TemplateId = templateId,
                CreatedAt = DateTime.UtcNow,
                Version = 1
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            foreach (var templateTask in template.Tasks)
            {
                var task = new Core.Entities.Task
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProjectId = project.Id,
                    Name = templateTask.Name,
                    Description = templateTask.Description,
                    Status = "Draft",
                    Priority = templateTask.Priority,
                    TaskType = templateTask.TaskType,
                    EstimatedHours = templateTask.EstimatedHours,
                    Requirements = templateTask.Requirements,
                    AcceptanceCriteria = templateTask.AcceptanceCriteria,
                    Tags = templateTask.Tags,
                    IsRecurring = templateTask.IsRecurring,
                    RecurrencePattern = templateTask.RecurrencePattern,
                    CreatedAt = DateTime.UtcNow,
                    Version = 1
                };

                _context.Tasks.Add(task);
            }

            await _context.SaveChangesAsync();

            return await GetProjectByIdAsync(project.Id) ?? throw new InvalidOperationException("Failed to retrieve created project");
        }

        public async Task<BulkOperationResult> BulkDeleteProjectsAsync(BulkDeleteRequest request)
        {
            var projects = await _context.Projects
                .Where(p => request.Ids.Contains(p.Id))
                .ToListAsync();

            var successCount = 0;
            var errors = new List<string>();

            foreach (var project in projects)
            {
                try
                {
                    project.IsDeleted = true;
                    project.DeletedAt = DateTime.UtcNow;
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to delete project {project.Id}: {ex.Message}");
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
    }
}
