using Microsoft.EntityFrameworkCore;
using Netpips.API.Core.Model;

namespace Netpips.API.Download.Model;

public class DownloadItemRepository : IDownloadItemRepository
{

    private readonly ILogger<DownloadItemRepository> _logger;
    private readonly AppDbContext _dbContext;

    public DownloadItemRepository(ILogger<DownloadItemRepository> logger, AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    /// <summary>
    /// Find all not cleaned up downloadItems
    /// </summary>
    /// <returns></returns>
    public IEnumerable<DownloadItem> FindAllUnarchived()
    {
        return _dbContext.DownloadItems.Include(x => x.Owner).Where(x => !x.Archived).OrderBy(x => x.StartedAt);
    }

    public void Start(DownloadItem item)
    {
        item.Archived = false;
        item.StartedAt = DateTime.Now;
        item.State = DownloadState.Downloading;
        _dbContext.SaveChanges();
        _dbContext.Entry(item).Reference(c => c.Owner).Load();
    }

    public void Archive(DownloadItem item)
    {
        item.Archived = true;
        _dbContext.Entry(item).State = EntityState.Modified;
        _dbContext.SaveChanges();
    }

    public void Cancel(DownloadItem item)
    {
        item.CanceledAt = DateTime.Now;
        item.State = DownloadState.Canceled;
        _dbContext.Entry(item).State = EntityState.Modified;
        _dbContext.SaveChanges();
    }

    /// <summary>
    /// Verifies if a given url is currently downloading
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public bool IsUrlDownloading(string url)
    {
        return _dbContext.DownloadItems.Include(x => x.Owner)
            .Where(x => !x.Archived)
            .Any(x => x.FileUrl == url);
    }


    public List<DownloadItem> GetPassedItemsToArchive(int thresholdDays)
    {
        var thresholdDate = DateTime.Now.AddDays(-1 * thresholdDays);

        var toArchive = _dbContext.DownloadItems.Where(
            d => !d.Archived && ((d.State == DownloadState.Canceled && d.CanceledAt < thresholdDate)
                                 || d.State == DownloadState.Completed && d.CompletedAt < thresholdDate)).ToList();

        return toArchive;
    }

    /// <inheritdoc />
    /// <summary>
    /// Fetch a downloadItem and calculate it's progression
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public DownloadItem Find(string token)
    {
        return _dbContext.DownloadItems
            .Include(x => x.Owner)
            .FirstOrDefault(x => x.Token == token);
    }

    public DownloadItem Find(Guid id)
    {
        return _dbContext.DownloadItems
            .Include(x => x.Owner)
            .FirstOrDefault(x => x.Id == id);
    }

    public void Add(DownloadItem? item)
    {
        _dbContext.Add(item);
        _dbContext.SaveChanges();
        _dbContext.Entry(item).Reference(c => c.Owner).Load();
    }

    public void Update(DownloadItem item)
    {
        _dbContext.Entry(item).State = EntityState.Modified;
        _dbContext.SaveChanges();
    }

    public bool HasPendingDownloads()
    {
        var pendingStates = new DownloadState[] { DownloadState.Downloading, DownloadState.Processing };
        return _dbContext.DownloadItems.Any(x => pendingStates.Contains(x.State));
    }
}