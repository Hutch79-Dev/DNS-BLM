using DNS_BLM.Application.Commands;
using MediatR;
using Microsoft.Extensions.Options;

namespace DNS_BLM.Api.TimedTasks.Tasks;

public class ScanBlacklistProviders(ILogger<ScanBlacklistProviders> logger, IMediator mediator, IOptions<AppConfiguration> appConfiguration) : TimedHostedService(logger, appConfiguration)
{
    protected override string TaskName => "ScanBlacklistProviders";

    protected override async Task ExecuteTimedTask(object? state = null)
    {
        await mediator.Send(new ScanBlacklistCommand(appConfiguration.Value.Domains));
    }
}