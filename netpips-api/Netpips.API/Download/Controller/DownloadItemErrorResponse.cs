namespace Netpips.API.Download.Controller;

public enum DownloadItemActionError
{
    DuplicateDownload,
    UrlNotHandled,
    DownloadabilityFailure,
    StartDownloadFailure,
    ItemNotFound,
    OperationNotPermitted,
}