using Coravel.Invocable;
using Netpips.API.Download.Model;
using Netpips.API.Download.Service;
using Netpips.API.Identity.Model;
using Netpips.API.Subscriptions.Model;

namespace Netpips.API.Subscriptions.Job;

public class ShowRssFeedConsumerJob : IInvocable
{
    private readonly ILogger<ShowRssFeedConsumerJob> _logger;
    private readonly IShowRssItemRepository _showRssItemRepository;
    private readonly IDownloadItemService _downloadItemService;
    private readonly IUserRepository _userRepository;
    public ShowRssFeedConsumerJob(ILogger<ShowRssFeedConsumerJob> logger, IShowRssItemRepository showRssItemRepository, IDownloadItemService downloadItemService, IUserRepository userRepository)
    {
        _logger = logger;
        _showRssItemRepository = showRssItemRepository;
        _downloadItemService = downloadItemService;
        _userRepository = userRepository;
    }

    public Task Invoke()
    {
        //todo: check if no download pendings
        _logger.LogInformation("[ConsumeFeedJob] Start");
        var showRssItem = _showRssItemRepository.FindFirstQueuedItem();
        if (showRssItem == null)
        {
            _logger.LogInformation("[ConsumeFeedJob] 0 item to consume");
            return  Task.CompletedTask;
        }
        _logger.LogInformation($"[ConsumeFeedJob] 1 item: {showRssItem.Title}");
        var daemonUser = _userRepository.GetDaemonUser();
        var downloadItem = new DownloadItem
        {
            OwnerId = daemonUser.Id,
            FileUrl = showRssItem.Link
        };

        if (!_downloadItemService.StartDownload(downloadItem, out var error))
        {
            _logger.LogError("[ConsumeFeedJob] Failed: " + error);
            return Task.CompletedTask;
        }
        _logger.LogInformation("[ConsumeFeedJob] Succeeded");
        showRssItem.DownloadItem = downloadItem;
        _showRssItemRepository.Update(showRssItem);
        _logger.LogInformation("[ConsumeFeedJob] Updated database");
        return Task.CompletedTask;
    }
}