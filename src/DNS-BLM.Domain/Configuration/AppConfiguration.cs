using System.ComponentModel.DataAnnotations;

namespace DNS_BLM.Domain.Configuration
{
    public class AppConfiguration
    {
        public bool Debug { get; init; } = false;

        [Required]
        public ApiCredentialsConfiguration ApiCredentials { get; init; }

        [Required]
        [EmailAddress]
        public string ReportReceiver { get; init; }

        [Required]
        public MailConfiguration Mail { get; init; }

        [Required]
        // [ConfigurationKeyName("Domains")]
        public string[] Domains { get; init; }

        // [ConfigurationKeyName("Sentry")]
        public SentryConfiguration? Sentry { get; init; }

        [Required]
        public TimedTasksSchedulesConfiguration TimedTasks { get; init; }
    }
}