using System.Reflection;
using DNS_BLM.Api;
using DNS_BLM.Api.Services;
using DNS_BLM.Api.TimedTasks;
using DNS_BLM.Application;
using DNS_BLM.Infrastructure;
using Scalar.AspNetCore;
using Sentry.Extensibility;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<AppConfiguration>()
    .Bind(builder.Configuration.GetSection("DNS-BLM"))
    .ValidateDataAnnotations();

var dnsBlmSettings = builder.Configuration.GetSection("DNS-BLM").Get<AppConfiguration>();

if (dnsBlmSettings is null)
    throw new ArgumentNullException(nameof(dnsBlmSettings), "No settings provided.");
builder.Services.AddSingleton(dnsBlmSettings);

if (dnsBlmSettings.Sentry is not null)
{
    builder.WebHost.UseSentry(o =>
    {
        o.Dsn = dnsBlmSettings.Sentry.Dsn;
        o.TracesSampleRate = dnsBlmSettings.Sentry.TracesSampleRate;
        
        if (!Enum.TryParse<RequestSize>(dnsBlmSettings.Sentry.MaxRequestBodySize, out var requestSize))
            throw new InvalidOperationException($"Invalid RequestSize value: {dnsBlmSettings.Sentry.MaxRequestBodySize}");
        
        o.MaxRequestBodySize = requestSize;
        o.SendDefaultPii = dnsBlmSettings.Sentry.SendDefaultPii;
    });
}

if (dnsBlmSettings.Debug)
    builder.Logging.AddFilter("DNS_BLM", LogLevel.Debug);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddApplicationModule();
builder.Services.AddInfrastructureModule(dnsBlmSettings);

builder.Services.AddTimedTaskModules();


var app = builder.Build();

// if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

var version = Assembly.GetEntryAssembly()!.GetSemanticVersion();
app.Logger.LogInformation($"DNS-BLM Version: {version}");

app.Run();