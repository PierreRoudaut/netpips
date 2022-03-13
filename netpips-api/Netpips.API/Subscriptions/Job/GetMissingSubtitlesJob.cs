using Coravel.Invocable;
using Microsoft.Extensions.Options;
using Netpips.API.Core.Settings;
using Netpips.API.Media.Filebot;
using Netpips.API.Subscriptions.Model;
using Serilog;

namespace Netpips.API.Subscriptions.Job;

public class GetMissingSubtitlesJob : IInvocable
{
    private readonly IShowRssItemRepository _repository;
    private readonly IFilebotService _filebot;
    private readonly NetpipsSettings _settings;

    public GetMissingSubtitlesJob(IShowRssItemRepository repository, IFilebotService filebot, IOptions<NetpipsSettings> options)
    {
        _repository = repository;
        _filebot = filebot;
        _settings = options.Value;
    }

    public Task Invoke()
    {
        var items = _repository.FindRecentCompletedItems(4);
        Log.Information($"[GetSubtitlesJob]: Found {items.Count} items needing subtitles");
        foreach (var item in items)
        {
            var videoFileRelPath = item.MovedFiles.OrderByDescending(x => x.Size).FirstOrDefault()?.Path;
            Log.Information($"[GetSubtitlesJob] handling [{videoFileRelPath}]");
            var videoFullPath = Path.Combine(_settings.MediaLibraryPath, videoFileRelPath);
            var fileInfo = new FileInfo(videoFullPath);
            if (!fileInfo.Exists || fileInfo.Directory == null)
            {
                Log.Information($"[GetSubtitlesJob] cannot download missing subtitles for [{videoFileRelPath}] File has been moved");
                continue;
            }

            var subs = fileInfo.Directory.EnumerateFiles("*.srt").ToList();
            Log.Information($"[GetSubtitlesJob] {subs.Count} found " + string.Join(", ", subs.Select(c => $"[{c}]")));
            if (subs.Count == 0)
            {
                var resEngSub = _filebot.GetSubtitles(videoFullPath, out _, "eng");
                Log.Information($"[GetSubtitlesJob] [eng] subtites: { (resEngSub ? "OK" : "KO") }");
                var resFraSub = _filebot.GetSubtitles(videoFullPath, out _, "fra");
                Log.Information($"[GetSubtitlesJob] [fra] subtites: { (resFraSub ? "OK" : "KO") }");
            }
        }
        return Task.CompletedTask;
    }
}