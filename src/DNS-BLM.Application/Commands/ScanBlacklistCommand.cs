using DNS_BLM.Infrastructure.Services;
using DNS_BLM.Infrastructure.Services.ServiceInterfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DNS_BLM.Application.Commands
{
    public class ScanBlacklistCommand(string[] domains, bool sendMail = true) : IRequest<string>
    {
        public string[] Domains { get; } = domains;
        public bool SendMail { get; } = sendMail;
    }

    public class ScannBlacklistCommandHandler(
        IEnumerable<IBlacklistScanner> scanners, 
        MessageService messageService, 
        INotificationService notificationService,
        ILogger<ScannBlacklistCommandHandler> logger
        ) : IRequestHandler<ScanBlacklistCommand, string>
    {
        public async Task<string> Handle(ScanBlacklistCommand request, CancellationToken cancellationToken)
        {
            foreach (var scanner in scanners)
            {
                var scannerName = scanner.ScannerName;
                
                logger.LogInformation("Starting {ScannerName} scan for {DomainsCount} domains", scannerName, request.Domains.Length);
                await scanner.Scan(request.Domains, cancellationToken);
                logger.LogInformation("Completed {ScannerName} scan for {DomainsCount} domains", scannerName, request.Domains.Length);
            }
            var results = messageService.GetResults();
            messageService.Clear();

            if (request.SendMail && results is not null)
            {
                await notificationService.Notify("DNS-BLM Scanning Results", results);
            }
            else
            {
                if (results is null)
                    results = "No blacklisted Domains.";
                logger.LogInformation("Mail notification skipped as per request");
            }
            
            return results;
        }
    }
}
