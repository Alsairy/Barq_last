using BARQ.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace BARQ.Application.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly ILogger<LocalFileStorageService> _logger;
        private readonly string _basePath;
        private readonly string _baseUrl;

        public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
        {
            _logger = logger;
            _basePath = configuration.GetValue<string>("FileStorage:LocalPath") ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            _baseUrl = configuration.GetValue<string>("FileStorage:BaseUrl") ?? "https://localhost:7001/api/files";
            
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? folder = null)
        {
            var sanitizedFileName = SanitizeFileName(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";
            var folderPath = string.IsNullOrEmpty(folder) ? _basePath : Path.Combine(_basePath, folder);
            
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, uniqueFileName);
            var relativePath = Path.GetRelativePath(_basePath, filePath);

            using var fileStreamOutput = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(fileStreamOutput);

            _logger.LogInformation("File uploaded successfully: {FilePath}", relativePath);
            return relativePath.Replace('\\', '/');
        }

        public async Task<Stream> DownloadFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            return Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
        }

        public Task<bool> DeleteFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (!File.Exists(fullPath))
            {
                return false;
            }

            try
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
                return false;
            }
        }

        public Task<bool> FileExistsAsync(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);
            return Task.FromResult(File.Exists(fullPath));
        }

        public Task<string> GenerateSignedUrlAsync(string filePath, TimeSpan expiry, string accessType = "read")
        {
            var token = GenerateAccessToken(filePath, expiry, accessType);
            return Task.FromResult($"{_baseUrl}/download/{Uri.EscapeDataString(filePath)}?token={token}&expires={DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds()}");
        }

        public Task<string> GenerateUploadUrlAsync(string fileName, string contentType, TimeSpan expiry)
        {
            var token = GenerateAccessToken(fileName, expiry, "write");
            return Task.FromResult($"{_baseUrl}/upload?filename={Uri.EscapeDataString(fileName)}&contentType={Uri.EscapeDataString(contentType)}&token={token}&expires={DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds()}");
        }

        public Task<long> GetFileSizeAsync(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileInfo = new FileInfo(fullPath);
            return fileInfo.Length;
        }

        public async Task<string> CopyFileAsync(string sourceFilePath, string destinationFilePath)
        {
            var sourcePath = Path.Combine(_basePath, sourceFilePath);
            var destPath = Path.Combine(_basePath, destinationFilePath);
            
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"Source file not found: {sourceFilePath}");
            }

            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(sourcePath, destPath, true);
            _logger.LogInformation("File copied from {Source} to {Destination}", sourceFilePath, destinationFilePath);
            
            return destinationFilePath;
        }

        public async Task<string> MoveFileAsync(string sourceFilePath, string destinationFilePath)
        {
            var sourcePath = Path.Combine(_basePath, sourceFilePath);
            var destPath = Path.Combine(_basePath, destinationFilePath);
            
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"Source file not found: {sourceFilePath}");
            }

            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Move(sourcePath, destPath);
            _logger.LogInformation("File moved from {Source} to {Destination}", sourceFilePath, destinationFilePath);
            
            return destinationFilePath;
        }

        public async Task<IEnumerable<string>> ListFilesAsync(string? folder = null, string? pattern = null)
        {
            var searchPath = string.IsNullOrEmpty(folder) ? _basePath : Path.Combine(_basePath, folder);
            
            if (!Directory.Exists(searchPath))
            {
                return Enumerable.Empty<string>();
            }

            var searchPattern = pattern ?? "*";
            var files = Directory.GetFiles(searchPath, searchPattern, SearchOption.AllDirectories);
            
            return files.Select(f => Path.GetRelativePath(_basePath, f).Replace('\\', '/'));
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            return string.IsNullOrEmpty(sanitized) ? "file" : sanitized;
        }

        private string GenerateAccessToken(string filePath, TimeSpan expiry, string accessType)
        {
            var payload = $"{filePath}|{accessType}|{DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds()}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("your-secret-key-here"));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }
    }
}
