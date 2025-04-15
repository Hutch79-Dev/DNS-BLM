using DNS_BLM.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DNS_BLM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DnsBlocklistScanController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public DnsBlocklistScanController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    [HttpGet("ScanConfiguredDomains", Name = "ScanConfiguredDomains")]
    public async Task<string> ScanConfiguredDomains()
    {
        var domains = _configuration.GetSection("DNS-BLM:Domains").Get<List<string>>();
        
        if (domains == null) throw new Exception("Domains not found");
        
        return await _mediator.Send(new ScannBlacklistCommand(domains, false));
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