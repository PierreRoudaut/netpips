using Coravel.Events.Interfaces;
using Microsoft.Extensions.Options;
using Netpips.API.Core.Settings;
using Netpips.API.Download.Controller;
using Netpips.API.Download.DownloadMethod;
using Netpips.API.Download.Event;
using Netpips.API.Download.Exception;
using Netpips.API.Download.Model;

namespace Netpips.API.Download.Service;

public class DownloadItemService : IDownloadItemService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DownloadItemService> _logger;
    private readonly NetpipsSettings _settings;
    private readonly IDispatcher _dispatcher;
    private readonly IDownloadItemRepository _repository;

    public DownloadItemService(ILogger<DownloadItemService> logger, IServiceProvider serviceProvider, IOptions<NetpipsSettings> options, IDispatcher dispatcher, IDownloadItemRepository repository)
    {
        _settings = options.Value;
        _serviceProvider = serviceProvider;
        _dispatcher = dispatcher;
        _repository = repository;
        _logger = logger;
    }

    public UrlValidationResult ValidateUrl(string fileUrl)
    {
        var downloadMethod = ResolveDownloadMethod(fileUrl);
        var result = new UrlValidationResult();
        if (downloadMethod == null)
        {
            _logger.LogWarning("URL not supported: " + fileUrl);
            result.IsSupported = false;
            result.Message = "URL not supported";
        }
        else
        {
            result.IsSupported = true;
        }

        return result;
          
    }

    public IDownloadMethod ResolveDownloadMethod(string fileUrl) => _serviceProvider.GetServices<IDownloadMethod>().FirstOrDefault(x => x.CanHandle(fileUrl));

    /// <inheritdoc />
    /// <summary>
    /// Starts a download
    /// </summary>
    /// <param name="item"></param>
    /// <param name="error"></param>
    /// <returns></returns>
    public bool StartDownload(DownloadItem? item, out DownloadItemActionError error)
    {
        error = DownloadItemActionError.DownloadabilityFailure;

        var downloadMethod = ResolveDownloadMethod(item.FileUrl);
        if (downloadMethod == null)
        {
            _logger.LogWarning("URL not handled: " + item.FileUrl);
            error = DownloadItemActionError.UrlNotHandled;
            return false;
        }

        try
        {
            downloadMethod.Start(item);
            item.DownloadedSize = 0;
            item.StartedAt = DateTime.Now;
            item.Archived = false;
            item.State = DownloadState.Downloading;

            _repository.Add(item);
            _ = _dispatcher.Broadcast(new ItemStarted(item));
        }
        catch (FileNotDownloadableException ex)
        {
            _logger.LogWarning(ex.Message);
            error = DownloadItemActionError.DownloadabilityFailure;
            return false;
        }
        catch (StartDownloadException ex)
        {
            _logger.LogWarning(ex.Message);
            error = DownloadItemActionError.StartDownloadFailure;
            return false;
        }
        return true;
    }


    public void CancelDownload(DownloadItem item)
    {
        _logger.LogInformation("Canceling " + item.Name);
        var downloadMethod = _serviceProvider.GetServices<IDownloadMethod>().First(x => x.CanHandle(item.Type));

        downloadMethod.Cancel(item);
        _repository.Cancel(item);
    }

    public void ArchiveDownload(DownloadItem item)
    {
        _logger.LogInformation("Archiving " + item.Name);
        var downloadMethod = _serviceProvider.GetServices<IDownloadMethod>().First(x => x.CanHandle(item.Type));
        downloadMethod.Archive(item);
        _repository.Archive(item);

        var dirInfo = new DirectoryInfo(Path.Combine(_settings.DownloadsPath, item.Token));
        if (dirInfo.Exists)
        {
            dirInfo.Delete(true);
        }

    }

    public void ComputeDownloadProgress(DownloadItem item)
    {
        var downloadMethod = _serviceProvider.GetServices<IDownloadMethod>().First(x => x.CanHandle(item.Type));
        item.DownloadedSize = downloadMethod.GetDownloadedSize(item);
    }
}