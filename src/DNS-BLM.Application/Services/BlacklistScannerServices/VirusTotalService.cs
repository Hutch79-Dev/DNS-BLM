using System.Text.Json;

namespace DNS_BLM.Application.Services.BlacklistScannerServices;

public class VirusTotalService : IBlacklistScanner
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly INotificationService _notificationService;

    public VirusTotalService(IHttpClientFactory httpClientFactory, INotificationService notificationService)
    {
        _httpClientFactory = httpClientFactory;
        _notificationService = notificationService;
    }

    public async Task<List<string>> Scan(List<string> domains)
    {
        var client = _httpClientFactory.CreateClient("VirusTotal");
        List<string> scannResults = new();

        foreach (var domain in domains)
        {
            try
            {
                var analysisResponse = await client.PostAsync($"domains/{domain}/analyse", new StringContent(""));
                analysisResponse.EnsureSuccessStatusCode();
                string analysisResponseBody = await analysisResponse.Content.ReadAsStringAsync();

                string analysisId;
                using (JsonDocument doc = JsonDocument.Parse(analysisResponseBody))
                {
                    JsonElement root = doc.RootElement;
                    analysisId = root.GetProperty("data").GetProperty("id").GetString(); // ToDo: Parsing in Object
                }

                var response = await client.GetAsync($"analyses/{analysisId}");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();

                int statsMalicious;
                int statsSuspicious;
                int statsUndetected;
                int statsHarmless;
                int statsTimeout;
                DateTime lastAnalysisDate;
                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = doc.RootElement;
                    var statsSection = root.GetProperty("data").GetProperty("attributes").GetProperty("stats"); // ToDo: Parsing in Object
                    statsMalicious = statsSection.GetProperty("malicious").GetInt32();
                    statsSuspicious = statsSection.GetProperty("suspicious").GetInt32();
                    statsUndetected = statsSection.GetProperty("undetected").GetInt32();
                    statsHarmless = statsSection.GetProperty("harmless").GetInt32();
                    statsTimeout = statsSection.GetProperty("timeout").GetInt32();
                    
                    var unixTimestamp = root.GetProperty("data").GetProperty("attributes").GetProperty("date").GetInt32();
                    lastAnalysisDate = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
                }
                // ToDo: Message Builder in eigenen Service auslagern
                string statsSummary = $@"
Statistics Summary for Domain {domain}:
---------------------------------------------------
Malicious:      {statsMalicious}
Suspicious:     {statsSuspicious}
Undetected:     {statsUndetected}
Harmless:       {statsHarmless}
Timeout:        {statsTimeout}
Last Analysis:  {lastAnalysisDate:yyyy-MM-dd HH:mm:ss}
---------------------------------------------------
";
                Console.WriteLine(statsSummary);
                scannResults.Add(statsSummary);

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"\nException Caught!");
                Console.WriteLine($"Message :{e}");
            }
        }

        if (scannResults.Count > 0)
        {
            await _notificationService.Notify($"DNS-BLM Scanning Results", scannResults);
        }
        return scannResults;
    }
}