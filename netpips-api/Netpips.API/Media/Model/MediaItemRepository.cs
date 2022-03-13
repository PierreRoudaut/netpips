using Humanizer;
using Humanizer.Bytes;
using Microsoft.Extensions.Options;
using Netpips.API.Core.Extensions;
using Netpips.API.Core.Settings;
using Netpips.API.Media.Service;

namespace Netpips.API.Media.Model;

public class MediaItemRepository : IMediaItemRepository
{
    private readonly ILogger<MediaLibraryService> _logger;

    private readonly NetpipsSettings _settings;

    private readonly DirectoryInfo _mediaLibraryDirInfo;

    public MediaItemRepository(ILogger<MediaLibraryService> logger, IOptions<NetpipsSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _mediaLibraryDirInfo = new DirectoryInfo(_settings.MediaLibraryPath);
    }

    public IEnumerable<PlainMediaItem> FindAll()
    {
        var entries = _mediaLibraryDirInfo.EnumerateFileSystemInfos("*", SearchOption.AllDirectories)
            .Select(fsInfo => new PlainMediaItem(fsInfo, _settings.MediaLibraryPath)).OrderBy(x => x.Path);

        return entries;
    }

    public PlainMediaItem Find(string path)
    {
        var realPath = Path.GetFullPath(Path.Combine(_settings.MediaLibraryPath, path));

        var fsInfo = _mediaLibraryDirInfo
            .EnumerateFileSystemInfos("*", SearchOption.AllDirectories)
            .FirstOrDefault(x => x.FullName == realPath);

        if (fsInfo == null)
        {
            return null;
        }

        return new PlainMediaItem(fsInfo, _settings.MediaLibraryPath);
    }

    public IEnumerable<MediaFolderSummary> GetMediaLibraryRootFolderDistribution()
    {
        var groups = FindAll()
            .Where(x => !x.FileSystemInfo.IsDirectory())
            .GroupBy(x => x.Path.Split('/').First())
            .Select(
                g => new MediaFolderSummary { Name = g.Key, Size = g.Sum(e => e.Size.GetValueOrDefault()) })
            .ToList();
        groups.ForEach(x => x.HumanizedSize = new ByteSize(x.Size).Humanize("#.##"));

        return groups;
    }

}

public class MediaFolderSummary
{
    public string Name { get; set; }
    public long Size { get; set; }
    public string HumanizedSize { get; set; }
}