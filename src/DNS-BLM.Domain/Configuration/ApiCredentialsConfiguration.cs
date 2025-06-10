using System.ComponentModel.DataAnnotations;

namespace DNS_BLM.Api;

public class ApiCredentialsConfiguration
{
    [Required]
    public string VirusTotal { get; init; } = string.Empty;
}