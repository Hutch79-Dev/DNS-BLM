using DNS_BLM.Application.Commands;
using MediatR;

namespace DNS_BLM.Api.TimedTasks.Tasks;

public class ScanBlacklistProviders(ILogger<ScanBlacklistProviders> logger, IServiceProvider serviceProvider) : TimedHostedService(logger, serviceProvider)
{
    protected override string TaskName => "ScannBlacklistProviders";

    protected override async Task ExecuteTimedTask(object? state = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            
        var domains = configuration.GetSection("DNS-BLM:Domains").Get<List<string>>();
        
        if (domains == null) throw new Exception("Domains not found");
        await mediator.Send(new ScanBlacklistCommand(domains));
    }

    protected override TimeSpan GetExecutionTime()
    {
        return new TimeSpan(2, 0, 0);
    }

    protected override TimeSpan GetInterval()
    {
        return TimeSpan.FromDays(1);
    }
}