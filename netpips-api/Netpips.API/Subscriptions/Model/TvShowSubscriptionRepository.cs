using Microsoft.EntityFrameworkCore;
using Netpips.API.Core.Model;
using Netpips.API.Download.Model;

namespace Netpips.API.Subscriptions.Model;

public class TvShowSubscriptionRepository : ITvShowSubscriptionRepository
{
    private readonly ILogger<TvShowSubscriptionRepository> _logger;
    private readonly AppDbContext _dbContext;

    public TvShowSubscriptionRepository(ILogger<TvShowSubscriptionRepository> logger, AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public List<string> GetSubscribedUsersEmail(ShowRssItem showRssItem)
    {
        return _dbContext.TvShowSubscriptions
            .Include(x => x.User)
            .Where(x => x.ShowRssId == showRssItem.ShowRssId && x.User.TvShowSubscriptionEmailNotificationEnabled)
            .Select(x => x.User.Email).ToList();
    }

    public bool IsSubscriptionDownload(DownloadItem item, out List<string> subscribedUsersEmail)
    {
        subscribedUsersEmail = null;
        var showRssItem = _dbContext.ShowRssItems.FirstOrDefault(x => x.DownloadItemId == item.Id);
        if (showRssItem == null)
        {
            return false;
        }
        subscribedUsersEmail = _dbContext.TvShowSubscriptions
            .Include(x => x.User)
            .Where(x => x.ShowRssId == showRssItem.ShowRssId && x.User.TvShowSubscriptionEmailNotificationEnabled)
            .Select(x => x.User.Email).ToList();
        return true;
    }
}