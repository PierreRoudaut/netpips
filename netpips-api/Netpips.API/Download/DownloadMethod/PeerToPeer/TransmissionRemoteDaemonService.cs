using Microsoft.Extensions.Options;
using Netpips.API.Core;
using Netpips.API.Core.Extensions;
using Netpips.API.Core.Settings;

namespace Netpips.API.Download.DownloadMethod.PeerToPeer;

public class TransmissionRemoteDaemonService : ITorrentDaemonService
{
    private string TrAuth => $"-n '{_transmission.Username}:{_transmission.Password}'";
    private readonly ILogger<TransmissionRemoteDaemonService> _logger;
    private readonly NetpipsSettings _settings;
    private readonly TransmissionSettings _transmission;

    public TransmissionRemoteDaemonService(ILogger<TransmissionRemoteDaemonService> logger, IOptions<NetpipsSettings> options, IOptions<TransmissionSettings> transmission)
    {
        _logger = logger;
        _settings = options.Value;
        _transmission = transmission.Value;
    }

    public bool AddTorrent(string torrentPath, string downloadDirPath)
    {
        var args = $"{TrAuth} -a " + torrentPath.Quoted() + " -w " + downloadDirPath.Quoted() + " --torrent-done-script " + _settings.TorrentDoneScript.Quoted();
        _logger.LogInformation("transmission-remote " + args);
        var result = OsHelper.ExecuteCommand("transmission-remote", args, out var output, out var error);
        _logger.LogInformation($"output: {output} error: {error}");
        return result == 0;
    }

    public bool StopTorrent(string hash)
    {
        return OsHelper.ExecuteCommand("transmission-remote", $"{TrAuth} -t {hash} -S", out _, out _) == 0;
    }

    public bool RemoveTorrent(string hash)
    {
        return OsHelper.ExecuteCommand("transmission-remote", $"{TrAuth} -t {hash} -r", out _, out _) == 0;
    }

    public long GetDownloadedSize(string hash)
    {
        OsHelper.ExecuteCommand("transmission-remote", $"{TrAuth} -t {hash} -i", out var output, out var _);
        if (string.IsNullOrEmpty(output))
            return 0;
        return new TransmissionItem(output, null).DownloadedSize;

    }
}