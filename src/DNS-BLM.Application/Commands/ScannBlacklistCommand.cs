using DNS_BLM.Application.Services;
using MediatR;

namespace DNS_BLM.Application.Commands
{
    public class ScannBlacklistCommand : IRequest<List<string>>
    {
        public List<string> Domains { get; }

        public ScannBlacklistCommand(List<string> domains)
        {
            Domains = domains;
        }
    }
    
    public class ScannBlacklistCommandHandler : IRequestHandler<ScannBlacklistCommand, List<string>>
    {
        private readonly IBlacklistScanner _scanner;

        public ScannBlacklistCommandHandler(IBlacklistScanner scanner)
        {
            _scanner = scanner;
        }

        public async Task<List<string>> Handle(ScannBlacklistCommand request, CancellationToken cancellationToken)
        {
            return await _scanner.Scan(request.Domains);
        }
    }
}