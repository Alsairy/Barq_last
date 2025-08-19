using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ImpersonationController : ControllerBase
    {
        private readonly IImpersonationService _impersonationService;
        private readonly ILogger<ImpersonationController> _logger;

        public ImpersonationController(IImpersonationService impersonationService, ILogger<ImpersonationController> logger)
        {
            _impersonationService = impersonationService;
            _logger = logger;
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<PagedResult<ImpersonationSessionDto>>> GetImpersonationSessions([FromQuery] ListRequest request)
        {
            try
            {
                var result = await _impersonationService.GetImpersonationSessionsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting impersonation sessions");
                return StatusCode(500, "An error occurred while retrieving impersonation sessions");
            }
        }

        [HttpGet("sessions/{id}")]
        public async Task<ActionResult<ImpersonationSessionDto>> GetImpersonationSession(Guid id)
        {
            try
            {
                var session = await _impersonationService.GetImpersonationSessionByIdAsync(id);
                if (session == null)
                {
                    return NotFound();
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting impersonation session: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the impersonation session");
            }
        }

        [HttpPost("sessions")]
        public async Task<ActionResult<ImpersonationSessionDto>> StartImpersonation([FromBody] CreateImpersonationSessionRequest request)
        {
            try
            {
                var adminUserId = User.Identity?.Name ?? throw new UnauthorizedAccessException("User not authenticated");
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = Request.Headers.UserAgent.ToString();

                var session = await _impersonationService.StartImpersonationAsync(request, adminUserId, ipAddress, userAgent);
                return CreatedAtAction(nameof(GetImpersonationSession), new { id = session.Id }, session);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting impersonation session");
                return StatusCode(500, "An error occurred while starting the impersonation session");
            }
        }

        [HttpPost("sessions/{id}/end")]
        public async Task<ActionResult> EndImpersonation(Guid id, [FromBody] EndImpersonationSessionRequest request)
        {
            try
            {
                var endedBy = User.Identity?.Name ?? "Unknown";
                var success = await _impersonationService.EndImpersonationAsync(id, request, endedBy);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending impersonation session: {Id}", id);
                return StatusCode(500, "An error occurred while ending the impersonation session");
            }
        }

        [HttpGet("sessions/active")]
        public async Task<ActionResult<List<ImpersonationSessionDto>>> GetActiveImpersonationSessions()
        {
            try
            {
                var sessions = await _impersonationService.GetActiveImpersonationSessionsAsync();
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active impersonation sessions");
                return StatusCode(500, "An error occurred while retrieving active impersonation sessions");
            }
        }

        [HttpGet("sessions/{id}/actions")]
        public async Task<ActionResult<PagedResult<ImpersonationActionDto>>> GetImpersonationActions(Guid id, [FromQuery] ListRequest request)
        {
            try
            {
                var result = await _impersonationService.GetImpersonationActionsAsync(id, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting impersonation actions for session: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving impersonation actions");
            }
        }

        [HttpPost("validate-token")]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> ValidateImpersonationToken([FromBody] ValidateTokenRequest request)
        {
            try
            {
                var isValid = await _impersonationService.ValidateImpersonationTokenAsync(request.Token);
                return Ok(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating impersonation token");
                return StatusCode(500, "An error occurred while validating the impersonation token");
            }
        }

        [HttpGet("token/{token}")]
        [AllowAnonymous]
        public async Task<ActionResult<ImpersonationSessionDto>> GetActiveImpersonationByToken(string token)
        {
            try
            {
                var session = await _impersonationService.GetActiveImpersonationByTokenAsync(token);
                if (session == null)
                {
                    return NotFound();
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active impersonation by token");
                return StatusCode(500, "An error occurred while retrieving the impersonation session");
            }
        }

        [HttpPost("expire-old-sessions")]
        public async Task<ActionResult> ExpireOldSessions()
        {
            try
            {
                await _impersonationService.ExpireOldSessionsAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring old impersonation sessions");
                return StatusCode(500, "An error occurred while expiring old sessions");
            }
        }

        [HttpGet("can-impersonate/{userId}/tenant/{tenantId}")]
        public async Task<ActionResult<bool>> CanUserBeImpersonated(Guid userId, Guid tenantId)
        {
            try
            {
                var canImpersonate = await _impersonationService.CanUserBeImpersonatedAsync(userId, tenantId);
                return Ok(canImpersonate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can be impersonated: {UserId}", userId);
                return StatusCode(500, "An error occurred while checking impersonation eligibility");
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<Dictionary<string, object>>> GetImpersonationStats()
        {
            try
            {
                var stats = await _impersonationService.GetImpersonationStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting impersonation stats");
                return StatusCode(500, "An error occurred while retrieving impersonation statistics");
            }
        }
    }

    public class ValidateTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
