using System.Globalization;
using Microsoft.Extensions.Options;
using Netpips.API.Core;
using Netpips.API.Core.Settings;
using Netpips.API.Download.Model;
using Netpips.API.Media.Filebot;
using Netpips.API.Media.MediaInfo;
using Netpips.API.Media.Model;

namespace Netpips.API.Media.Service;

public class MediaLibraryMover : IMediaLibraryMover
{
    private readonly NetpipsSettings _settings;
    private readonly ILogger<MediaLibraryMover> _logger;
    private readonly IFilebotService _filebot;
    private readonly IMediaInfoService _mediaInfo;
    private readonly IArchiveExtractorService _archiveExtractorService;

    private static readonly List<string> MatchingSubtitlesExtensions = CultureInfo
        .GetCultures(CultureTypes.AllCultures)
        .Select(x => $".{x.ThreeLetterISOLanguageName}.srt")
        .Concat(CultureInfo.GetCultures(CultureTypes.AllCultures).Select(x => $".{x.TwoLetterISOLanguageName}.srt"))
        .Concat(new []{ ".srt" })
        .ToHashSet()
        .ToList();


    public MediaLibraryMover(IOptions<NetpipsSettings> options, ILogger<MediaLibraryMover> logger, IFilebotService filebot, IMediaInfoService mediaInfo, IArchiveExtractorService archiveExtractorService)
    {
        _settings = options.Value;
        _logger = logger;
        _filebot = filebot;
        _mediaInfo = mediaInfo;
        _archiveExtractorService = archiveExtractorService;
    }

    public List<FileSystemInfo> MoveVideoFile(string videoSrcPath)
    {
        var filesystemItems = new List<FileSystemInfo>();

        string videoDestPath;
        var renameResult = _filebot.Rename(new RenameRequest{Path = videoSrcPath, BaseDestPath = _settings.MediaLibraryPath});
        if (!renameResult.Succeeded)
        {
            //fallback strategy to move video file based on duration
            _logger.LogInformation("[MoveVideoFile] TryRename failed, executing fallback logic");
            _mediaInfo.TryGetDuration(videoSrcPath, out var duration);
            var minutesDuration = duration.TotalMinutes;
            var fallbackDir = "Others";
            if (minutesDuration > 0 && minutesDuration < 105)
                fallbackDir = "TV Shows";
            else if (minutesDuration >= 105)
                fallbackDir = "Movies";
            _logger.LogInformation($"[MoveVideoFile] TryRename fallback destDir is [{fallbackDir}]");
            videoDestPath = Path.Combine(_settings.MediaLibraryPath, fallbackDir, Path.GetFileName(videoSrcPath));
        } 
        else
        {
            videoDestPath = renameResult.DestPath;
            _logger.LogInformation("[MoveVideoFile] TryRename Succeeded videoDestPath: " + renameResult.DestPath);
        }

        //create dest dir if it does not exists and move item
        var destDirInfo = new DirectoryInfo(Path.GetDirectoryName(videoDestPath));

        //track created directories
        for (var tempDirInfo = destDirInfo; !tempDirInfo.Exists; tempDirInfo = tempDirInfo.Parent)
            filesystemItems.Add(tempDirInfo);

        if (!destDirInfo.Exists)
        {
            destDirInfo.Create();
        }
        filesystemItems.Add(new FileInfo(videoDestPath));
        FilesystemHelper.MoveOrReplace(videoSrcPath, videoDestPath, _logger);
        return filesystemItems;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="srcPath"></param>
    /// <param name="destPath"></param>
    /// <returns></returns>
    public List<FileInfo> MoveMatchingSubtitlesOf(string srcPath, string destPath)
    {

        var subtitles = new List<FileInfo>();

        var srcBasePath = Path.Combine(Path.GetDirectoryName(srcPath), Path.GetFileNameWithoutExtension(srcPath));
        var destBasePath = Path.Combine(Path.GetDirectoryName(destPath), Path.GetFileNameWithoutExtension(destPath));

        //reverse logic to iterate over folder
        MatchingSubtitlesExtensions.ForEach(subExt =>
        {
            var subPath = srcBasePath + subExt;
            if (File.Exists(subPath))
            {
                var srtDestPath = destBasePath + subExt;
                var srtDestPath2Letters = FilesystemHelper.ConvertToTwoLetterIsoLanguageNameSubtitle(srtDestPath);
                if (srtDestPath2Letters != null)
                {
                    srtDestPath = srtDestPath2Letters;
                }
                FilesystemHelper.MoveOrReplace(subPath, srtDestPath);
                subtitles.Add(new FileInfo(srtDestPath));
            }
        });

        return subtitles;
    }

    // nicetohave: interface music item with spotify API
    public FileInfo MoveMusicFile(string path)
    {
        var destMusicPath = Path.Combine(_settings.MediaLibraryPath, "Music", Path.GetFileName(path));
        FilesystemHelper.MoveOrReplace(path, destMusicPath);
        _logger.LogInformation("ProcessMusicItem from: " + path + " to: " + destMusicPath);
        return new FileInfo(destMusicPath);
    }

    public FileInfo MoveUnknownFile(string path)
    {
        var destPath = Path.Combine(_settings.MediaLibraryPath,
            "Others", Path.GetFileName(path));
        FilesystemHelper.MoveOrReplace(path, destPath);
        return new FileInfo(destPath);
    }

    public List<MediaItem>? ProcessDownloadItem(DownloadItem item)
    {
        return new List<MediaItem>(ProcessDir(Path.Combine(_settings.DownloadsPath, item.Token))
            .Select(p => new PlainMediaItem(p, _settings.MediaLibraryPath).ToMediaItem)
            .OrderBy(x => x.Path)
            .ToList());
    }

    public List<FileSystemInfo> ProcessDir(string path)
    {
        var processedFiles = new List<FileSystemInfo>();
        var dirInfo = new DirectoryInfo(path);
        var fsInfos = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).ToList();
        foreach (var fsInfo in fsInfos)
        {
            _logger.LogInformation("Handling " + fsInfo.Name);
            switch (Path.GetExtension(fsInfo.Name).ToLower())
            {
                case ".avi":
                case ".mp4":
                case ".mkv":
                    var createdMediaItems = MoveVideoFile(fsInfo.FullName);
                    processedFiles.AddRange(createdMediaItems);
                    var movedVideoFile = createdMediaItems.Last();
                    processedFiles.AddRange(MoveMatchingSubtitlesOf(fsInfo.FullName, movedVideoFile.FullName));
                    if (_filebot.GetSubtitles(movedVideoFile.FullName, out var engSrtPath, "eng")) processedFiles.Add(new FileInfo(engSrtPath));
                    if (_filebot.GetSubtitles(movedVideoFile.FullName, out var fraSrtPath, "fra")) processedFiles.Add(new FileInfo(fraSrtPath));
                    break;
                case ".flac":
                case ".mp3":
                    processedFiles.Add(MoveMusicFile(fsInfo.FullName));
                    break;
                case ".rar":
                    if (_archiveExtractorService.HandleRarFile(fsInfo.FullName, out var destDir))
                        processedFiles.AddRange(ProcessDir(destDir));
                    else
                        processedFiles.AddRange(Directory.GetFiles(destDir, "*.rar", SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)));
                    break;
                default:
                    processedFiles.Add(MoveUnknownFile(fsInfo.FullName));
                    break;
            }
        }
        return processedFiles;
    }
}