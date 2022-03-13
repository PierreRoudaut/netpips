using System.Collections.Concurrent;
using Coravel.Events.Interfaces;
using Microsoft.Extensions.Options;
using Netpips.API.Core;
using Netpips.API.Core.Extensions;
using Netpips.API.Core.Settings;
using Netpips.API.Download.Exception;
using Netpips.API.Download.Model;

namespace Netpips.API.Download.DownloadMethod.DirectDownload;

public class DirectDownloadMethod : IDownloadMethod
{
    public static readonly ConcurrentDictionary<string, IDirectDownloadTask> DirectDownloadTasks = new();

    private readonly ILogger<DirectDownloadMethod> _logger;
    private readonly NetpipsSettings _settings;
    private readonly IDispatcher _dispatcher;
    private readonly DirectDownloadSettings _directDownloadSettings;

    public DirectDownloadMethod(ILogger<DirectDownloadMethod> logger,
        IOptions<NetpipsSettings> settings,
        IOptions<DirectDownloadSettings> directDownloadOptions,
        IDispatcher dispatcher)
    {
        _logger = logger;
        _settings = settings.Value;
        _dispatcher = dispatcher;
        _directDownloadSettings = directDownloadOptions.Value;
    }

    /// <summary>
    /// Perform an authentication and returns a list of cookies
    /// </summary>
    /// <param name="fileHosterInfo"></param>
    /// <returns>A dictionary of cookies</returns>
    public List<string> Authenticate(FileHosterInfo fileHosterInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, fileHosterInfo.LoginUrl)
        {
            Content = new FormUrlEncodedContent(fileHosterInfo.CredentialsData)
        };
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("User-Agent", OsHelper.UserAgent);
            var response = client.SendAsync(request).Result;
            if (!response.IsSuccessStatusCode)
            {
                var msg = "An error occured while authenticating on provider: " + fileHosterInfo.Name;
                _logger.LogCritical(msg);
                _logger.LogError("Status code: " + response.StatusCode);
                throw new StartDownloadException(msg);
            }

            var hasCookies = response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders);
            var cookies = new List<string>((hasCookies && cookieHeaders != null) ? cookieHeaders : Enumerable.Empty<string>());
            if (!cookies.Any())
            {
                var msg = "No cookies retrieved while authenticating on: " + fileHosterInfo.Name;
                _logger.LogCritical(msg);
                throw new StartDownloadException(msg);
            }

            return cookies;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="cookies"></param>
    /// <returns></returns>
    public bool CheckDownloadability(DownloadItem? item, List<string> cookies)
    {

        var request = new HttpRequestMessage(HttpMethod.Head, item.FileUrl);
        request.Headers.Add("Cookie", cookies);
        using (var handler = new HttpClientHandler() { UseCookies = false, AllowAutoRedirect = true })
        using (var client = new HttpClient(handler))
        {
            client.DefaultRequestHeaders.Add("User-Agent", OsHelper.UserAgent);
            var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("request failed for " + item.FileUrl + " code:" + response.StatusCode);
                return false;
            }
            item.TotalSize = response.Content.Headers.ContentLength ?? 0;
            item.Name = (response.Content.Headers.ContentDisposition?.FileName ?? response.RequestMessage.RequestUri?.AbsolutePath.Split('/').Last()).ToSafeFilename();
        }
        return !string.IsNullOrEmpty(item.Name) && item.TotalSize > 0;
    }

    /// <summary>
    /// Starts the download
    /// </summary>
    /// <param name="item"></param>
    /// <param name="cookies"></param>
    private void StartDirectDownloadTask(DownloadItem? item, List<string> cookies)
    {
        var itemFolder = Path.Combine(_settings.DownloadsPath, item.Token);
        Directory.CreateDirectory(itemFolder);
        var downloadDestPath = Path.Combine(itemFolder, item.Name);
        DirectDownloadTasks[item.Token] = new DirectDownloadTask(item, _dispatcher, downloadDestPath, cookies);
        DirectDownloadTasks[item.Token].StartAsync();
    }

    public void Start(DownloadItem? item)
    {
        var fileHosterInfo = _directDownloadSettings.Filehosters.First(x => x.CanHandle(item.FileUrl));

        var cookies = new List<string>();

        //auth on filehoster
        if (fileHosterInfo.LoginUrl != null)
        {
            cookies = Authenticate(fileHosterInfo);
        }

        //check downloadability
        if (!CheckDownloadability(item, cookies))
        {
            var msg = "No file found at url " + item.FileUrl;
            _logger.LogWarning(msg);
            throw new FileNotDownloadableException(msg);
        }

        //start download
        item.Type = DownloadType.Ddl;
        item.Token = "_" + DownloadType.Ddl.ToString().ToLower() + Guid.NewGuid().ToString("N");
        try
        {
            StartDirectDownloadTask(item, cookies);
        }
        catch (System.Exception ex)
        {
            var msg = "Unexpected error occured for: " + item.FileUrl;
            _logger.LogCritical(msg + " exception:" + ex.Message);
            throw new StartDownloadException(msg);
        }
    }

    public bool Cancel(DownloadItem item)
    {
        if (DirectDownloadTasks.TryRemove(item.Token, out var directDownloadTask))
        {
            directDownloadTask.Cancel();
            return true;
        }
        return false;
    }

    public bool Archive(DownloadItem item) => true;
    public bool CanHandle(string fileUrl)
    {
        return _directDownloadSettings.Filehosters.Any(x => x.CanHandle(fileUrl));
    }

    public bool CanHandle(DownloadType type) => type == DownloadType.Ddl;

    public long GetDownloadedSize(DownloadItem item)
    {
        var itemPath = Path.Combine(_settings.DownloadsPath, item.Token);
        if (!Directory.Exists(itemPath))
        {
            return 0;
        }
        return Directory
            .GetFiles(itemPath, "*", SearchOption.AllDirectories)
            .Sum(t => (new FileInfo(t).Length));
    }

}