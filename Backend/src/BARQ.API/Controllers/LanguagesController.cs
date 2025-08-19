using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LanguagesController : ControllerBase
    {
        private readonly ILanguageService _languageService;
        private readonly ILogger<LanguagesController> _logger;

        public LanguagesController(ILanguageService languageService, ILogger<LanguagesController> logger)
        {
            _languageService = languageService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<LanguageDto>>> GetLanguages([FromQuery] ListRequest request)
        {
            try
            {
                var result = await _languageService.GetLanguagesAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting languages");
                return StatusCode(500, "An error occurred while retrieving languages");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LanguageDto>> GetLanguage(Guid id)
        {
            try
            {
                var language = await _languageService.GetLanguageByIdAsync(id);
                if (language == null)
                {
                    return NotFound();
                }

                return Ok(language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting language: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the language");
            }
        }

        [HttpGet("code/{code}")]
        public async Task<ActionResult<LanguageDto>> GetLanguageByCode(string code)
        {
            try
            {
                var language = await _languageService.GetLanguageByCodeAsync(code);
                if (language == null)
                {
                    return NotFound();
                }

                return Ok(language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting language by code: {Code}", code);
                return StatusCode(500, "An error occurred while retrieving the language");
            }
        }

        [HttpGet("enabled")]
        public async Task<ActionResult<List<LanguageDto>>> GetEnabledLanguages()
        {
            try
            {
                var languages = await _languageService.GetEnabledLanguagesAsync();
                return Ok(languages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enabled languages");
                return StatusCode(500, "An error occurred while retrieving enabled languages");
            }
        }

        [HttpGet("default")]
        public async Task<ActionResult<LanguageDto>> GetDefaultLanguage()
        {
            try
            {
                var language = await _languageService.GetDefaultLanguageAsync();
                if (language == null)
                {
                    return NotFound("No default language configured");
                }

                return Ok(language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default language");
                return StatusCode(500, "An error occurred while retrieving the default language");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<LanguageDto>> CreateLanguage([FromBody] CreateLanguageRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var language = await _languageService.CreateLanguageAsync(request, userId);
                return CreatedAtAction(nameof(GetLanguage), new { id = language.Id }, language);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating language");
                return StatusCode(500, "An error occurred while creating the language");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<LanguageDto>> UpdateLanguage(Guid id, [FromBody] UpdateLanguageRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var language = await _languageService.UpdateLanguageAsync(id, request, userId);
                if (language == null)
                {
                    return NotFound();
                }

                return Ok(language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating language: {Id}", id);
                return StatusCode(500, "An error occurred while updating the language");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> DeleteLanguage(Guid id)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _languageService.DeleteLanguageAsync(id, userId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting language: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the language");
            }
        }

        [HttpPost("{id}/set-default")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> SetDefaultLanguage(Guid id)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _languageService.SetDefaultLanguageAsync(id, userId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default language: {Id}", id);
                return StatusCode(500, "An error occurred while setting the default language");
            }
        }

        [HttpPost("{id}/toggle")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> ToggleLanguage(Guid id, [FromBody] ToggleLanguageRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _languageService.ToggleLanguageAsync(id, request.IsEnabled, userId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling language: {Id}", id);
                return StatusCode(500, "An error occurred while toggling the language");
            }
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<Dictionary<string, object>>> GetLanguageStats()
        {
            try
            {
                var stats = await _languageService.GetLanguageStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting language stats");
                return StatusCode(500, "An error occurred while retrieving language statistics");
            }
        }

        [HttpPost("refresh-completion/{languageCode}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> RefreshLanguageCompletion(string languageCode)
        {
            try
            {
                await _languageService.RefreshLanguageCompletionAsync(languageCode);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing language completion: {LanguageCode}", languageCode);
                return StatusCode(500, "An error occurred while refreshing language completion");
            }
        }

        [HttpGet("direction/{direction}")]
        public async Task<ActionResult<List<LanguageDto>>> GetLanguagesByDirection(string direction)
        {
            try
            {
                var languages = await _languageService.GetLanguagesByDirectionAsync(direction);
                return Ok(languages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting languages by direction: {Direction}", direction);
                return StatusCode(500, "An error occurred while retrieving languages");
            }
        }

        [HttpGet("validate-code/{code}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<bool>> ValidateLanguageCode(string code)
        {
            try
            {
                var isValid = await _languageService.ValidateLanguageCodeAsync(code);
                return Ok(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating language code: {Code}", code);
                return StatusCode(500, "An error occurred while validating the language code");
            }
        }
    }

    public class ToggleLanguageRequest
    {
        public bool IsEnabled { get; set; }
    }
}
