using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace DNS_BLM.Api
{
    using System.ComponentModel.DataAnnotations;

    public class AppConfiguration
    {
        public bool Debug { get; init; } = false;

        [Required]
        // [ConfigurationKeyName("ApiCredentials")]
        public ApiCredentialsConfiguration ApiCredentials { get; init; }

        [Required]
        [EmailAddress]
        // [ConfigurationKeyName("ReportReceiver")]
        public string ReportReceiver { get; init; }

        [Required]
        // [ConfigurationKeyName("Mail")]
        public MailConfiguration Mail { get; init; }

        [Required]
        // [ConfigurationKeyName("Domains")]
        public string[] Domains { get; init; }

        // [ConfigurationKeyName("Sentry")]
        public SentryConfiguration? Sentry { get; init; }

        [Required]
        // [ConfigurationKeyName("TimedTasks")]
        public TimedTasksSchedulesConfiguration TimedTasks { get; init; }
    }
}