namespace Netpips.API.Search.Service;

public interface ITorrentSearchScrapper
{
    Task<TorrentSearchResult> SearchAsync(string query);
}