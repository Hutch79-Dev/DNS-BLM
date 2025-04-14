using DNS_BLM.Infrastructure.Services;
using DNS_BLM.Infrastructure.Services.ServiceInterfaces;
using MediatR;

namespace DNS_BLM.Application.Commands
{
    public class ScannBlacklistCommand : IRequest<string>
    {
        public List<string> Domains { get; }

        public ScannBlacklistCommand(List<string> domains)
        {
            Domains = domains;
        }
    }
    
    public class ScannBlacklistCommandHandler : IRequestHandler<ScannBlacklistCommand, string>
    {
        private readonly IBlacklistScanner _scanner;
        private readonly MessageService _messageService;

        public ScannBlacklistCommandHandler(IBlacklistScanner scanner, MessageService messageService)
        {
            _scanner = scanner;
            _messageService = messageService;
        }

        public async Task<string> Handle(ScannBlacklistCommand request, CancellationToken cancellationToken)
        {
            // foreach (var scanner in _scanner)
            // {
                await _scanner.Scan(request.Domains);
            // }

            return _messageService.GetResults();
        }
    }
}