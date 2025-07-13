using System.ComponentModel.DataAnnotations;

namespace DNS_BLM.Domain.Configuration;

public class MailConfiguration
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string Host { get; init; } = string.Empty;

    [Required]
    public int Port { get; init; } = 587;

    [Required]
    [EmailAddress]
    public string From { get; init; } = string.Empty;

    [Required]
    public bool EnableSsl { get; init; } = true;
    public string MailTemplate { get; init; } = string.Empty;

}