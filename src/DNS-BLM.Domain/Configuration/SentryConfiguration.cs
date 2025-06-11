using System.ComponentModel.DataAnnotations;

namespace DNS_BLM.Domain.Configuration;

public class SentryConfiguration
{
    [Required]
    [Url]
    public string Dsn { get; init; } = string.Empty;

    [Range(0.0, 1.0)]
    public double TracesSampleRate { get; init; }

    [Required]
    public string MaxRequestBodySize { get; init; } = string.Empty;

    public bool SendDefaultPii { get; init; }
}