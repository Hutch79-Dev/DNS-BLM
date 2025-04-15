namespace DNS_BLM.Infrastructure.Services.ServiceInterfaces;

public interface INotificationService
{
    public Task Notify(string subject, string message);
}