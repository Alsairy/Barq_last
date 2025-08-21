using BARQ.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BARQ.Application.Services
{
    public class MockAntiVirusService : IAntiVirusService
    {
        private readonly ILogger<MockAntiVirusService> _logger;
        private readonly Random _random = new();

        public MockAntiVirusService(ILogger<MockAntiVirusService> logger)
        {
            _logger = logger;
        }

        public async System.Threading.Tasks.Task<ScanResult> ScanFileAsync(Stream fileStream, string fileName)
        {
            await Task.Delay(100); // Simulate scan time
            
            var result = new ScanResult
            {
                IsClean = !IsSimulatedThreat(fileName),
                Status = IsSimulatedThreat(fileName) ? "Infected" : "Clean",
                EngineVersion = "MockAV 1.0.0",
                ScanDuration = TimeSpan.FromMilliseconds(100)
            };

            if (!result.IsClean)
            {
                result.ThreatName = "Test.Virus.Simulated";
                result.Details = "This is a simulated threat for testing purposes";
            }

            _logger.LogInformation("Mock AV scan completed for {FileName}: {Status}", fileName, result.Status);
            return result;
        }

        public async System.Threading.Tasks.Task<ScanResult> ScanFileAsync(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return await ScanFileAsync(fileStream, fileName);
        }

        public async System.Threading.Tasks.Task<bool> IsServiceAvailableAsync()
        {
            return true;
        }

        public async System.Threading.Tasks.Task<string> GetEngineVersionAsync()
        {
            return "MockAV 1.0.0";
        }

        public async System.Threading.Tasks.Task<ScanResult> QuickScanAsync(string fileHash, long fileSize)
        {
            await Task.Delay(50);
            
            return new ScanResult
            {
                IsClean = true,
                Status = "Clean",
                EngineVersion = "MockAV 1.0.0",
                ScanDuration = TimeSpan.FromMilliseconds(50),
                Details = "Quick scan based on file hash"
            };
        }

        private bool IsSimulatedThreat(string fileName)
        {
            var lowerFileName = fileName.ToLowerInvariant();
            return lowerFileName.Contains("virus") || 
                   lowerFileName.Contains("malware") || 
                   lowerFileName.Contains("threat") ||
                   lowerFileName.EndsWith(".exe.txt") ||
                   _random.Next(100) < 5; // 5% chance of simulated threat
        }
    }
}
