using Netpips.API.Media.Model;

namespace Netpips.API.Media.Service;

public interface IMediaLibraryService
{
    IEnumerable<PlainMediaItem> AutoRename(PlainMediaItem item);
    PlainMediaItem GetSubtitles(PlainMediaItem item, string lang);
}