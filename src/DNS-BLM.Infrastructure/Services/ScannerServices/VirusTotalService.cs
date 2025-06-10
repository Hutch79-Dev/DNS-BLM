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

        string status = string.Empty;
        int statsMalicious = 0;
        int statsSuspicious = 0;
        int maxAttempts = 3;

        for (int attempt = 0; attempt <= maxAttempts; attempt++)
        {
            var response = await client.GetAsync($"analyses/{analysisId}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(responseBody);
            var attributes = doc.RootElement.GetProperty("data").GetProperty("attributes");
            status = attributes.GetProperty("status").GetString();

            if (status == "failed")
            {
                _logger.LogError("The analysis for Domain {Domain} has failed", domain);
                return;
            }

            if (status == "completed")
            {
                statsMalicious = attributes.GetProperty("stats").GetProperty("malicious").GetInt32();
                statsSuspicious = attributes.GetProperty("stats").GetProperty("suspicious").GetInt32();
                _logger.LogDebug("The analysis for Domain {Domain} has succeeded after {attempt}/{maxAttempts} attempts.", domain, attempt, maxAttempts);
                break;
            }
            
            cancellationToken.ThrowIfCancellationRequested();
            var delay = CalculateBackoffTime(attempt);
            _logger.LogDebug("No valide result for domain {Domain}. Wait for {delay} seconds before retrying.", domain, delay);
            await Task.Delay(delay * 1000, cancellationToken);
        }

        if (status != "completed")
        {
            _logger.LogError("Maximum retries ({maxAttempts}) reached while analyzing domain {Domain}", maxAttempts, domain);
            return;
        }

        var scanResult = new ScanResult
        {
            Domain = domain,
            IsBlacklisted = statsMalicious > 0 || statsSuspicious > 0,
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

    /// <summary>
    /// Returns the delay for a given retry in seconds
    /// </summary>
    /// <param name="numberOfAttempts"></param>
    /// <returns></returns>
    private static int CalculateBackoffTime(int numberOfAttempts)
    {
        numberOfAttempts += 3; // Increase attempts to skip 1 and 5 second delays
        int totalSeconds = 0;

        for (int attempt = 1; attempt <= numberOfAttempts; attempt++)
        {
            // Each attempt adds attempt² seconds of delay
            totalSeconds += attempt * attempt;
        }

        return totalSeconds;
    }
}