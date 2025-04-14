using System.Text.Json;
using DNS_BLM.Infrastructure.Dtos;
using DNS_BLM.Infrastructure.Services.ServiceInterfaces;

namespace DNS_BLM.Infrastructure.Services.ScannerServices;

public class VirusTotalService(IHttpClientFactory httpClientFactory, MessageService messageService) : IBlacklistScanner
{
    public string ScannerName => "VirusTotal";
    public async Task Scan(List<string> domains)
    {
        var client = httpClientFactory.CreateClient(ScannerName);

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
                    analysisId = root.GetProperty("data").GetProperty("id").GetString();
                }

                var response = await client.GetAsync($"analyses/{analysisId}");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();

                int statsMalicious;
                int statsSuspicious;
                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = doc.RootElement;
                    var statsSection = root.GetProperty("data").GetProperty("attributes").GetProperty("stats");
                    statsMalicious = statsSection.GetProperty("malicious").GetInt32();
                    statsSuspicious = statsSection.GetProperty("suspicious").GetInt32();
                }

                if (statsMalicious > 0 || statsSuspicious > 0)
                {
                    var hui = new ScanResult
                    {
                        Domain = domain,
                        IsBlacklisted = true,
                        ScannerName = ScannerName,
                    };
                    messageService.AddResult(hui);
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"\nException Caught!");
                Console.WriteLine($"Message :{e}");
            }
        }
    }
}