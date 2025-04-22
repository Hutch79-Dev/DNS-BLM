using DNS_BLM.Infrastructure.Services;
using DNS_BLM.Infrastructure.Services.ServiceInterfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DNS_BLM.Application.Commands
{
    public class ScanBlacklistCommand(List<string> domains, bool sendMail = true) : IRequest<string>
    {
        public List<string> Domains { get; } = domains;
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
                
                logger.LogInformation("Starting {ScannerName} scan for {DomainsCount} domains", scannerName, request.Domains.Count);
                await scanner.Scan(request.Domains, cancellationToken);
                logger.LogInformation("Completed {ScannerName} scan for {DomainsCount} domains", scannerName, request.Domains.Count);
            }
            var results = messageService.GetResults();
            messageService.Clear();
            
            if (!request.SendMail)
                await notificationService.Notify("DNS-BLM Scanning Results", results);
            
            return results;
        }
    }
}
