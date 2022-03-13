using Coravel.Invocable;
using Netpips.API.Download.Model;
using Netpips.API.Download.Service;

namespace Netpips.API.Download.Job;

public class ArchiveDownloadItemsJob : IInvocable
{
    private readonly ILogger<ArchiveDownloadItemsJob> _logger;

    private readonly IDownloadItemRepository _repository;

    private readonly IDownloadItemService _service;

    public const int ArchiveThresholdDays = 3;

    public ArchiveDownloadItemsJob(ILogger<ArchiveDownloadItemsJob> logger, IDownloadItemRepository repository, IDownloadItemService service)
    {
        _logger = logger;
        _repository = repository;
        _service = service;
    }

    public Task Invoke()
    {
        _logger.LogInformation("[ArchiveDownloadItemsJob] Start");
        var toArchive = _repository.GetPassedItemsToArchive(ArchiveThresholdDays);
        _logger.LogInformation($"[ArchiveDownloadItemsJob] {toArchive.Count} items to archive");
        toArchive.ForEach(item =>
        {
            _service.ArchiveDownload(item);
            _logger.LogInformation($"[ArchiveDownloadItemsJob] Archived {item.Token}");
        });

        _logger.LogInformation($"[ArchiveDownloadItemsJob] Done");
        return Task.CompletedTask;
    }
}