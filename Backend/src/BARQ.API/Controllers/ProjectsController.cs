using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Models.Responses;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        private Guid GetCurrentTenantId()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : Guid.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<ProjectDto>>>> GetProjects([FromQuery] ListRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var result = await _projectService.GetProjectsAsync(tenantId, request);
                return Ok(ApiResponse<PagedResult<ProjectDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<PagedResult<ProjectDto>>.Fail(ex.Message));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetProject(Guid id)
        {
            try
            {
                var project = await _projectService.GetProjectByIdAsync(id);
                if (project == null)
                    return NotFound(ApiResponse<ProjectDto>.Fail("Project not found"));

                return Ok(ApiResponse<ProjectDto>.Ok(project));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.Fail(ex.Message));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateProject([FromBody] CreateProjectRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var project = await _projectService.CreateProjectAsync(tenantId, request);
                return CreatedAtAction(nameof(GetProject), new { id = project.Id }, 
                    ApiResponse<ProjectDto>.Ok(project, "Project created successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
        {
            try
            {
                var project = await _projectService.UpdateProjectAsync(id, request);
                return Ok(ApiResponse<ProjectDto>.Ok(project, "Project updated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProject(Guid id)
        {
            try
            {
                var result = await _projectService.DeleteProjectAsync(id);
                if (!result)
                    return NotFound(ApiResponse<bool>.Fail("Project not found"));

                return Ok(ApiResponse<bool>.Ok(true, "Project deleted successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.Fail(ex.Message));
            }
        }

        [HttpGet("templates")]
        public async Task<ActionResult<ApiResponse<List<ProjectDto>>>> GetProjectTemplates()
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var templates = await _projectService.GetProjectTemplatesAsync(tenantId);
                return Ok(ApiResponse<List<ProjectDto>>.Ok(templates));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<List<ProjectDto>>.Fail(ex.Message));
            }
        }

        [HttpPost("from-template")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateProjectFromTemplate([FromBody] CreateFromTemplateRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var project = await _projectService.CreateProjectFromTemplateAsync(tenantId, request.TemplateId, request.Name);
                return CreatedAtAction(nameof(GetProject), new { id = project.Id }, 
                    ApiResponse<ProjectDto>.Ok(project, "Project created from template successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<ProjectDto>.Fail(ex.Message));
            }
        }
    }

    public class CreateFromTemplateRequest
    {
        public Guid TemplateId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
