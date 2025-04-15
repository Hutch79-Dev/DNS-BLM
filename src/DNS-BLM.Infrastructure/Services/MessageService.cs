using DNS_BLM.Infrastructure.Dtos;

namespace DNS_BLM.Infrastructure.Services;

public class MessageService
{
    private List<ScanResult> _results = [];
    private HashSet<string> _domains = [];

    public void AddResult(ScanResult result)
    {
        _results.Add(result);
        _domains.Add(result.Domain);
    }

    public string GetResults()
    {
        List<string> allResults = new List<string>();
        foreach (var domain in _domains)
        {
            var domainResults = _results.Where(r => r.Domain == domain).ToList();
            var result = ArrangeResults(domainResults);
            if (result == null) continue;
            allResults.Add(result);
        }

        return string.Join(Environment.NewLine, allResults);
    }

    private static string? ArrangeResults(List<ScanResult> results)
    {
        if (!results.Any(r => r.IsBlacklisted)) // no blacklisted domains
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
