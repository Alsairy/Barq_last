using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface IFileAttachmentService
    {
        Task<FileUploadResponse> UploadFileAsync(Guid userId, Guid? tenantId, FileUploadRequest request);
        Task<FileAttachmentDto?> GetFileAsync(string fileId, Guid userId);
        Task<PagedResult<FileAttachmentDto>> GetFilesAsync(Guid userId, Guid? tenantId, FileListRequest request);
        Task<FileAccessResponse> GenerateAccessUrlAsync(string fileId, Guid userId, FileAccessRequest request);
        Task<bool> DeleteFileAsync(string fileId, Guid userId);
        Task<FileScanResponse> ScanFileAsync(string fileId, Guid userId, FileScanRequest request);
        Task<bool> QuarantineFileAsync(string fileId, Guid userId, FileQuarantineRequest request);
        Task<bool> ReleaseFromQuarantineAsync(string fileId, Guid userId, string? reviewNotes = null);
        Task<Stream?> DownloadFileAsync(string fileId, Guid userId, string accessToken);
        Task<Stream?> GetThumbnailAsync(string fileId, Guid userId);
        Task<Stream?> GetPreviewAsync(string fileId, Guid userId);
        Task<bool> GenerateThumbnailAsync(string fileId);
        Task<bool> GeneratePreviewAsync(string fileId);
        Task<IEnumerable<FileAttachmentDto>> GetExpiredFilesAsync();
        Task<bool> CleanupExpiredFilesAsync();
        Task<bool> UpdateFileMetadataAsync(string fileId, Guid userId, string metadata);
        Task<IEnumerable<FileAttachmentDto>> GetQuarantinedFilesAsync(Guid userId);
        Task LogFileAccessAsync(string fileId, Guid userId, string accessType, string? ipAddress = null, string? userAgent = null);
    }
}
