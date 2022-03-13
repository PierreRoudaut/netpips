using System.Net.Mail;
using Coravel.Events.Interfaces;
using Humanizer;
using Humanizer.Bytes;
using Microsoft.Extensions.Options;
using Netpips.API.Core;
using Netpips.API.Core.Service;
using Netpips.API.Core.Settings;
using Netpips.API.Download.Model;
using Netpips.API.Subscriptions.Model;

namespace Netpips.API.Download.Event;

public class SendItemCompletedEmail : IListener<ItemDownloaded>
{
    private readonly ITvShowSubscriptionRepository _tvShowSubscriptionRepository;
    private readonly ISmtpService _smtpService;
    private readonly ILogger<SendItemCompletedEmail> _logger;
    private readonly IDownloadItemRepository _downloadItemRepository;
    private readonly NetpipsSettings _settings;


    public SendItemCompletedEmail(ITvShowSubscriptionRepository tvShowSubscriptionRepository, ISmtpService smtpService, ILogger<SendItemCompletedEmail> logger, IDownloadItemRepository downloadItemRepository, IOptions<NetpipsSettings> options)
    {
        _tvShowSubscriptionRepository = tvShowSubscriptionRepository;
        _smtpService = smtpService;
        _logger = logger;
        _downloadItemRepository = downloadItemRepository;
        _settings = options.Value;
    }

    public Task HandleAsync(ItemDownloaded broadcasted)
    {
        _logger.LogInformation("SendItemCompletedEmail START");
        var item = _downloadItemRepository.Find(broadcasted.DownloadItemId);
        if (_tvShowSubscriptionRepository.IsSubscriptionDownload(item, out var subscribedUsersEmails))
        {
            _logger.LogInformation("Item was downloaded by a subscription");
            _logger.LogInformation($"Sending email to [{string.Join(",", subscribedUsersEmails)}]");
            NotifySubscribedUsers(item, subscribedUsersEmails);
        }
        else
        {
            _logger.LogInformation($"Item was manually downloaded by {item.Owner.Email}");
            NotifyOwner(item);
        }
        _logger.LogInformation("SendItemCompletedEmail END");
        return Task.CompletedTask;
    }

    public void NotifySubscribedUsers(DownloadItem item, List<string> subscribedUsersEmails)
    {
        if (subscribedUsersEmails.Count == 0)
        {
            return;
        }
        var email = new MailMessage
        {
            IsBodyHtml = true,
            Subject = $"[New episode available] { item.MainFilename }",
            Body = BuildNewEpisodeAvailableMailBody(item)
        };
        subscribedUsersEmails.ForEach(e => email.Bcc.Add(new MailAddress(e)));
        _smtpService.Send(email);
    }

    private string BuildNewEpisodeAvailableMailBody(DownloadItem item)
    {
        // todo: embed TvMazeApi info for episode
        // todo: enhance email template with Coravel.Mailer
        // todo: add button to open file on Plex web/app

        var tvShowsUri = new Uri(_settings.Domain, "tv-shows");
        var html = "<div>";
        html += $"<div>{ item.MainFilename } is available on <a href='{_settings.PlexDomain}'>{_settings.PlexDomain.Host}</a></div>";
        if (!item.MovedFiles.Any(f => f.Path.EndsWith(".srt")))
        {
            html += "<div style='color:grey'>Subtitles are not available yet but will be downloaded shortly.</div>";
        }
        html += $"<div>To update your list of shows: <a href='{tvShowsUri}'>{tvShowsUri.Host}</a></div>";
        html += "</div>";
        return html;
    }

    public string BuildDownloadCompletedMailBody(DownloadItem item)
    {
        var downloadedIn = item.DownloadedAt.Subtract(item.StartedAt);
        var avgSpeed = new ByteSize(item.TotalSize).Per(downloadedIn);
        var processedIn = item.CompletedAt.Subtract(item.DownloadedAt);
        var html = OsHelper.GetRessourceContent("download-completed-email.tmpl.html");
        html = html
            .Replace("{downloadedIn}", downloadedIn.Humanize())
            .Replace("{avgSpeed}", avgSpeed.Humanize("#"))
            .Replace("{processedIn}", processedIn.Humanize())
            .Replace("{movedFiles}", string.Join("",
                item.MovedFiles.Where(pmi => pmi.Size.HasValue).OrderBy(x => x.Path).Select(pmi =>
                    "<tr>" +
                    "   <td>" + pmi.Path.Split('/').Last() + "</td>" +
                    "   <td>" + new ByteSize((double)pmi.Size).Humanize("#") + "</td>" +
                    "</tr>")
            ));
        return html;
    }

    public void NotifyOwner(DownloadItem item)
    {
        var toAddress = new MailAddress(item.Owner.Email);
        var email = new MailMessage
        {
            To = { toAddress },
            IsBodyHtml = true,
            Subject = "[Download completed] " + item.Name,
            Body = BuildDownloadCompletedMailBody(item)
        };
        _smtpService.Send(email);
    }
}