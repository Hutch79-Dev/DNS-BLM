using DNS_BLM.Api.TimedTasks;
using DNS_BLM.Application;
using Scalar.AspNetCore;
using Sentry.Extensibility;

var builder = WebApplication.CreateBuilder(args);
if (builder.Configuration.GetSection("DNS-BLM:Sentry").Exists())
{
    builder.WebHost.UseSentry(o =>
    {
        o.Dsn = builder.Configuration.GetSection("DNS-BLM:Sentry").GetValue<string>("Dsn");
        o.TracesSampleRate = builder.Configuration.GetSection("DNS-BLM:Sentry").GetValue<double>("TracesSampleRate");
        o.MaxRequestBodySize = builder.Configuration.GetSection("DNS-BLM:Sentry").GetValue<RequestSize>("MaxRequestBodySize");
        o.SendDefaultPii = builder.Configuration.GetSection("DNS-BLM:Sentry").GetValue<bool>("SendDefaultPii");
    });
}

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddApplicationModule(builder.Configuration);
builder.Services.AddTimedTaskModules();


var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();