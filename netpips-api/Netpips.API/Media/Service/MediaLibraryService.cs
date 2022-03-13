using Microsoft.Extensions.Options;
using Netpips.API.Core.Extensions;
using Netpips.API.Core.Settings;
using Netpips.API.Media.Filebot;
using Netpips.API.Media.Model;

namespace Netpips.API.Media.Service;

public class MediaLibraryService : IMediaLibraryService
{
    private readonly ILogger<MediaLibraryService> _logger;
    private readonly NetpipsSettings _settings;
    private readonly IMediaLibraryMover _mover;
    private readonly IFilebotService _filebot;

    public MediaLibraryService(ILogger<MediaLibraryService> logger, IOptions<NetpipsSettings> appSettings, IMediaLibraryMover mover, IFilebotService filebot)
    {
        _logger = logger;
        _mover = mover;
        _filebot = filebot;
        _settings = appSettings.Value;
    }


    public IEnumerable<PlainMediaItem> AutoRename(PlainMediaItem item)
    {
        if (item.FileSystemInfo.IsDirectory())
        {
            _logger.LogWarning("cannot autoRename: " + item.Path + " is a directory");
            return null;
        }

        return _mover
            .MoveVideoFile(item.FileSystemInfo.FullName)
            .Select(fsInfo => new PlainMediaItem(fsInfo, _settings.MediaLibraryPath));
    }

    public PlainMediaItem GetSubtitles(PlainMediaItem item, string lang)
    {
        if (item.FileSystemInfo.IsDirectory())
        {
            _logger.LogWarning("cannot getSubtitles: " + item.Path + " is a directory");
            return null;
        }

        if (!_filebot.GetSubtitles(item.FileSystemInfo.FullName, out var srtPath, lang))
        {
            _logger.LogWarning("getSubtitles: " + item.Path + " subtitles not found");
            return null;
        }

        return new PlainMediaItem(new FileInfo(srtPath), _settings.MediaLibraryPath);
    }
}