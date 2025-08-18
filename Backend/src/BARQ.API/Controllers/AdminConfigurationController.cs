using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Models.Responses;
using BARQ.Core.Entities;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Policy = "RequireAdministratorRole")]
    public class AdminConfigurationController : ControllerBase
    {
        private readonly IAdminConfigurationService _adminConfigurationService;

        public AdminConfigurationController(IAdminConfigurationService adminConfigurationService)
        {
            _adminConfigurationService = adminConfigurationService;
        }

        private Guid GetCurrentTenantId()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : Guid.Empty;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<AdminConfigurationDto>>>> GetConfigurations([FromQuery] AdminConfigurationListRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var result = await _adminConfigurationService.GetConfigurationsAsync(tenantId, request);
                return Ok(ApiResponse<PagedResult<AdminConfigurationDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<PagedResult<AdminConfigurationDto>>.ErrorResponse(ex.Message));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<AdminConfigurationDto>>> GetConfiguration(Guid id)
        {
            try
            {
                var configuration = await _adminConfigurationService.GetConfigurationByIdAsync(id);
                if (configuration == null)
                    return NotFound(ApiResponse<AdminConfigurationDto>.ErrorResponse("Configuration not found"));

                return Ok(ApiResponse<AdminConfigurationDto>.SuccessResponse(configuration));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<AdminConfigurationDto>.ErrorResponse(ex.Message));
            }
        }

        [HttpGet("by-key/{key}")]
        public async Task<ActionResult<ApiResponse<AdminConfigurationDto>>> GetConfigurationByKey(string key)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var configuration = await _adminConfigurationService.GetConfigurationByKeyAsync(tenantId, key);
                if (configuration == null)
                    return NotFound(ApiResponse<AdminConfigurationDto>.ErrorResponse("Configuration not found"));

                return Ok(ApiResponse<AdminConfigurationDto>.SuccessResponse(configuration));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<AdminConfigurationDto>.ErrorResponse(ex.Message));
            }
        }

        [HttpGet("by-category/{category}")]
        public async Task<ActionResult<ApiResponse<List<AdminConfigurationDto>>>> GetConfigurationsByCategory(string category)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var configurations = await _adminConfigurationService.GetConfigurationsByCategoryAsync(tenantId, category);
                return Ok(ApiResponse<List<AdminConfigurationDto>>.SuccessResponse(configurations));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<List<AdminConfigurationDto>>.ErrorResponse(ex.Message));
            }
        }

        [HttpGet("by-type/{type}")]
        public async Task<ActionResult<ApiResponse<List<AdminConfigurationDto>>>> GetConfigurationsByType(AdminConfigurationType type)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var configurations = await _adminConfigurationService.GetConfigurationsByTypeAsync(tenantId, type);
                return Ok(ApiResponse<List<AdminConfigurationDto>>.SuccessResponse(configurations));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<List<AdminConfigurationDto>>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<AdminConfigurationDto>>> CreateConfiguration([FromBody] CreateAdminConfigurationRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var configuration = await _adminConfigurationService.CreateConfigurationAsync(tenantId, request);
                return CreatedAtAction(nameof(GetConfiguration), new { id = configuration.Id }, 
                    ApiResponse<AdminConfigurationDto>.SuccessResponse(configuration, "Configuration created successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<AdminConfigurationDto>.ErrorResponse(ex.Message));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<AdminConfigurationDto>>> UpdateConfiguration(Guid id, [FromBody] UpdateAdminConfigurationRequest request)
        {
            try
            {
                var configuration = await _adminConfigurationService.UpdateConfigurationAsync(id, request);
                return Ok(ApiResponse<AdminConfigurationDto>.SuccessResponse(configuration, "Configuration updated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<AdminConfigurationDto>.ErrorResponse(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteConfiguration(Guid id)
        {
            try
            {
                var result = await _adminConfigurationService.DeleteConfigurationAsync(id);
                if (!result)
                    return NotFound(ApiResponse<bool>.ErrorResponse("Configuration not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Configuration deleted successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost("{id}/validate")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateConfiguration(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _adminConfigurationService.ValidateConfigurationAsync(id, userId);
                if (!result)
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Failed to validate configuration"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Configuration validated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost("{id}/test")]
        public async Task<ActionResult<ApiResponse<bool>>> TestConfiguration(Guid id)
        {
            try
            {
                var result = await _adminConfigurationService.TestConfigurationAsync(id);
                return Ok(ApiResponse<bool>.SuccessResponse(result, result ? "Configuration test passed" : "Configuration test failed"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
        }
    }
}
