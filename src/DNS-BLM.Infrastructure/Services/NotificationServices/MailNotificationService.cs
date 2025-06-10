using System.Net;
using System.Net.Mail;
using DNS_BLM.Api;
using DNS_BLM.Infrastructure.Services.ServiceInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DNS_BLM.Infrastructure.Services.NotificationServices;

public class MailNotificationService(IConfiguration configuration, ILogger<MailNotificationService> logger, IOptions<AppConfiguration> appConfiguration) : INotificationService
{
    public async Task Notify(string subject, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message, nameof(message));

        int maxRetries = 3;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                using var client = CreateSmtpClient();
                var from = configuration.GetValue<string>("DNS-BLM:Mail:From");
                using (var mailMessage = new MailMessage
                       {
                           From = new MailAddress(from),
                           Subject = subject,
                           Body = message,
                           IsBodyHtml = false
                       })
                {
                    var reportReceiver = configuration.GetValue<string>("DNS-BLM:ReportReceiver");
                    ArgumentException.ThrowIfNullOrWhiteSpace(reportReceiver, nameof(reportReceiver));
                    mailMessage.To.Add(reportReceiver);
                    await client.SendMailAsync(mailMessage);
                    logger.LogDebug("Successfully send Mail Notification");
                    return;
                }
            }
            catch (SmtpException ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                    throw;

                logger.LogError(ex, "Failed to send Mail Notification. Retry {retryCount}/{maxRetries}", retryCount, maxRetries);
                await Task.Delay(1000 * retryCount);
            }
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        var host = appConfiguration.Value.Mail.Host;
        var port = appConfiguration.Value.Mail.Port;
        var username = appConfiguration.Value.Mail.Username;
        var password = appConfiguration.Value.Mail.Password;
        var enableSsl = appConfiguration.Value.Mail.EnableSsl;

        return new SmtpClient(host)
        {
            Port = port,
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl,
            Timeout = 30000
        };
    }
}