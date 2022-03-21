using Netpips.API.Download.Controller;
using Netpips.API.Download.Model;

namespace Netpips.API.Download.Service;

public interface IDownloadItemService
{
    /// <summary>
    /// Starts a download
    /// </summary>
    /// <param name="fileUrl"></param>
    /// <param name="item"></param>
    /// <param name="error"></param>
    /// <returns></returns>
    bool StartDownload(DownloadItem? item, out DownloadItemActionError error);


    /// <summary>
    ///  Perform cancellation on download item
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    void CancelDownload(DownloadItem item);

        
    void ArchiveDownload(DownloadItem item);

    void ComputeDownloadProgress(DownloadItem item);
    UrlValidationResult ValidateUrl(string? fileUrl);
}