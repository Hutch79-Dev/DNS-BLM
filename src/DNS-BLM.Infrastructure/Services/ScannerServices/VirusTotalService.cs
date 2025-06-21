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
    private readonly RetryService _retryService;

    public string ScannerName => "VirusTotal";

    public VirusTotalService(
        IHttpClientFactory httpClientFactory,
        MessageService messageService,
        ILogger<VirusTotalService> logger,
        RetryService  retryService
        )
    {
        _httpClientFactory = httpClientFactory;
        _messageService = messageService;
        _logger = logger;
        _retryService = retryService;
    }

    public async Task Scan(string[] domains, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(ScannerName);
        foreach (var domain in domains)
        {
            _logger.LogInformation("Scanning domain {Domain} with {ScannerName}", domain, ScannerName);
            try
            {
                int maxRetries = 3;
                int currentRetry = 0;
                string analysisResponseBody = null;

                while (currentRetry <= maxRetries)
                {
                    try
                    {
                        using HttpResponseMessage analysisResponse = await client.PostAsync($"domains/{domain}/analyse", new StringContent(string.Empty), cancellationToken);
                        analysisResponse.EnsureSuccessStatusCode();
                        analysisResponseBody = await analysisResponse.Content.ReadAsStringAsync(cancellationToken);
                        break;
                    }
                    catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                    {
                        currentRetry++;
                        if (currentRetry > maxRetries)
                            throw;
        
                        int delay = 1000 * (int)Math.Pow(2, currentRetry - 1);
                        await Task.Delay(delay, cancellationToken);
                    }
                }

                string? analysisId;
                using (var doc = JsonDocument.Parse(analysisResponseBody))
                {
                    var root = doc.RootElement;
                    analysisId = root.GetProperty("data").GetProperty("id").GetString();
                }

                _logger.LogDebug("Received analysis ID {AnalysisId} for domain {Domain}", analysisId, domain);

                await Task.Delay(5000, cancellationToken);
                await Analyze(analysisId, domain, client, cancellationToken);
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

    private async Task Analyze(string? analysisId, string domain, HttpClient client, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(analysisId))
        {
            _logger.LogError("The analysis ID for Domain {Domain} must not be null!", domain);
            return;
        }

        int maxAttempts = 3;
        
        var result = await _retryService.Retry(async () =>
        {
            var response = await client.GetAsync($"analyses/{analysisId}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(responseBody);
            var attributes = doc.RootElement.GetProperty("data").GetProperty("attributes");
            string status = attributes.GetProperty("status").GetString();

            if (status == "failed")
            {
                _logger.LogError("The analysis for Domain {Domain} has failed", domain);
                return null;
            }

            if (status == "completed")
            {
                int statsMalicious = attributes.GetProperty("stats").GetProperty("malicious").GetInt32();
                int statsSuspicious = attributes.GetProperty("stats").GetProperty("suspicious").GetInt32();
                _logger.LogDebug("The analysis for Domain {Domain} has succeeded.", domain);
                return new RetryResult<RetryScanResult>
                {
                    IsSuccess = true,
                    Result = new RetryScanResult()
                    {
                        Malicious = statsMalicious,
                        Suspicious = statsSuspicious,
                        status = status
                    }
                };
            }
            return null;
        }, maxAttempts);

        if (result is null)
            _logger.LogWarning("Scann for domain {Domain} failed", domain);
        
        if (result.status != "completed")
        {
            _logger.LogError("Maximum retries ({maxAttempts}) reached while analyzing domain {Domain}", maxAttempts, domain);
            return;
        }

        var scanResult = new ScanResult
        {
            Domain = domain,
            IsBlacklisted = result.Malicious > 0 || result.Suspicious > 0,
            ScannerName = ScannerName,
            ScanResultUrl = $"https://www.virustotal.com/gui/domain-analysis/{analysisId}"
        };

        if (scanResult.IsBlacklisted)
        {
            _logger.LogInformation("Domain \"{Domain}\" is listed on {ScannerName}", domain, ScannerName);
        }
        else
        {
            _logger.LogInformation("Domain \"{Domain}\" is not listed on {ScannerName}", domain, ScannerName);
        }

        _messageService.AddResult(scanResult);

    }

    private class RetryScanResult
    {
        public int Malicious;
        public int Suspicious;
        public string status;
    }
}