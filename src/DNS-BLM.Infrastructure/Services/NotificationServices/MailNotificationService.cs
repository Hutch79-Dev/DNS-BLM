using System.Net;
using System.Net.Mail;
using System.Text;
using DNS_BLM.Infrastructure.Services.ServiceInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DNS_BLM.Infrastructure.Services.NotificationServices;

public class MailNotificationService : INotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailNotificationService> _logger;

    public MailNotificationService(IConfiguration configuration, ILogger<MailNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var host = _configuration.GetValue<string>("DNS-BLM:Mail:Host");
        var port = _configuration.GetValue<int>("DNS-BLM:Mail:Port");
        var username = _configuration.GetValue<string>("DNS-BLM:Mail:Username");
        var password = _configuration.GetValue<string>("DNS-BLM:Mail:Password");
        
        if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port), "Port must be greater than 0");
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Host must not be empty", nameof(host));
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username must not be empty", nameof(username));
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password must not be empty", nameof(password));
    }

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
                var from = _configuration.GetValue<string>("DNS-BLM:Mail:From");
                using (var mailMessage = new MailMessage
                       {
                           From = new MailAddress(from),
                           Subject = subject,
                           Body = message,
                           IsBodyHtml = false
                       })
                {
                    var reportReceiver = _configuration.GetValue<string>("DNS-BLM:ReportReceiver");
                    ArgumentException.ThrowIfNullOrWhiteSpace(reportReceiver, nameof(reportReceiver));
                    mailMessage.To.Add(reportReceiver);
                    await client.SendMailAsync(mailMessage);
                    _logger.LogDebug("Successfully send Mail Notification");
                    return;
                }
            }
            catch (SmtpException ex)
            {
                retryCount++;
                if (retryCount >= maxRetries)
                    throw;
                
                _logger.LogError(ex, "Failed to send Mail Notification. Retry {retryCount}/{maxRetries}", retryCount, maxRetries);
                await Task.Delay(1000 * retryCount);
            }
        }
    }
    
    private SmtpClient CreateSmtpClient()
    {
        var host = _configuration.GetValue<string>("DNS-BLM:Mail:Host");
        var port = _configuration.GetValue<int>("DNS-BLM:Mail:Port");
        var username = _configuration.GetValue<string>("DNS-BLM:Mail:Username");
        var password = _configuration.GetValue<string>("DNS-BLM:Mail:Password");
        var enableSsl = _configuration.GetValue<bool>("DNS-BLM:Mail:EnableSsl");
    
        return new SmtpClient(host)
        {
            Port = port,
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl,
            Timeout = 30000
        };
    }
}