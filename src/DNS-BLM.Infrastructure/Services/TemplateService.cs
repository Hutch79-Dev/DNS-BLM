using System.Collections.Concurrent;
using DNS_BLM.Infrastructure.Dtos;
using HandlebarsDotNet;
using Microsoft.Extensions.Logging;

namespace DNS_BLM.Infrastructure.Services;

public class TemplateService() // ILogger<TemplateService> logger
{
    private readonly ConcurrentDictionary<string, HandlebarsTemplate<object, object>> _compiledTemplates = new();

    public string RenderTemplate(List<ScanResult> model, string? template = null)
    {
        string defaultTemplate =
            """
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8" />
              <title>DNS-BLM Results</title>
            </head>
            <body style="margin:0; padding:0; background-color:#f4f4f4;">
              <center>
                <table width="100%" bgcolor="#f4f4f4" cellpadding="0" cellspacing="0" border="0">
                  <tr>
                    <td align="center">
                      <table width="600" cellpadding="0" cellspacing="0" border="0" bgcolor="#ffffff" style="border:1px solid #ddd;">
                        <tr>
                          <td align="center" bgcolor="#004080" style="padding:24px 0 24px 0; color:#ffffff; font-size:24px; font-family:Arial,sans-serif; font-weight:bold;">
                            DNS-BLM Results
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:24px 24px 24px 24px;">
                            <table width="100%" cellpadding="8" cellspacing="0" border="1" style="border-collapse:collapse; border-color:#ddd; font-family:Arial,sans-serif; font-size:14px;">
                              <thead>
                                <tr bgcolor="#e6f0ff" style="font-weight:bold;">
                                  <th align="left" style="border-color:#ddd;">Domain</th>
                                  <th align="left" style="border-color:#ddd;">Scanner</th>
                                  <th align="left" style="border-color:#ddd;">Details</th>
                                </tr>
                              </thead>
                              <tbody>
                                {{#each this}}
                                  <tr>
                                    <td style="border-color:#ddd;">{{Domain}}</td>
                                    <td style="border-color:#ddd;">{{ScannerName}}</td>
                                    <td style="border-color:#ddd;">
                                      {{#if ScanResultUrl}}
                                        <a href="{{ScanResultUrl}}" target="_blank" style="color:#004080; text-decoration:underline;">View Report</a>
                                      {{else}}
                                        N/A
                                      {{/if}}
                                    </td>
                                  </tr>
                                {{/each}}
                              </tbody>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td align="center" style="padding:16px 0 16px 0; color:#777; font-size:12px; font-family:Arial,sans-serif; border-top:1px solid #ddd;">
                            Sent by <a href="https://github.com/Hutch79-Dev/DNS-BLM" target="_blank" style="color:#004080; text-decoration:none;">DNS-BLM</a>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </center>
            </body>
            </html>
            """;
        try
        {
            var usableTemplate = string.IsNullOrWhiteSpace(template) ? defaultTemplate : template;
            var compiledTemplate = _compiledTemplates.GetOrAdd(
                (usableTemplate), _ => Handlebars.Compile(usableTemplate));

            var result = compiledTemplate(model);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to render template", ex);
        }
    }

    /// <summary>
    /// Deletes all precompiled Templates.
    /// Only needs to be used if a Template is changed
    /// </summary>
    public void ClearCompiledTemplates()
    {
        _compiledTemplates.Clear();
    }
}