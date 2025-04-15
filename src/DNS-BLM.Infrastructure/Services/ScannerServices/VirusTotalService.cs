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
    private readonly SemaphoreSlim _throttleSemaphore = new(2); // Limit concurrent requests

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
        var tasks = new List<Task>();

        foreach (var domain in domains)
        {
            tasks.Add(ScanDomainAsync(client, domain, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ScanDomainAsync(HttpClient client, string domain, CancellationToken cancellationToken)
    {
        try
        {
            await _throttleSemaphore.WaitAsync(cancellationToken);
            _logger.LogDebug("Scanning domain {Domain} with {ScannerName}", domain, ScannerName);

            try
            {
                var analysisResponse = await client.PostAsync(
                    $"domains/{domain}/analyse", 
                    new StringContent(""), 
                    cancellationToken);
                
                analysisResponse.EnsureSuccessStatusCode();
                var analysisResponseBody = await analysisResponse.Content.ReadAsStringAsync(cancellationToken);

                string? analysisId;
                using (var doc = JsonDocument.Parse(analysisResponseBody))
                {
                    var root = doc.RootElement;
                    analysisId = root.GetProperty("data").GetProperty("id").GetString();
                }

                _logger.LogDebug("Received analysis ID {AnalysisId} for domain {Domain}", analysisId, domain);

                var response = await client.GetAsync($"analyses/{analysisId}", cancellationToken);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                int statsMalicious;
                int statsSuspicious;
                using (var doc = JsonDocument.Parse(responseBody))
                {
                    var root = doc.RootElement;
                    var statsSection = root.GetProperty("data").GetProperty("attributes").GetProperty("stats");
                    statsMalicious = statsSection.GetProperty("malicious").GetInt32();
                    statsSuspicious = statsSection.GetProperty("suspicious").GetInt32();
                }

                var scanResult = new ScanResult
                {
                    Domain = domain,
                    IsBlacklisted = statsMalicious > 0 || statsSuspicious > 0,
                    ScannerName = ScannerName,
                    ScanResultUrl = $"https://www.virustotal.com/gui/domain-analysis/{analysisId}"
                };
                
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
        finally
        {
            _throttleSemaphore.Release();
        }
    }
}
