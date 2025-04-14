using DNS_BLM.Infrastructure.Dtos;

namespace DNS_BLM.Infrastructure.Services.ServiceInterfaces;

public interface IBlacklistScanner
{
    public Task Scan(List<string> domains);
    public string ScannerName { get; }
}