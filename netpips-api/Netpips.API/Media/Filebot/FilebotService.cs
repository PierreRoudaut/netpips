using System.Text.RegularExpressions;
using Netpips.API.Core;
using Netpips.API.Core.Extensions;

namespace Netpips.API.Media.Filebot;

public class FilebotService : IFilebotService
{
    private readonly ILogger<IFilebotService> _logger;

    private static readonly Regex FileAlreadyExistsPattern = new(@"because \[(?<dest>.*)\] already exists");

    public FilebotService(ILogger<IFilebotService> logger)
    {
        _logger = logger;
    }

    public bool GetSubtitles(string path, out string srtPath, string lang = "eng", bool nonStrict = false)
    {
        // todo: wrap in requst/result object
        srtPath = "";
        var args = "-get-subtitles " + path.Quoted() + " --lang " + lang.Quoted();
        if (nonStrict)
        {
            args += " -non-strict ";
        }

        var expectedSrtPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)) +
                              $".{lang}.srt";
        _logger.LogInformation("filebot " + args);
        Console.WriteLine("filebot " + args);

        var code = OsHelper.ExecuteCommand("filebot", args, out var output, out var error);
        var msg = $"code: {code}, output: {output}, error: {error}";
        Console.WriteLine(msg);
        _logger.LogInformation(msg);
        if (!File.Exists(expectedSrtPath))
        {
            Console.WriteLine(expectedSrtPath + " does not exists");
            return false;
        }

        /* -get-subtitles option always returns 0 regardless of failure */
        _logger.LogInformation("Renaming to 2 letter iso code");
        var twoLetterSrtPath = FilesystemHelper.ConvertToTwoLetterIsoLanguageNameSubtitle(expectedSrtPath);
        if (twoLetterSrtPath != null)
        {
            FilesystemHelper.MoveOrReplace(expectedSrtPath, twoLetterSrtPath);
            srtPath = twoLetterSrtPath;
        }

        return true;
    }

    /// <summary>
    /// Get the media location for the file
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public RenameResult Rename(RenameRequest request)
    {
        var result = new RenameResult();
        var destFormat = request.BaseDestPath + Path.DirectorySeparatorChar + "{plex}";
        var args = "-rename " + request.Path.Quoted() + " --format " + destFormat.Quoted() + " -non-strict --action " + request.Action.Quoted();
        if (request.Db != null)
        {
            args += " --db " + request.Db.Quoted();
        }

        _logger.LogInformation("filebot " + args);
        result.RawExecutedCommand = $"filebot {args}";
        result.ExitCode = OsHelper.ExecuteCommand("filebot", args, out var stdout, out var stderr);
        result.StandardOutput = stdout;
        result.StandardError = stderr;
            
        _logger.LogInformation($"code: {result.ExitCode}, output: {result.StandardOutput}, error: {result.StandardError}");

        if (result.ExitCode != 0)
        {
            var match = FileAlreadyExistsPattern.Match(result.StandardOutput);
            if (match.Success && match.Groups["dest"].Success)
            {
                result.DestPath = match.Groups["dest"].Value;
                _logger.LogInformation($"Filebot.TryRename [SUCCESS] [FileAlreadyExists] [{result.DestPath}]");
                result.Reason = "File already exists at dest location";
                result.Succeeded = true;
                return result;
            }
        }

        var p = new Regex(@"\[" + request.Action.ToUpper() + @"\].*\[.*\] to \[(?<dest>.*)\]").Match(result.StandardOutput);
        if (p.Success && p.Groups["dest"].Success)
        {
            result.DestPath = p.Groups["dest"].Value;
            result.Succeeded = true;
            result.Reason = "Found";
            _logger.LogWarning($"Filebot.TryRename [SUCCESS] [{result.DestPath}]");
            return result;
        }

        result.Succeeded = false;
        result.Reason = "Failed to capture destPath in stdout";
        _logger.LogWarning("Filebot.TryRename [FAILED] to capture destPath in output: ", result.StandardOutput);
        return result;
    }
}