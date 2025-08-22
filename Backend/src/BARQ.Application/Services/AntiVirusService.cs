using BARQ.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace BARQ.Application.Services;

public class AntiVirusService : IAntiVirusService
{
    private readonly ILogger<AntiVirusService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AntiVirusService(
        ILogger<AntiVirusService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ScanResult> ScanFileAsync(Stream fileStream, string fileName)
    {
        try
        {
            var endpoint = _configuration["AntiVirus:Endpoint"];
            var apiKey = _configuration["AntiVirus:ApiKey"];
            
            using var client = _httpClientFactory.CreateClient("AntiVirus");
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", fileName);
            
            var response = await client.PostAsync($"{endpoint}/scan", content);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<ScanResult>();
            
            _logger.LogInformation("File {FileName} scanned successfully: {Status}", fileName, result?.Status);
            return result ?? new ScanResult { Status = "Clean", IsClean = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan file {FileName}", fileName);
            return new ScanResult { Status = "Error", Details = ex.Message };
        }
    }

    public async Task<string> GetEngineVersionAsync()
    {
        try
        {
            var endpoint = _configuration["AntiVirus:Endpoint"];
            using var client = _httpClientFactory.CreateClient("AntiVirus");
            
            var response = await client.GetAsync($"{endpoint}/version");
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get antivirus engine version");
            return "Unknown";
        }
    }

    public async Task<ScanResult> ScanFileAsync(string filePath)
    {
        try
        {
            using var fileStream = File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);
            return await ScanFileAsync(fileStream, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan file {FilePath}", filePath);
            return new ScanResult { Status = "Error", Details = ex.Message };
        }
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            var endpoint = _configuration["AntiVirus:Endpoint"];
            using var client = _httpClientFactory.CreateClient("AntiVirus");
            
            var response = await client.GetAsync($"{endpoint}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ScanResult> QuickScanAsync(string fileHash, long fileSize)
    {
        try
        {
            if (fileSize > 100 * 1024 * 1024) // 100MB limit
            {
                return new ScanResult { Status = "Error", Details = "File exceeds maximum scan size" };
            }

            var endpoint = _configuration["AntiVirus:Endpoint"];
            using var client = _httpClientFactory.CreateClient("AntiVirus");
            
            var requestData = new { hash = fileHash, size = fileSize };
            var requestContent = JsonContent.Create(requestData);
            var response = await client.PostAsync($"{endpoint}/quick-scan", requestContent);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<ScanResult>();
            return result ?? new ScanResult { Status = "Clean", IsClean = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform quick scan");
            return new ScanResult { Status = "Error", Details = ex.Message };
        }
    }
}
