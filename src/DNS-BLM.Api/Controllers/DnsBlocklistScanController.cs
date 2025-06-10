using DNS_BLM.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DNS_BLM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DnsBlocklistScanController(IMediator mediator, IOptions<AppConfiguration> appConfiguration) : ControllerBase
{

    [HttpGet("ScanConfiguredDomains", Name = "ScanConfiguredDomains")]
    public async Task<string> ScanConfiguredDomains()
    {
        var domains = appConfiguration.Value.Domains;
        
        if (domains == null) throw new Exception("Domains not found");
        
        return await mediator.Send(new ScanBlacklistCommand(domains, false));
    }

    // [HttpGet("ScanCustomDomain", Name = "ScanCustomDomain")]
    // public async Task<string> ScanCustomDomain(string domain)
    // {
    //     ArgumentException.ThrowIfNullOrWhiteSpace(domain, nameof(domain));
    //     
    //     var domains = new List<string> {domain};
    //     var result =  await _mediator.Send(new ScannBlacklistCommand(domains));
    //     return result.First();
    // }
}