using DNS_BLM.Infrastructure.Dtos;
using Microsoft.Extensions.Logging;

namespace DNS_BLM.Infrastructure.Services;

public class MessageService(ILogger<MessageService> logger)
{
    private List<ScanResult> _results = [];
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
        if (_results.Any(r => r.IsBlacklisted))
        {
            logger.LogWarning("No results available to return from MessageService");
            return null;
        }

        List<string> allResults = new List<string>();
        foreach (var domain in _domains)
        {
            var domainResults = _results.Where(r => r.Domain == domain).ToList();
            var result = ArrangeResults(domainResults);
            if (result == null) continue;
            allResults.Add(result);
        }

        var results = string.Join(Environment.NewLine, allResults);
        if (String.IsNullOrWhiteSpace(results))
            logger.LogError("No results available to return, despite passing previous checks!");
        ArgumentException.ThrowIfNullOrWhiteSpace(results, nameof(results));
        
        return results;
    }

    public void Clear()
    {
        _results = [];
        _domains = [];
    }

    private static string? ArrangeResults(List<ScanResult> results)
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