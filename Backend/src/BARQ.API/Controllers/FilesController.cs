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
    public class FilesController : ControllerBase
    {
        private readonly IFileAttachmentService _fileService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(
            IFileAttachmentService fileService,
            ILogger<FilesController> logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<FileUploadResponse>> UploadFile([FromForm] FileUploadRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            Guid? tenantId = null;
            if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var parsedTenantId))
            {
                tenantId = parsedTenantId;
            }

            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest("No file provided");
            }

            var result = await _fileService.UploadFileAsync(userId, tenantId, request);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<FileAttachmentDto>>> GetFiles([FromQuery] FileListRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            Guid? tenantId = null;
            if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var parsedTenantId))
            {
                tenantId = parsedTenantId;
            }

            var files = await _fileService.GetFilesAsync(userId, tenantId, request);
            return Ok(files);
        }

        [HttpGet("{fileId}")]
        public async Task<ActionResult<FileAttachmentDto>> GetFile(string fileId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var file = await _fileService.GetFileAsync(fileId, userId);
            if (file == null)
            {
                return NotFound();
            }

            return Ok(file);
        }

        [HttpPost("{fileId}/access")]
        public async Task<ActionResult<FileAccessResponse>> GenerateAccessUrl(string fileId, [FromBody] FileAccessRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            request.FileId = fileId;
            var accessResponse = await _fileService.GenerateAccessUrlAsync(fileId, userId, request);
            return Ok(accessResponse);
        }

        [HttpGet("{fileId}/download")]
        public async Task<IActionResult> DownloadFile(string fileId, [FromQuery] string accessToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var fileStream = await _fileService.DownloadFileAsync(fileId, userId, accessToken);
            if (fileStream == null)
            {
                return NotFound();
            }

            var file = await _fileService.GetFileAsync(fileId, userId);
            if (file == null)
            {
                return NotFound();
            }

            return File(fileStream, file.ContentType, file.FileName);
        }

        [HttpGet("{fileId}/thumbnail")]
        public async Task<IActionResult> GetThumbnail(string fileId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var thumbnailStream = await _fileService.GetThumbnailAsync(fileId, userId);
            if (thumbnailStream == null)
            {
                return NotFound();
            }

            return File(thumbnailStream, "image/jpeg");
        }

        [HttpGet("{fileId}/preview")]
        public async Task<IActionResult> GetPreview(string fileId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var previewStream = await _fileService.GetPreviewAsync(fileId, userId);
            if (previewStream == null)
            {
                return NotFound();
            }

            return File(previewStream, "application/pdf");
        }

        [HttpDelete("{fileId}")]
        public async Task<ActionResult> DeleteFile(string fileId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var success = await _fileService.DeleteFileAsync(fileId, userId);
            if (!success)
            {
                return NotFound();
            }

            return Ok();
        }

        [HttpPost("{fileId}/scan")]
        public async Task<ActionResult<FileScanResponse>> ScanFile(string fileId, [FromBody] FileScanRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            request.FileId = fileId;
            var scanResult = await _fileService.ScanFileAsync(fileId, userId, request);
            return Ok(scanResult);
        }

        [HttpPost("{fileId}/quarantine")]
        public async Task<ActionResult> QuarantineFile(string fileId, [FromBody] FileQuarantineRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            request.FileId = fileId;
            var success = await _fileService.QuarantineFileAsync(fileId, userId, request);
            if (!success)
            {
                return BadRequest("Failed to quarantine file");
            }

            return Ok();
        }

        [HttpPost("{fileId}/release")]
        public async Task<ActionResult> ReleaseFromQuarantine(string fileId, [FromBody] ReleaseFromQuarantineRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var success = await _fileService.ReleaseFromQuarantineAsync(fileId, userId, request.ReviewNotes);
            if (!success)
            {
                return BadRequest("Failed to release file from quarantine");
            }

            return Ok();
        }

        [HttpGet("quarantined")]
        public async Task<ActionResult<IEnumerable<FileAttachmentDto>>> GetQuarantinedFiles()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var files = await _fileService.GetQuarantinedFilesAsync(userId);
            return Ok(files);
        }

        [HttpGet("expired")]
        public async Task<ActionResult<IEnumerable<FileAttachmentDto>>> GetExpiredFiles()
        {
            var files = await _fileService.GetExpiredFilesAsync();
            return Ok(files);
        }

        [HttpPost("cleanup-expired")]
        public async Task<ActionResult> CleanupExpiredFiles()
        {
            var success = await _fileService.CleanupExpiredFilesAsync();
            if (!success)
            {
                return BadRequest("Failed to cleanup expired files");
            }

            return Ok();
        }

        [HttpPut("{fileId}/metadata")]
        public async Task<ActionResult> UpdateFileMetadata(string fileId, [FromBody] UpdateFileMetadataRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var success = await _fileService.UpdateFileMetadataAsync(fileId, userId, request.Metadata);
            if (!success)
            {
                return BadRequest("Failed to update file metadata");
            }

            return Ok();
        }
    }

    public class ReleaseFromQuarantineRequest
    {
        public string? ReviewNotes { get; set; }
    }

    public class UpdateFileMetadataRequest
    {
        public string Metadata { get; set; } = string.Empty;
    }
}
