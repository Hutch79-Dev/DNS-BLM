using System.Collections.Concurrent;
using DNS_BLM.Domain.Configuration;
using DNS_BLM.Infrastructure.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNS_BLM.Infrastructure.Services;

public class MessageService(ILogger<MessageService> logger, TemplateService templateService, IOptions<AppConfiguration> appConfiguration)
{
    private ConcurrentBag<ScanResult> _results = [];
    private HashSet<string> _domains = [];

    public void AddResult(ScanResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(result.Domain);
        ArgumentException.ThrowIfNullOrWhiteSpace(result.ScannerName);

        _results.Add(result);
        _domains.Add(result.Domain);
        
        logger.LogDebug("Added result for {Domain} from {ScannerName} - Blacklisted: {IsBlacklisted}", result.Domain, result.ScannerName, result.IsBlacklisted);
    }

    public string? GetResults()
    {
        if (!_results.Any(r => r.IsBlacklisted))
        {
            logger.LogDebug("No blacklisted results available to return from MessageService");
            return null;
        }

        logger.LogDebug("There are {ResultsCount} results: ", _results.Count);
        foreach (var result in _results)
        {
            logger.LogDebug($"{result.Domain}: {result.ScannerName} - {result.IsBlacklisted}");
        }
        
        var domainResults = _results.Where(r => r.IsBlacklisted).ToArray();
        var results = templateService.RenderTemplate(domainResults.ToList(), appConfiguration.Value.Mail.MailTemplate);
        return results;
    }

    public void Clear()
    {
        _results = [];
        _domains = [];
    }

    private static string? ArrangeResults(ScanResult[] results)
    {
        if (!results.Any(r => r.IsBlacklisted)) // no blocklisted domains
            return null;

        List<string> resultList =
        [
            results.First().Domain
        ];

        foreach (var result in results)
        {
            if (result.IsBlacklisted)
            {
                resultList.Add("| " + result.ScannerName + ": Listed");
                if (!string.IsNullOrEmpty(result.ScanResultUrl))
                    resultList.Add("| URL: " + result.ScanResultUrl);
                resultList.Add("|");
            }
        }

        resultList.RemoveAt(resultList.Count - 1); // remove trialing |
        resultList.Add(string.Empty); // add separator between domains
        return string.Join(Environment.NewLine, resultList);
    }
}