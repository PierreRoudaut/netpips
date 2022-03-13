using Coravel.Events.Interfaces;
using Netpips.API.Core;
using Netpips.API.Download.Event;
using Netpips.API.Download.Model;

namespace Netpips.API.Download.DownloadMethod.DirectDownload;

public class DirectDownloadTask : IDirectDownloadTask
{

    private static readonly HttpClientHandler Handler = new()
    {
        UseCookies = false,
        AllowAutoRedirect = true
    };
    private static readonly HttpClient Client = new(Handler);

    private readonly DownloadItem? _item;
    private readonly IDispatcher _dispatcher;

    private readonly CancellationTokenSource _tokenSource = new();
    private readonly string _downloadDestPath;
    private readonly List<string> _cookies;

    public DirectDownloadTask(DownloadItem? item, IDispatcher dispatcher, string downloadDestPath, List<string> cookies)
    {
        _item = item;
        _dispatcher = dispatcher;
        _downloadDestPath = downloadDestPath;
        _cookies = cookies;
        Client.DefaultRequestHeaders.Add("User-Agent", OsHelper.UserAgent);
    }

    public async Task StartAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _item.FileUrl);
        request.Headers.Add("Cookie", _cookies);

        var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var resStream = await response.Content.ReadAsStreamAsync();
        var fStream = File.Create(_downloadDestPath);

        var downloadCompleted = false;

        await resStream
            .CopyToAsync(fStream, 81920, _tokenSource.Token)
            .ContinueWith(task => downloadCompleted = task.IsCompletedSuccessfully);

        resStream.Dispose();
        fStream.Dispose();
        DirectDownloadMethod.DirectDownloadTasks.TryRemove(_item.Token, out _);
        if (downloadCompleted)
        {
            _ = _dispatcher.Broadcast(new ItemDownloaded(_item.Id));
        }
    }

    public void Cancel()
    {
        _tokenSource.Cancel();
    }
}