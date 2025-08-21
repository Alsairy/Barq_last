using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdministratorRole")]
    public class AdminConstraintsController : ControllerBase
    {
        private readonly ITemplateValidationService _templateValidationService;
        private readonly ILogger<AdminConstraintsController> _logger;

        public AdminConstraintsController(
            ITemplateValidationService templateValidationService,
            ILogger<AdminConstraintsController> logger)
        {
            _templateValidationService = templateValidationService;
            _logger = logger;
        }

        [HttpPost("validate-template/{templateId}")]
        public async Task<IActionResult> ValidateTemplate(Guid templateId)
        {
            try
            {
                var result = await _templateValidationService.ValidateTemplateAsync(templateId);
                return Ok(new
                {
                    IsValid = result.IsValid,
                    Errors = result.Errors,
                    Warnings = result.Warnings,
                    ValidatedAt = result.ValidatedAt,
                    ValidationVersion = result.ValidationVersion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template {TemplateId}", templateId);
                return StatusCode(500, new { Message = "Internal server error during template validation" });
            }
        }

        [HttpPost("validate-template-content")]
        public async Task<IActionResult> ValidateTemplateContent([FromBody] ValidateTemplateContentRequest request)
        {
            try
            {
                var result = await _templateValidationService.ValidateTemplateContentAsync(request.Content, request.TemplateType);
                return Ok(new
                {
                    IsValid = result.IsValid,
                    Errors = result.Errors,
                    Warnings = result.Warnings,
                    ValidatedAt = result.ValidatedAt,
                    ValidationVersion = result.ValidationVersion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template content");
                return StatusCode(500, new { Message = "Internal server error during template content validation" });
            }
        }

        [HttpPost("test-connection")]
        public async Task<IActionResult> TestConnection([FromBody] TestConnectionRequest request)
        {
            try
            {
                var isConnected = await _templateValidationService.TestConnectionAsync(request.ConnectionString, request.ConnectionType);
                return Ok(new
                {
                    IsConnected = isConnected,
                    ConnectionType = request.ConnectionType,
                    TestedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection {ConnectionType}", request.ConnectionType);
                return StatusCode(500, new { Message = "Internal server error during connection test" });
            }
        }

        [HttpGet("violations")]
        public async Task<IActionResult> GetViolations([FromQuery] Guid? templateId = null, [FromQuery] Guid? taskId = null)
        {
            try
            {
                var violations = await _templateValidationService.GetViolationsAsync(templateId, taskId);
                return Ok(violations.Select(v => new
                {
                    v.Id,
                    v.TemplateId,
                    v.TaskId,
                    ConstraintName = v.Constraint.Name,
                    v.ViolationType,
                    v.ViolationDetails,
                    v.Severity,
                    v.IsResolved,
                    v.ResolvedAt,
                    ResolvedBy = v.ResolvedByUser?.UserName,
                    v.ResolutionNotes,
                    v.CreatedAt
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving violations");
                return StatusCode(500, new { Message = "Internal server error retrieving violations" });
            }
        }

        [HttpPost("violations/{violationId}/resolve")]
        public async Task<IActionResult> ResolveViolation(Guid violationId, [FromBody] ResolveViolationRequest request)
        {
            try
            {
                var success = await _templateValidationService.ResolveViolationAsync(violationId, request.ResolutionNotes);
                if (success)
                {
                    return Ok(new { Message = "Violation resolved successfully" });
                }
                return NotFound(new { Message = "Violation not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving violation {ViolationId}", violationId);
                return StatusCode(500, new { Message = "Internal server error resolving violation" });
            }
        }

        [HttpGet("constraints")]
        public async Task<IActionResult> GetConstraints()
        {
            try
            {
                var constraints = await _templateValidationService.GetActiveConstraintsAsync();
                return Ok(constraints.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.ConstraintType,
                    c.AllowedValues,
                    c.DeniedValues,
                    c.IsActive,
                    c.Priority,
                    c.ValidationRule,
                    c.CreatedAt,
                    c.UpdatedAt
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving constraints");
                return StatusCode(500, new { Message = "Internal server error retrieving constraints" });
            }
        }

        [HttpPost("constraints")]
        public async Task<IActionResult> CreateConstraint([FromBody] CreateTechnologyConstraintRequest request)
        {
            try
            {
                var constraint = await _templateValidationService.CreateConstraintAsync(request);
                return CreatedAtAction(nameof(GetConstraints), new { id = constraint.Id }, new
                {
                    constraint.Id,
                    constraint.Name,
                    constraint.Description,
                    constraint.ConstraintType,
                    constraint.AllowedValues,
                    constraint.DeniedValues,
                    constraint.IsActive,
                    constraint.Priority,
                    constraint.ValidationRule,
                    constraint.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating constraint");
                return StatusCode(500, new { Message = "Internal server error creating constraint" });
            }
        }

        [HttpPut("constraints/{constraintId}")]
        public async Task<IActionResult> UpdateConstraint(Guid constraintId, [FromBody] UpdateTechnologyConstraintRequest request)
        {
            try
            {
                var constraint = await _templateValidationService.UpdateConstraintAsync(constraintId, request);
                return Ok(new
                {
                    constraint.Id,
                    constraint.Name,
                    constraint.Description,
                    constraint.ConstraintType,
                    constraint.AllowedValues,
                    constraint.DeniedValues,
                    constraint.IsActive,
                    constraint.Priority,
                    constraint.ValidationRule,
                    constraint.UpdatedAt
                });
            }
            catch (ArgumentException)
            {
                return NotFound(new { Message = "Constraint not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating constraint {ConstraintId}", constraintId);
                return StatusCode(500, new { Message = "Internal server error updating constraint" });
            }
        }

        [HttpDelete("constraints/{constraintId}")]
        public async Task<IActionResult> DeleteConstraint(Guid constraintId)
        {
            try
            {
                var success = await _templateValidationService.DeleteConstraintAsync(constraintId);
                if (success)
                {
                    return Ok(new { Message = "Constraint deleted successfully" });
                }
                return NotFound(new { Message = "Constraint not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting constraint {ConstraintId}", constraintId);
                return StatusCode(500, new { Message = "Internal server error deleting constraint" });
            }
        }
    }

    public class ValidateTemplateContentRequest
    {
        public string Content { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty;
    }

    public class TestConnectionRequest
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ConnectionType { get; set; } = string.Empty;
    }

    public class ResolveViolationRequest
    {
        public string ResolutionNotes { get; set; } = string.Empty;
    }
}
