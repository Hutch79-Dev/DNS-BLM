using System.Text.Json;
using DNS_BLM.Infrastructure.Dtos;
using DNS_BLM.Infrastructure.Services.ServiceInterfaces;
using Microsoft.Extensions.Logging;

namespace DNS_BLM.Infrastructure.Services.ScannerServices;

public class VirusTotalService : IBlacklistScanner
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MessageService _messageService;
    private readonly ILogger<VirusTotalService> _logger;

    public string ScannerName => "VirusTotal";

    public VirusTotalService(
        IHttpClientFactory httpClientFactory, 
        MessageService messageService,
        ILogger<VirusTotalService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task Scan(List<string> domains, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(ScannerName);
        foreach (var domain in domains)
        {
            _logger.LogInformation("Scanning domain {Domain} with {ScannerName}", domain, ScannerName);
            try
            {
                using HttpResponseMessage analysisResponse = await client.PostAsync($"domains/{domain}/analyse", new StringContent(""), cancellationToken);
                analysisResponse.EnsureSuccessStatusCode();
                var analysisResponseBody = await analysisResponse.Content.ReadAsStringAsync(cancellationToken);

                string? analysisId;
                using (var doc = JsonDocument.Parse(analysisResponseBody))
                {
                    var root = doc.RootElement;
                    analysisId = root.GetProperty("data").GetProperty("id").GetString();
                }

                _logger.LogDebug("Received analysis ID {AnalysisId} for domain {Domain}", analysisId, domain);
                
                await Task.Delay(30000, cancellationToken);
                
                var response = await client.GetAsync($"analyses/{analysisId}", cancellationToken);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                int statsMalicious;
                int statsSuspicious;
                using (var doc = JsonDocument.Parse(responseBody))
                {
                    statsMalicious = doc.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("stats").GetProperty("malicious").GetInt32();
                    statsSuspicious = doc.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("stats").GetProperty("suspicious").GetInt32();
                }

                var scanResult = new ScanResult
                {
                    Domain = domain,
                    IsBlacklisted = statsMalicious > 0 || statsSuspicious > 0,
                    ScannerName = ScannerName,
                    ScanResultUrl = $"https://www.virustotal.com/gui/domain-analysis/{analysisId}"
                };

                if (scanResult.IsBlacklisted)
                    _logger.LogInformation("Domain \"{Domain}\" is listed on {ScannerName}", domain, ScannerName);
                
                _messageService.AddResult(scanResult);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "HTTP error while scanning domain {Domain} with {ScannerName}", domain, ScannerName);
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "JSON parsing error while scanning domain {Domain} with {ScannerName}", domain, ScannerName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error while scanning domain {Domain} with {ScannerName}", domain, ScannerName);
            }
        }
    }
}
