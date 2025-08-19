using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.Json;

namespace BARQ.Application.Services
{
    public class FileAttachmentService : IFileAttachmentService
    {
        private readonly BarqDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly IAntiVirusService _antiVirus;
        private readonly ILogger<FileAttachmentService> _logger;

        public FileAttachmentService(
            BarqDbContext context,
            IFileStorageService fileStorage,
            IAntiVirusService antiVirus,
            ILogger<FileAttachmentService> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _antiVirus = antiVirus;
            _logger = logger;
        }

        public async Task<FileUploadResponse> UploadFileAsync(Guid userId, Guid? tenantId, FileUploadRequest request)
        {
            var fileId = Guid.NewGuid();
            var fileName = request.File.FileName;
            var contentType = request.File.ContentType;
            var fileSize = request.File.Length;

            string fileHash;
            using (var stream = request.File.OpenReadStream())
            {
                fileHash = await ComputeFileHashAsync(stream);
            }

            var existingFile = await _context.FileAttachments
                .FirstOrDefaultAsync(f => f.FileHash == fileHash && f.TenantId == tenantId);

            if (existingFile != null)
            {
                _logger.LogInformation("Duplicate file detected, returning existing file: {FileId}", existingFile.Id);
                return new FileUploadResponse
                {
                    FileId = existingFile.Id.ToString(),
                    FileName = existingFile.FileName,
                    FileSize = existingFile.FileSize,
                    Status = existingFile.Status
                };
            }

            string storagePath;
            using (var stream = request.File.OpenReadStream())
            {
                var folder = tenantId?.ToString() ?? "shared";
                storagePath = await _fileStorage.UploadFileAsync(stream, fileName, contentType, folder);
            }

            var fileAttachment = new FileAttachment
            {
                Id = fileId,
                FileName = fileName,
                OriginalFileName = fileName,
                ContentType = contentType,
                FileSize = fileSize,
                StoragePath = storagePath,
                FileHash = fileHash,
                UploadedBy = userId,
                TenantId = tenantId ?? Guid.Empty,
                Status = "Pending",
                IsPublic = request.IsPublic,
                ExpiresAt = request.ExpiresAt,
                Metadata = request.Metadata,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.FileAttachments.Add(fileAttachment);
            await _context.SaveChangesAsync();

            _ = System.Threading.Tasks.Task.Run(async () => await ScanFileInBackgroundAsync(fileId));

            _logger.LogInformation("File uploaded successfully: {FileId} by user {UserId}", fileId, userId);

            return new FileUploadResponse
            {
                FileId = fileId.ToString(),
                FileName = fileName,
                FileSize = fileSize,
                Status = "Pending",
                RequiresScanning = true
            };
        }

        public async Task<FileAttachmentDto?> GetFileAsync(string fileId, Guid userId)
        {
            if (!Guid.TryParse(fileId, out var fileGuid))
                return null;

            var file = await _context.FileAttachments
                .Include(f => f.UploadedByUser)
                .FirstOrDefaultAsync(f => f.Id == fileGuid);

            if (file == null || !CanUserAccessFile(file, userId))
                return null;

            await LogFileAccessAsync(fileId, userId, "View");

            return MapToDto(file);
        }

        public async Task<PagedResult<FileAttachmentDto>> GetFilesAsync(Guid userId, Guid? tenantId, FileListRequest request)
        {
            var query = _context.FileAttachments
                .Include(f => f.UploadedByUser)
                .Where(f => f.TenantId == tenantId || f.IsPublic);

            if (!string.IsNullOrEmpty(request.Status))
                query = query.Where(f => f.Status == request.Status);

            if (!string.IsNullOrEmpty(request.ContentType))
                query = query.Where(f => f.ContentType.Contains(request.ContentType));

            if (request.UploadedAfter.HasValue)
                query = query.Where(f => f.CreatedAt >= request.UploadedAfter.Value);

            if (request.UploadedBefore.HasValue)
                query = query.Where(f => f.CreatedAt <= request.UploadedBefore.Value);

            if (request.MinSize.HasValue)
                query = query.Where(f => f.FileSize >= request.MinSize.Value);

            if (request.MaxSize.HasValue)
                query = query.Where(f => f.FileSize <= request.MaxSize.Value);

            if (request.IsPublic.HasValue)
                query = query.Where(f => f.IsPublic == request.IsPublic.Value);

            if (!string.IsNullOrEmpty(request.SearchTerm))
                query = query.Where(f => f.FileName.Contains(request.SearchTerm) || f.OriginalFileName.Contains(request.SearchTerm));

            var totalCount = await query.CountAsync();
            var files = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var result = new PagedResult<FileAttachmentDto>
            {
                Items = files.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
            return result;
        }

        public async Task<FileAccessResponse> GenerateAccessUrlAsync(string fileId, Guid userId, FileAccessRequest request)
        {
            if (!Guid.TryParse(fileId, out var fileGuid))
                throw new ArgumentException("Invalid file ID");

            var file = await _context.FileAttachments.FindAsync(fileGuid);
            if (file == null || !CanUserAccessFile(file, userId))
                throw new UnauthorizedAccessException("File not found or access denied");

            var expiry = TimeSpan.FromMinutes(request.ExpiryMinutes ?? 60);
            var accessUrl = await _fileStorage.GenerateSignedUrlAsync(file.StoragePath, expiry, request.AccessType);
            var accessToken = GenerateAccessToken(fileId, userId, request.AccessType, expiry);

            await LogFileAccessAsync(fileId, userId, request.AccessType);

            return new FileAccessResponse
            {
                AccessUrl = accessUrl,
                ExpiresAt = DateTime.UtcNow.Add(expiry),
                AccessToken = accessToken
            };
        }

        public async Task<bool> DeleteFileAsync(string fileId, Guid userId)
        {
            if (!Guid.TryParse(fileId, out var fileGuid))
                return false;

            var file = await _context.FileAttachments.FindAsync(fileGuid);
            if (file == null || !CanUserDeleteFile(file, userId))
                return false;

            file.Status = "Deleted";
            file.UpdatedAt = DateTime.UtcNow;

            await _fileStorage.DeleteFileAsync(file.StoragePath);
            await _context.SaveChangesAsync();

            await LogFileAccessAsync(fileId, userId, "Delete");
            _logger.LogInformation("File deleted: {FileId} by user {UserId}", fileId, userId);

            return true;
        }

        public async Task<FileScanResponse> ScanFileAsync(string fileId, Guid userId, FileScanRequest request)
        {
            if (!Guid.TryParse(fileId, out var fileGuid))
                throw new ArgumentException("Invalid file ID");

            var file = await _context.FileAttachments.FindAsync(fileGuid);
            if (file == null)
                throw new ArgumentException("File not found");

            if (!request.ForceRescan && file.Status == "Clean")
            {
                return new FileScanResponse
                {
                    FileId = fileId,
                    Status = file.Status,
                    ScanResult = file.ScanResult,
                    ScanCompletedAt = file.ScanCompletedAt
                };
            }

            var scanResult = await _antiVirus.ScanFileAsync(Path.Combine("uploads", file.StoragePath));
            
            file.Status = scanResult.IsClean ? "Clean" : "Quarantined";
            file.ScanResult = scanResult.Status;
            file.ScanCompletedAt = DateTime.UtcNow;
            file.ScanDetails = JsonSerializer.Serialize(scanResult);
            file.UpdatedAt = DateTime.UtcNow;

            if (!scanResult.IsClean)
            {
                await QuarantineFileInternalAsync(file, userId, scanResult.ThreatName ?? "Unknown threat", scanResult.Details);
            }

            await _context.SaveChangesAsync();

            return new FileScanResponse
            {
                FileId = fileId,
                Status = file.Status,
                ScanResult = file.ScanResult,
                ScanCompletedAt = file.ScanCompletedAt,
                Details = scanResult.Details
            };
        }

        public async Task<bool> QuarantineFileAsync(string fileId, Guid userId, FileQuarantineRequest request)
        {
            if (!Guid.TryParse(fileId, out var fileGuid))
                return false;

            var file = await _context.FileAttachments.FindAsync(fileGuid);
            if (file == null)
                return false;

            await QuarantineFileInternalAsync(file, userId, request.Reason, request.Details);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ReleaseFromQuarantineAsync(string fileId, Guid userId, string? reviewNotes = null)
        {
            if (!Guid.TryParse(fileId, out var fileGuid))
                return false;

            var file = await _context.FileAttachments.FindAsync(fileGuid);
            var quarantine = await _context.FileQuarantines.FirstOrDefaultAsync(q => q.FileAttachmentId == fileGuid && q.Status == "Quarantined");

            if (file == null || quarantine == null)
                return false;

            file.Status = "Clean";
            file.UpdatedAt = DateTime.UtcNow;

            quarantine.Status = "Released";
            quarantine.ReviewedAt = DateTime.UtcNow;
            quarantine.ReviewedBy = userId;
            quarantine.ReviewNotes = reviewNotes;

            await _context.SaveChangesAsync();
            _logger.LogInformation("File released from quarantine: {FileId} by user {UserId}", fileId, userId);

            return true;
        }

        public async Task<Stream?> DownloadFileAsync(string fileId, Guid userId, string accessToken)
        {
            if (!Guid.TryParse(fileId, out var fileGuid))
                return null;

            var file = await _context.FileAttachments.FindAsync(fileGuid);
            if (file == null || !CanUserAccessFile(file, userId))
                return null;

            if (!ValidateAccessToken(accessToken, fileId, userId))
                return null;

            await LogFileAccessAsync(fileId, userId, "Download");
            return await _fileStorage.DownloadFileAsync(file.StoragePath);
        }

        public async Task<Stream?> GetThumbnailAsync(string fileId, Guid userId)
        {
            if (!Guid.TryParse(fileId, out var fileGuid))
                return null;

            var file = await _context.FileAttachments.FindAsync(fileGuid);
            if (file == null || !CanUserAccessFile(file, userId) || string.IsNullOrEmpty(file.ThumbnailPath))
                return null;

            return await _fileStorage.DownloadFileAsync(file.ThumbnailPath);
        }

        public async Task<Stream?> GetPreviewAsync(string fileId, Guid userId)
        {
            if (!Guid.TryParse(fileId, out var fileGuid))
                return null;

            var file = await _context.FileAttachments.FindAsync(fileGuid);
            if (file == null || !CanUserAccessFile(file, userId) || string.IsNullOrEmpty(file.PreviewPath))
                return null;

            return await _fileStorage.DownloadFileAsync(file.PreviewPath);
        }

        public async Task<bool> GenerateThumbnailAsync(string fileId)
        {
            return false;
        }

        public async Task<bool> GeneratePreviewAsync(string fileId)
        {
            return false;
        }

        public async Task<IEnumerable<FileAttachmentDto>> GetExpiredFilesAsync()
        {
            var expiredFiles = await _context.FileAttachments
                .Where(f => f.ExpiresAt.HasValue && f.ExpiresAt.Value <= DateTime.UtcNow && f.Status != "Deleted")
                .ToListAsync();

            return expiredFiles.Select(MapToDto);
        }

        public async System.Threading.Tasks.Task<bool> CleanupExpiredFilesAsync()
        {
            var expiredFiles = await _context.FileAttachments
                .Where(f => f.ExpiresAt.HasValue && f.ExpiresAt.Value <= DateTime.UtcNow && f.Status != "Deleted")
                .ToListAsync();

            foreach (var file in expiredFiles)
            {
                file.Status = "Deleted";
                file.UpdatedAt = DateTime.UtcNow;
                await _fileStorage.DeleteFileAsync(file.StoragePath);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired files", expiredFiles.Count);

            return true;
        }

        public async System.Threading.Tasks.Task<bool> UpdateFileMetadataAsync(string fileId, Guid userId, string metadata)
        {
            if (!Guid.TryParse(fileId, out var fileGuid))
                return false;

            var file = await _context.FileAttachments.FindAsync(fileGuid);
            if (file == null || !CanUserAccessFile(file, userId))
                return false;

            file.Metadata = metadata;
            file.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<FileAttachmentDto>> GetQuarantinedFilesAsync(Guid userId)
        {
            var quarantinedFiles = await _context.FileAttachments
                .Where(f => f.Status == "Quarantined")
                .ToListAsync();

            return quarantinedFiles.Select(MapToDto);
        }

        public async System.Threading.Tasks.Task LogFileAccessAsync(string fileId, Guid userId, string accessType, string? ipAddress = null, string? userAgent = null)
        {
            if (!Guid.TryParse(fileId, out var fileGuid))
                return;

            var accessRecord = new FileAttachmentAccess
            {
                Id = Guid.NewGuid(),
                FileAttachmentId = fileGuid,
                UserId = userId,
                AccessType = accessType,
                AccessedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.FileAttachmentAccesses.Add(accessRecord);
            await _context.SaveChangesAsync();
        }

        private async System.Threading.Tasks.Task ScanFileInBackgroundAsync(Guid fileId)
        {
            try
            {
                var file = await _context.FileAttachments.FindAsync(fileId);
                if (file == null) return;

                var scanResult = await _antiVirus.ScanFileAsync(Path.Combine("uploads", file.StoragePath));
                
                file.Status = scanResult.IsClean ? "Clean" : "Quarantined";
                file.ScanResult = scanResult.Status;
                file.ScanCompletedAt = DateTime.UtcNow;
                file.ScanDetails = JsonSerializer.Serialize(scanResult);
                file.UpdatedAt = DateTime.UtcNow;

                if (!scanResult.IsClean)
                {
                    await QuarantineFileInternalAsync(file, file.UploadedBy, scanResult.ThreatName ?? "Virus detected", scanResult.Details);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Background scan completed for file {FileId}: {Status}", fileId, file.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background scan failed for file {FileId}", fileId);
            }
        }

        private async System.Threading.Tasks.Task QuarantineFileInternalAsync(FileAttachment file, Guid quarantinedBy, string reason, string? details)
        {
            file.Status = "Quarantined";
            file.UpdatedAt = DateTime.UtcNow;

            var quarantine = new FileQuarantine
            {
                Id = Guid.NewGuid(),
                FileAttachmentId = file.Id,
                Reason = reason,
                Details = details,
                QuarantinedBy = quarantinedBy,
                QuarantinedAt = DateTime.UtcNow,
                Status = "Quarantined",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.FileQuarantines.Add(quarantine);
        }

        private bool CanUserAccessFile(FileAttachment file, Guid userId)
        {
            return file.IsPublic || file.UploadedBy == userId;
        }

        private bool CanUserDeleteFile(FileAttachment file, Guid userId)
        {
            return file.UploadedBy == userId;
        }

        private FileAttachmentDto MapToDto(FileAttachment file)
        {
            return new FileAttachmentDto
            {
                Id = file.Id.ToString(),
                FileName = file.FileName,
                OriginalFileName = file.OriginalFileName,
                ContentType = file.ContentType,
                FileSize = file.FileSize,
                Status = file.Status,
                ScanResult = file.ScanResult,
                ScanCompletedAt = file.ScanCompletedAt,
                IsPublic = file.IsPublic,
                ExpiresAt = file.ExpiresAt,
                CreatedAt = file.CreatedAt,
                UploadedBy = file.UploadedBy.ToString(),
                TenantId = file.TenantId.ToString(),
                HasThumbnail = !string.IsNullOrEmpty(file.ThumbnailPath),
                HasPreview = !string.IsNullOrEmpty(file.PreviewPath)
            };
        }

        private async Task<string> ComputeFileHashAsync(Stream stream)
        {
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(hash);
        }

        private string GenerateAccessToken(string fileId, Guid userId, string accessType, TimeSpan expiry)
        {
            var payload = $"{fileId}|{userId}|{accessType}|{DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds()}";
            using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes("your-secret-key-here"));
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }

        private bool ValidateAccessToken(string token, string fileId, Guid userId)
        {
            return true;
        }
    }
}
