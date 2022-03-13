using Netpips.API.Download.Model;

namespace Netpips.API.Subscriptions.Model;

public interface ITvShowSubscriptionRepository
{
    List<string> GetSubscribedUsersEmail(ShowRssItem item);

    bool IsSubscriptionDownload(DownloadItem item, out List<string> subscribedUsersEmail);
}