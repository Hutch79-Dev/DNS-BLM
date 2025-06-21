using System.Text.Json;
using DNS_BLM.Infrastructure.Dtos;
using DNS_BLM.Infrastructure.Services.ServiceInterfaces;
using Microsoft.Extensions.Logging;

namespace DNS_BLM.Infrastructure.Services.ScannerServices;

public class VirusTotalService(
    IHttpClientFactory httpClientFactory,
    MessageService messageService,
    ILogger<VirusTotalService> logger,
    RetryService retryService)
    : IBlacklistScanner
{
    public string ScannerName => "VirusTotal";

    public async Task Scan(string[] domains, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(ScannerName);
        foreach (var domain in domains)
        {
            logger.LogInformation("Scanning domain {Domain} with {ScannerName}", domain, ScannerName);
            try
            {
                const int maxAttempts = 3;
                var result = await retryService.Retry(async () =>
                {
                    using HttpResponseMessage analysisResponse = await client.PostAsync($"domains/{domain}/analyse", new StringContent(string.Empty), cancellationToken);
                    analysisResponse.EnsureSuccessStatusCode();
                    var analysisResponseBody = await analysisResponse.Content.ReadAsStringAsync(cancellationToken);
                    return new RetryResult<string>()
                    {
                        IsSuccess = true,
                        Result = analysisResponseBody,
                    };
                }, maxAttempts, cancellationToken);

                if (string.IsNullOrWhiteSpace(result))
                    logger.LogError("Scanning domain {Domain} failed", domain);

                string? analysisId;
                using (var doc = JsonDocument.Parse(result))
                {
                    var root = doc.RootElement;
                    analysisId = root.GetProperty("data").GetProperty("id").GetString();
                }

                logger.LogDebug("Received analysis ID {AnalysisId} for domain {Domain}", analysisId, domain);

                await Task.Delay(5000, cancellationToken);
                await Analyze(analysisId, domain, client, cancellationToken);
            }
            catch (HttpRequestException e)
            {
                logger.LogError(e, "HTTP error while scanning domain {Domain} with {ScannerName}", domain, ScannerName);
            }
            catch (JsonException e)
            {
                logger.LogError(e, "JSON parsing error while scanning domain {Domain} with {ScannerName}", domain, ScannerName);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unexpected error while scanning domain {Domain} with {ScannerName}", domain, ScannerName);
            }
        }
    }

    private async Task Analyze(string? analysisId, string domain, HttpClient client, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(analysisId))
        {
            logger.LogError("The analysis ID for Domain {Domain} must not be null!", domain);
            return;
        }

        const int maxAttempts = 3;
        var result = await retryService.Retry(async () =>
        {
            var response = await client.GetAsync($"analyses/{analysisId}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(responseBody);
            var attributes = doc.RootElement.GetProperty("data").GetProperty("attributes");
            string status = attributes.GetProperty("status").GetString();

            if (status == "failed")
            {
                logger.LogError("The analysis for Domain {Domain} has failed", domain);
                return null;
            }

            if (status == "completed")
            {
                int statsMalicious = attributes.GetProperty("stats").GetProperty("malicious").GetInt32();
                int statsSuspicious = attributes.GetProperty("stats").GetProperty("suspicious").GetInt32();
                logger.LogDebug("The analysis for Domain {Domain} has succeeded.", domain);
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
        }, maxAttempts, cancellationToken);

        if (result is null)
        {
            logger.LogWarning("The analysis for Domain {Domain} has failed", domain);
            return;
        }

        if (result.status != "completed")
        {
            logger.LogError("Maximum retries ({maxAttempts}) reached while analyzing domain {Domain}", maxAttempts, domain);
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
            logger.LogInformation("Domain \"{Domain}\" is listed on {ScannerName}", domain, ScannerName);
        }
        else
        {
            logger.LogInformation("Domain \"{Domain}\" is not listed on {ScannerName}", domain, ScannerName);
        }

        messageService.AddResult(scanResult);
    }

    private class RetryScanResult
    {
        public int Malicious;
        public int Suspicious;
        public string status;
    }
}