using DNS_BLM.Infrastructure.Dtos;

namespace DNS_BLM.Infrastructure.Services;

public class MessageService
{
    private List<ScanResult> _results = new List<ScanResult>();
    private HashSet<string> _domains = new HashSet<string>();

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
            allResults.Add(ArrangeResults(domainResults));
        }

        return string.Join(Environment.NewLine, allResults);
    }
    
    private string ArrangeResults(List<ScanResult> results)
    {
        List<string> resultList =
        [
            results.First().Domain
        ];
        foreach (var result in results)
        {
            if (result.IsBlacklisted)
            {
                resultList.Add("| " + result.ScannerName + ": Listed");
                resultList.Add("| URL: " + result.ScanResultUrl);
                resultList.Add("|");
            }
        }
        resultList.RemoveAt(resultList.Count - 1);
        resultList.Add(""); // add separator between domains
        return string.Join(Environment.NewLine, resultList);
    }
}


/* Example output
 *
 * 
 * Hutch79.ch
 * | VirusTotal: Listed
 * | URL: https://virustotal.com/xxx
 * |
 * | SomeOtherScanner: Success
 * | URL: https://example.ch
 *
 * Google.ch
 * | VirusTotal: Listed
 * | URL: https://virustotal.com/xxx
 * |
 * | SomeOtherScanner: Success
 * | URL: https://example.ch
 */