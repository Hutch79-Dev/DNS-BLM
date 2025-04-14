namespace DNS_BLM.Infrastructure.Dtos;

public class ScanResult()
{
    public required string Domain { get; set; }
    public required string ScannerName { get; set; }
    public required bool IsBlacklisted { get; set; }
    public string ScanResultUrl { get; set; }
}