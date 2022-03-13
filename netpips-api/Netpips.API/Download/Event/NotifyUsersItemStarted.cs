using Coravel.Events.Interfaces;

namespace Netpips.API.Download.Event;

public class NotifyUsersItemStarted : IListener<ItemStarted>
{
    private readonly ILogger<NotifyUsersItemStarted> _logger;
    public NotifyUsersItemStarted(ILogger<NotifyUsersItemStarted> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(ItemStarted broadcasted)
    {
        _logger.LogInformation("[HandleAsync] handling DownloadItemStarted event for: " + broadcasted.Item.Name);
        //todo: send push notification with SignalR
        return Task.CompletedTask;
    }
}