using DNS_BLM.Infrastructure.Services;
using DNS_BLM.Infrastructure.Services.ServiceInterfaces;
using MediatR;

namespace DNS_BLM.Application.Commands
{
    public class ScannBlacklistCommand(List<string> domains) : IRequest<string>
    {
        public List<string> Domains { get; } = domains;
    }
    
    public class ScannBlacklistCommandHandler(IEnumerable<IBlacklistScanner> scanners, MessageService messageService) : IRequestHandler<ScannBlacklistCommand, string>
    {
        public async Task<string> Handle(ScannBlacklistCommand request, CancellationToken cancellationToken)
        {
            foreach (var scanner in scanners)
            {
                await scanner.Scan(request.Domains);
            }

            return messageService.GetResults();
        }
    }
}