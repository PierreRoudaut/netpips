using Coravel.Events.Interfaces;
using Netpips.API.Download.Model;
using Netpips.API.Media.Model;
using Netpips.API.Media.Service;

namespace Netpips.API.Download.Event;

public class ProcessDownloadItem : IListener<ItemDownloaded>
{
    private readonly ILogger<ProcessDownloadItem> _logger;
    private readonly IDownloadItemRepository _repository;
    private readonly IMediaLibraryMover _mediaLibraryMover;


    public ProcessDownloadItem(ILogger<ProcessDownloadItem> logger, IDownloadItemRepository repository, IMediaLibraryMover mediaLibraryMover)
    {
        _logger = logger;
        _repository = repository;
        _mediaLibraryMover = mediaLibraryMover;
    }
    public Task HandleAsync(ItemDownloaded broadcasted)
    {
        _logger.LogInformation("[HandleAsync] handling DownloadItemDownloaded event for: " + broadcasted.DownloadItemId);
        var item = _repository.Find(broadcasted.DownloadItemId);

        // mark item as processing
        item = _repository.Find(item.Id);
        item.DownloadedAt = DateTime.Now;
        item.State = DownloadState.Processing;
        _repository.Update(item);

        _logger.LogInformation($"[Processing] [{item.Name}]");
        var movedFiles = new List<MediaItem>();
        try
        {
            movedFiles = _mediaLibraryMover.ProcessDownloadItem(item);
        }
        catch (System.Exception e)
        {
            _logger.LogError("ProcessDownloadItem Failed to process download items");
            _logger.LogError(e.Message);
        }

        // mark item as completed
        item = _repository.Find(item.Id);
        item.MovedFiles = movedFiles;
        item.CompletedAt = DateTime.Now;
        item.State = DownloadState.Completed;
        _repository.Update(item);
        return Task.CompletedTask;
    }
}