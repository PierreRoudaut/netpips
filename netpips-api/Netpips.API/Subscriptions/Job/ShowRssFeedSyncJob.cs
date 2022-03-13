using System.Xml.Linq;
using Coravel.Invocable;
using Microsoft.Extensions.Options;
using Netpips.API.Core.Settings;
using Netpips.API.Subscriptions.Model;

namespace Netpips.API.Subscriptions.Job;

public class ShowRssFeedSyncJob : IInvocable
{
    private readonly ILogger<ShowRssFeedSyncJob> _logger;

    private readonly IShowRssItemRepository _repository;

    private readonly ShowRssSettings _settings;

    public ShowRssFeedSyncJob(ILogger<ShowRssFeedSyncJob> logger, IOptions<ShowRssSettings> options, IShowRssItemRepository repository)
    {
        _settings = options.Value;
        _logger = logger;
        _repository = repository;
    }

    public List<ShowRssItem> FetchRssItemsFromFeed()
    {
        //todo, make XElement load xml as Stream and bypass "unexpected token" error
        var feed = XElement.Load(_settings.Feed);
        XNamespace np = feed.Attributes().First(a => a.Value.Contains("showrss")).Value;
        var items = feed
            .Descendants("item")
            .Select(
                item => new ShowRssItem
                {
                    Guid = item.Element("guid")?.Value,
                    Title = item.Element("title")?.Value,
                    Link = item.Element("link")?.Value,
                    ShowRssId = int.Parse(item.Element(np + "show_id")?.Value),
                    TvMazeShowId = int.Parse(item.Element(np + "external_id")?.Value),
                    Hash = item.Element(np + "info_hash")?.Value,
                    PubDate = DateTime.Parse(item.Element("pubDate")?.Value),
                    TvShowName = item.Element(np + "show_name")?.Value
                }).ToList();


        return items;
    }

    public Task Invoke()
    {
        _logger.LogInformation("[FeedSyncJob] Start");
        List<ShowRssItem> items;
        try
        {
            items = FetchRssItemsFromFeed();
            _logger.LogInformation($"[FeedSyncJob] fetched {items.Count} items in feed");
        }
        catch (Exception ex)
        {
            _logger.LogError("[FeedSyncJob] failed to fetch ShowRssItems");
            _logger.LogError(ex.Message);
            return Task.CompletedTask;
        }
        _repository.SyncFeedItems(items);
        _logger.LogInformation("[FeedSyncJob] Completed");
        return Task.CompletedTask;
    }
}