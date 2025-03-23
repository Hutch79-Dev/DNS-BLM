namespace DNS_BLM.Application.Services;

public interface IBlacklistScanner
{
    public Task<List<string>> Scan(List<string> domains);
}