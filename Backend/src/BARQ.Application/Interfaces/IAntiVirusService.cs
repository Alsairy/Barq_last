namespace BARQ.Application.Interfaces
{
    public interface IAntiVirusService
    {
        Task<ScanResult> ScanFileAsync(Stream fileStream, string fileName);
        Task<ScanResult> ScanFileAsync(string filePath);
        Task<bool> IsServiceAvailableAsync();
        Task<string> GetEngineVersionAsync();
        Task<ScanResult> QuickScanAsync(string fileHash, long fileSize);
    }

    public class ScanResult
    {
        public bool IsClean { get; set; }
        public string Status { get; set; } = string.Empty; // Clean, Infected, Suspicious, Error
        public string? ThreatName { get; set; }
        public string? Details { get; set; }
        public DateTime ScanTime { get; set; } = DateTime.UtcNow;
        public string EngineVersion { get; set; } = string.Empty;
        public TimeSpan ScanDuration { get; set; }
    }
}
