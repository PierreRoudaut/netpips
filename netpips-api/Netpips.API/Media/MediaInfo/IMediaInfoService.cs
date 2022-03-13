namespace Netpips.API.Media.MediaInfo;

public interface IMediaInfoService
{
    bool TryGetDuration(string path, out TimeSpan duration);
}