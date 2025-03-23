namespace DNS_BLM.Application.Services;

public interface INotificationService
{
    public Task Notify(string subject, List<string> message);
}