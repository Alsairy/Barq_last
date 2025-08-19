using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface IProjectService
    {
        Task<PagedResult<ProjectDto>> GetProjectsAsync(Guid tenantId, ListRequest request);
        Task<ProjectDto?> GetProjectByIdAsync(Guid id);
        Task<ProjectDto> CreateProjectAsync(Guid tenantId, CreateProjectRequest request);
        Task<ProjectDto> UpdateProjectAsync(Guid id, UpdateProjectRequest request);
        Task<bool> DeleteProjectAsync(Guid id);
        Task<bool> UpdateProjectStatusAsync(Guid projectId, string status);
        Task<bool> UpdateProjectProgressAsync(Guid projectId, decimal progressPercentage);
        Task<List<ProjectDto>> GetProjectTemplatesAsync(Guid tenantId);
        Task<ProjectDto> CreateProjectFromTemplateAsync(Guid tenantId, Guid templateId, string name);
        Task<BulkOperationResult> BulkDeleteProjectsAsync(BulkDeleteRequest request);
    }
}
