using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TranslationsController : ControllerBase
    {
        private readonly ITranslationService _translationService;
        private readonly ILogger<TranslationsController> _logger;

        public TranslationsController(ITranslationService translationService, ILogger<TranslationsController> logger)
        {
            _translationService = translationService;
            _logger = logger;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in claims");
            }
            return userId;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<TranslationDto>>> GetTranslations([FromQuery] ListRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _translationService.GetTranslationsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translations");
                return StatusCode(500, "An error occurred while retrieving translations");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TranslationDto>> GetTranslation(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var translation = await _translationService.GetTranslationByIdAsync(id);
                if (translation == null)
                {
                    return NotFound();
                }

                return Ok(translation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translation: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the translation");
            }
        }

        [HttpGet("key/{languageCode}/{key}")]
        public async Task<ActionResult<TranslationDto>> GetTranslationByKey(string languageCode, string key)
        {
            try
            {
                var translation = await _translationService.GetTranslationByKeyAsync(languageCode, key);
                if (translation == null)
                {
                    return NotFound();
                }

                return Ok(translation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translation by key: {LanguageCode}/{Key}", languageCode, key);
                return StatusCode(500, "An error occurred while retrieving the translation");
            }
        }

        [HttpGet("language/{languageCode}")]
        public async Task<ActionResult<Dictionary<string, string>>> GetTranslationsForLanguage(string languageCode, [FromQuery] string? category = null)
        {
            try
            {
                var translations = await _translationService.GetTranslationsForLanguageAsync(languageCode, category);
                return Ok(translations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translations for language: {LanguageCode}", languageCode);
                return StatusCode(500, "An error occurred while retrieving translations");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin,Translator")]
        public async Task<ActionResult<TranslationDto>> CreateTranslation([FromBody] CreateTranslationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var translation = await _translationService.CreateTranslationAsync(request, userId.ToString());
                return CreatedAtAction(nameof(GetTranslation), new { id = translation.Id }, translation);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating translation");
                return StatusCode(500, "An error occurred while creating the translation");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin,Translator")]
        public async Task<ActionResult<TranslationDto>> UpdateTranslation(Guid id, [FromBody] UpdateTranslationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var translation = await _translationService.UpdateTranslationAsync(id, request, userId);
                if (translation == null)
                {
                    return NotFound();
                }

                return Ok(translation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating translation: {Id}", id);
                return StatusCode(500, "An error occurred while updating the translation");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> DeleteTranslation(Guid id)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _translationService.DeleteTranslationAsync(id, userId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting translation: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the translation");
            }
        }

        [HttpPost("bulk")]
        [Authorize(Roles = "Admin,SuperAdmin,Translator")]
        public async Task<ActionResult<List<TranslationDto>>> BulkCreateTranslations([FromBody] BulkTranslationRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var translations = await _translationService.BulkCreateTranslationsAsync(request, userId);
                return Ok(translations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating translations");
                return StatusCode(500, "An error occurred while creating translations");
            }
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin,SuperAdmin,TranslationReviewer")]
        public async Task<ActionResult> ApproveTranslation(Guid id)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _translationService.ApproveTranslationAsync(id, userId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving translation: {Id}", id);
                return StatusCode(500, "An error occurred while approving the translation");
            }
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin,SuperAdmin,TranslationReviewer")]
        public async Task<ActionResult> RejectTranslation(Guid id, [FromBody] RejectTranslationRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _translationService.RejectTranslationAsync(id, userId, request.Reason);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting translation: {Id}", id);
                return StatusCode(500, "An error occurred while rejecting the translation");
            }
        }

        [HttpGet("category/{languageCode}/{category}")]
        public async Task<ActionResult<List<TranslationDto>>> GetTranslationsByCategory(string languageCode, string category)
        {
            try
            {
                var translations = await _translationService.GetTranslationsByCategoryAsync(languageCode, category);
                return Ok(translations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translations by category: {LanguageCode}/{Category}", languageCode, category);
                return StatusCode(500, "An error occurred while retrieving translations");
            }
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin,SuperAdmin,TranslationReviewer")]
        public async Task<ActionResult<List<TranslationDto>>> GetPendingTranslations([FromQuery] string? languageCode = null)
        {
            try
            {
                var translations = await _translationService.GetPendingTranslationsAsync(languageCode);
                return Ok(translations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending translations");
                return StatusCode(500, "An error occurred while retrieving pending translations");
            }
        }

        [HttpGet("stats/{languageCode}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<TranslationStatsDto>> GetTranslationStats(string languageCode)
        {
            try
            {
                var stats = await _translationService.GetTranslationStatsAsync(languageCode);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translation stats: {LanguageCode}", languageCode);
                return StatusCode(500, "An error occurred while retrieving translation statistics");
            }
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<List<TranslationStatsDto>>> GetAllLanguageStats()
        {
            try
            {
                var stats = await _translationService.GetAllLanguageStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all language stats");
                return StatusCode(500, "An error occurred while retrieving language statistics");
            }
        }

        [HttpGet("export/{languageCode}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<Dictionary<string, object>>> ExportTranslations(string languageCode, [FromQuery] string format = "json")
        {
            try
            {
                var export = await _translationService.ExportTranslationsAsync(languageCode, format);
                return Ok(export);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting translations: {LanguageCode}", languageCode);
                return StatusCode(500, "An error occurred while exporting translations");
            }
        }

        [HttpPost("import/{languageCode}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> ImportTranslations(string languageCode, [FromBody] Dictionary<string, object> translations)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _translationService.ImportTranslationsAsync(languageCode, translations, userId);
                if (!success)
                {
                    return BadRequest("Failed to import translations");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing translations: {LanguageCode}", languageCode);
                return StatusCode(500, "An error occurred while importing translations");
            }
        }

        [HttpGet("missing/{languageCode}")]
        [Authorize(Roles = "Admin,SuperAdmin,Translator")]
        public async Task<ActionResult<List<string>>> GetMissingTranslationKeys(string languageCode, [FromQuery] string? category = null)
        {
            try
            {
                var missingKeys = await _translationService.GetMissingTranslationKeysAsync(languageCode, category);
                return Ok(missingKeys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting missing translation keys: {LanguageCode}", languageCode);
                return StatusCode(500, "An error occurred while retrieving missing translation keys");
            }
        }

        [HttpPost("sync/{sourceLanguageCode}/{targetLanguageCode}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> SyncTranslationKeys(string sourceLanguageCode, string targetLanguageCode)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _translationService.SyncTranslationKeysAsync(sourceLanguageCode, targetLanguageCode, userId);
                if (!success)
                {
                    return BadRequest("Failed to sync translation keys");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing translation keys: {Source} -> {Target}", sourceLanguageCode, targetLanguageCode);
                return StatusCode(500, "An error occurred while syncing translation keys");
            }
        }
    }

    public class RejectTranslationRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
