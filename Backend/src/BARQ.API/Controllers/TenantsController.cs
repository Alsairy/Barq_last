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
    public class TenantsController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public TenantsController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpGet]
        [Authorize(Policy = "RequireAdministratorRole")]
        public async Task<ActionResult<ApiResponse<PagedResult<TenantDto>>>> GetTenants([FromQuery] ListRequest request)
        {
            try
            {
                var result = await _tenantService.GetTenantsAsync(request);
                return Ok(ApiResponse<PagedResult<TenantDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<PagedResult<TenantDto>>.Fail(ex.Message));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<TenantDto>>> GetTenant(Guid id)
        {
            try
            {
                var tenant = await _tenantService.GetTenantByIdAsync(id);
                if (tenant == null)
                    return NotFound(ApiResponse<TenantDto>.Fail("Tenant not found"));

                return Ok(ApiResponse<TenantDto>.Ok(tenant));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<TenantDto>.Fail(ex.Message));
            }
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdministratorRole")]
        public async Task<ActionResult<ApiResponse<TenantDto>>> CreateTenant([FromBody] CreateTenantRequest request)
        {
            try
            {
                var tenant = await _tenantService.CreateTenantAsync(request);
                return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, 
                    ApiResponse<TenantDto>.Ok(tenant, "Tenant created successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<TenantDto>.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdministratorRole")]
        public async Task<ActionResult<ApiResponse<TenantDto>>> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
        {
            try
            {
                var tenant = await _tenantService.UpdateTenantAsync(id, request);
                return Ok(ApiResponse<TenantDto>.Ok(tenant, "Tenant updated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<TenantDto>.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdministratorRole")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteTenant(Guid id)
        {
            try
            {
                var result = await _tenantService.DeleteTenantAsync(id);
                if (!result)
                    return NotFound(ApiResponse<bool>.Fail("Tenant not found"));

                return Ok(ApiResponse<bool>.Ok(true, "Tenant deleted successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.Fail(ex.Message));
            }
        }

        [HttpPost("{id}/activate")]
        [Authorize(Policy = "RequireAdministratorRole")]
        public async Task<ActionResult<ApiResponse<bool>>> ActivateTenant(Guid id)
        {
            try
            {
                var result = await _tenantService.ActivateTenantAsync(id);
                if (!result)
                    return NotFound(ApiResponse<bool>.Fail("Tenant not found"));

                return Ok(ApiResponse<bool>.Ok(true, "Tenant activated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.Fail(ex.Message));
            }
        }

        [HttpPost("{id}/deactivate")]
        [Authorize(Policy = "RequireAdministratorRole")]
        public async Task<ActionResult<ApiResponse<bool>>> DeactivateTenant(Guid id)
        {
            try
            {
                var result = await _tenantService.DeactivateTenantAsync(id);
                if (!result)
                    return NotFound(ApiResponse<bool>.Fail("Tenant not found"));

                return Ok(ApiResponse<bool>.Ok(true, "Tenant deactivated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.Fail(ex.Message));
            }
        }
    }
}
