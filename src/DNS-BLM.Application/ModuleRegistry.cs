using System.Net.Http.Headers;
using DNS_BLM.Infrastructure.Services.NotificationServices;
using DNS_BLM.Infrastructure.Services.ScannerServices;
using DNS_BLM.Infrastructure.Services.ServiceInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DNS_BLM.Application;

public static class ModuleRegistry
{
    public static void AddApplicationModule(this IServiceCollection services)
    {
        // services.AddValidatorsFromAssembly(typeof(ModuleRegistry).Assembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(typeof(ModuleRegistry).Assembly);
            // cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            // cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorisationBehavior<,>));
        });
    }
}