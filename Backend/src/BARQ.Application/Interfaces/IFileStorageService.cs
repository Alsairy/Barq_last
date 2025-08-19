namespace BARQ.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? folder = null);
        Task<Stream> DownloadFileAsync(string filePath);
        Task<bool> DeleteFileAsync(string filePath);
        Task<bool> FileExistsAsync(string filePath);
        Task<string> GenerateSignedUrlAsync(string filePath, TimeSpan expiry, string accessType = "read");
        Task<string> GenerateUploadUrlAsync(string fileName, string contentType, TimeSpan expiry);
        Task<long> GetFileSizeAsync(string filePath);
        Task<string> CopyFileAsync(string sourceFilePath, string destinationFilePath);
        Task<string> MoveFileAsync(string sourceFilePath, string destinationFilePath);
        Task<IEnumerable<string>> ListFilesAsync(string? folder = null, string? pattern = null);
    }
}
