using Microsoft.EntityFrameworkCore;
using Netpips.API.Core.Model;
using Netpips.API.Download.Model;

namespace Netpips.API.Subscriptions.Model;

public class ShowRssItemRepository : IShowRssItemRepository
{
    private readonly ILogger<ShowRssItemRepository> _logger;
    private readonly AppDbContext _dbContext;

    public ShowRssItemRepository(ILogger<ShowRssItemRepository> logger, AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }


    /// <inheritdoc />
    public void SyncFeedItems(List<ShowRssItem> items)
    {
        var missingItems = items.Except(_dbContext.ShowRssItems.ToList(), new ShowRssItem()).ToList();
        _logger.LogInformation("[FeedSyncJob] " + missingItems.Count + " missing items");
        if (missingItems.Count == 0)
        {
            _logger.LogInformation("[FeedSyncJob] No missing items to add");
            return;
        }
        try
        {
            _dbContext.ShowRssItems.AddRange(missingItems);
            _dbContext.SaveChanges();
            _logger.LogInformation("[FeedSyncJob] Inserted " + missingItems.Count + " items");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("[FeedSyncJob] Failed to Add missing items");
            _logger.LogError(ex.Message);
        }
    }

    public ShowRssItem FindShowRssItem(Guid downloadItemId)
    {
        return _dbContext.ShowRssItems.Include(x => x.DownloadItem).FirstOrDefault(x => x.DownloadItemId == downloadItemId);
    }

    public ShowRssItem FindFirstQueuedItem()
    {
        return _dbContext.ShowRssItems
            .FirstOrDefault(x => x.DownloadItemId == null);
    }

    public void Update(ShowRssItem item)
    {
        _dbContext.Entry(item).State = EntityState.Modified;
        _dbContext.SaveChanges();
    }

    /// <inheritdoc />
    public List<DownloadItem> FindRecentCompletedItems(int timeWindow)
    {
        var threshold = DateTime.Now.AddDays(-timeWindow);
        var items = _dbContext
            .ShowRssItems
            .Include(x => x.DownloadItem)
            .Where(s => s.DownloadItem != null && s.DownloadItem.State == DownloadState.Completed &&
                        s.DownloadItem.CompletedAt >= threshold)
            .Select(s => s.DownloadItem)
            .ToList();
        return items;
    }
}