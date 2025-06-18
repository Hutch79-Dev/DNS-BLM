using DNS_BLM.Application.Commands;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace VivavisTool2.Tests;

public class DatabaseTestCase
{
    public IMediator Mediator { get; private set; }
    public IConfigurationRoot ConfigurationRoot { get; private set; }

    public DatabaseTestCase()
    {
        var services = new ServiceCollection();
        ConfigurationRoot = new ConfigurationBuilder()
            .AddUserSecrets<DatabaseTestCase>()
            .Build();
        
        var assembly = typeof(ScanBlacklistCommand).Assembly;
        // services.AddValidatorsFromAssembly(assembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assembly);
            // cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });

        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        Mediator = serviceProvider.GetRequiredService<IMediator>();
    }
}