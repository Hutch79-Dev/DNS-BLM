using System.ComponentModel.DataAnnotations;

namespace DNS_BLM.Domain.Configuration;

public class TimedTasksSchedulesConfiguration
{
    [Required]
    public string ScanBlacklistProviders { get; init; } = "9 6 * * *";
}