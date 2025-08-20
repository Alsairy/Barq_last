using Microsoft.AspNetCore.Http;

namespace BARQ.Core.DTOs
{
    public class FileAttachmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ScanResult { get; set; }
        public DateTime? ScanCompletedAt { get; set; }
        public bool IsPublic { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public string? TenantId { get; set; }
        public bool HasThumbnail { get; set; }
        public bool HasPreview { get; set; }
        public string? DownloadUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? PreviewUrl { get; set; }
    }

    public class FileUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public bool IsPublic { get; set; } = false;
        public DateTime? ExpiresAt { get; set; }
        public string? Metadata { get; set; }
        public string? Category { get; set; }
    }

    public class FileUploadResponse
    {
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? UploadUrl { get; set; }
        public bool RequiresScanning { get; set; }
    }

    public class FileAccessRequest
    {
        public string FileId { get; set; } = string.Empty;
        public string AccessType { get; set; } = "View"; // View, Download, Edit
        public int? ExpiryMinutes { get; set; } = 60;
    }

    public class FileAccessResponse
    {
        public string AccessUrl { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string AccessToken { get; set; } = string.Empty;
    }

    public class FileScanRequest
    {
        public string FileId { get; set; } = string.Empty;
        public bool ForceRescan { get; set; } = false;
    }

    public class FileScanResponse
    {
        public string FileId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ScanResult { get; set; }
        public DateTime? ScanCompletedAt { get; set; }
        public string? Details { get; set; }
    }

    public class FileQuarantineRequest
    {
        public string FileId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }
    }

    public class FileListRequest : Common.ListRequest
    {
        public string? Status { get; set; }
        public string? ContentType { get; set; }
        public string? Category { get; set; }
        public DateTime? UploadedAfter { get; set; }
        public DateTime? UploadedBefore { get; set; }
        public long? MinSize { get; set; }
        public long? MaxSize { get; set; }
        public bool? IsPublic { get; set; }
        public bool? HasThumbnail { get; set; }
        public string? UploadedBy { get; set; }
    }
}
