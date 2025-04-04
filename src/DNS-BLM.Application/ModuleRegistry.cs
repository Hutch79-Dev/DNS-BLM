using System.Net.Http.Headers;
using DNS_BLM.Application.Services;
using DNS_BLM.Application.Services.NotificationServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DNS_BLM.Application;

public static class ModuleRegistry
{
    public static void AddApplicationModule(this IServiceCollection services, IConfiguration configuration)
    {
        // services.AddValidatorsFromAssembly(typeof(ModuleRegistry).Assembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(typeof(ModuleRegistry).Assembly);
            // cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            // cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorisationBehavior<,>));
        });

        services.AddSingleton<INotificationService, MailNotificationService>();


        string? virusTotalApiKey = configuration["DNS-BLM:API_Credentials:VirusTotal"];
        if (virusTotalApiKey != null)
        {
            var type = typeof(IBlacklistScanner).Assembly.GetTypes().Where(e => e == typeof(IBlacklistScanner)).SingleOrDefault(e => e.Name == "VirusTotalService");

            services.AddSingleton(typeof(IBlacklistScanner), type);

            services.AddHttpClient("VirusTotal", client =>
            {
                client.BaseAddress = new Uri("https://www.virustotal.com/api/v3/");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-apikey", virusTotalApiKey);
            });
        }
    }
}