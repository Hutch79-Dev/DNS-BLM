using DNS_BLM.Domain.Configuration;
using DNS_BLM.Infrastructure.Services.ServiceInterfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace DNS_BLM.Infrastructure.Services.NotificationServices;

public class MailNotificationService(ILogger<MailNotificationService> logger, IOptions<AppConfiguration> appConfiguration) : INotificationService
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
                using var client = await CreateSmtpClient();
                var mailMessage = new MimeMessage();
                
                var reportReceiver = appConfiguration.Value.ReportReceiver;
                ArgumentException.ThrowIfNullOrWhiteSpace(reportReceiver, nameof(reportReceiver));
                
                mailMessage.From.Add(MailboxAddress.Parse(appConfiguration.Value.Mail.From));
                mailMessage.To.Add(MailboxAddress.Parse(reportReceiver));
                mailMessage.Subject = subject;
                
                var builder = new BodyBuilder ();
                builder.HtmlBody = message;

                mailMessage.Body = builder.ToMessageBody();
                
                await client.SendAsync(mailMessage);
                await client.DisconnectAsync(true);
                
                logger.LogDebug("Successfully send Mail Notification");
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                    throw;

                logger.LogError(ex, "Failed to send Mail Notification. Retry {retryCount}/{maxRetries}", retryCount, maxRetries);
                await Task.Delay(1000 * retryCount);
            }
        }
    }

    private async Task<SmtpClient> CreateSmtpClient()
    {
        var host = appConfiguration.Value.Mail.Host;
        var port = appConfiguration.Value.Mail.Port;
        var username = appConfiguration.Value.Mail.Username;
        var password = appConfiguration.Value.Mail.Password;
        var enableSsl = appConfiguration.Value.Mail.EnableSsl;

        ArgumentException.ThrowIfNullOrWhiteSpace(host, nameof(host));
        ArgumentException.ThrowIfNullOrWhiteSpace(username, nameof(username));
        ArgumentException.ThrowIfNullOrWhiteSpace(password, nameof(password));
        
        var client = new SmtpClient();
        await client.ConnectAsync(host, port, enableSsl);
        await client.AuthenticateAsync(username, password);
        
        return client;
    }
}