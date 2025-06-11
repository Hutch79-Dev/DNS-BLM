using System.ComponentModel.DataAnnotations;

namespace DNS_BLM.Domain.Configuration;

public class ApiCredentialsConfiguration
{
    [Required]
    public string VirusTotal { get; init; } = string.Empty;
}