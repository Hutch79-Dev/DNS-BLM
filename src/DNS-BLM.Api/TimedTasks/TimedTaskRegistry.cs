using DNS_BLM.Api.TimedTasks.Tasks;
using DNS_BLM.Application.Commands;

namespace DNS_BLM.Api.TimedTasks;

public static class TimedTaskRegistry
{
    public static void AddTimedTaskModules(this IServiceCollection services)
    {
        services.AddHostedService<ScannBlacklistProviders>();
    }
}