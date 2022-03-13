namespace Netpips.API.Download.DownloadMethod.PeerToPeer;

public interface IAria2CService
{
    int DownloadTorrentFile(string magnetLink, string downloadFolder, TimeSpan? timeout = null);
}