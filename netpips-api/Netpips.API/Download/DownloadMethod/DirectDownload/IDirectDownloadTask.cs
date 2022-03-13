namespace Netpips.API.Download.DownloadMethod.DirectDownload;

public interface IDirectDownloadTask
{
    void Cancel();
    Task StartAsync();
}