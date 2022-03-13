using Coravel.Events.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netpips.API.Core.Service;
using Netpips.API.Download.Authorization;
using Netpips.API.Download.DownloadMethod.PeerToPeer;
using Netpips.API.Download.Event;
using Netpips.API.Download.Model;

namespace Netpips.API.Download.Controller;

[Produces("application/json")]
[Route("api/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
public class TorrentDoneController : Microsoft.AspNetCore.Mvc.Controller
{
    private readonly ILogger<TorrentDoneController> _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly ITorrentDaemonService _torrentDaemonService;
    private readonly IDispatcher _dispatcher;
    private readonly IDownloadItemRepository _repository;

    public TorrentDoneController(ILogger<TorrentDoneController> logger, 
        IDownloadItemRepository repository,
        IAuthorizationService authorizationService,
        ITorrentDaemonService torrentDaemonService,
        IDispatcher dispatcher)
    {
        _logger = logger;
        _authorizationService = authorizationService;
        _torrentDaemonService = torrentDaemonService;
        _repository = repository;
        _dispatcher = dispatcher;
    }

    [HttpGet("{hash}", Name = "TorrentDone")]
    [ProducesResponseType(typeof(DownloadItemActionError), 404)]
    [ProducesResponseType(typeof(DownloadItemActionError), 400)]
    [ProducesResponseType(200)]
    public ObjectResult TorrentDone([FromServices] IControllerHelperService helper, string hash)
    {
        _logger.LogInformation("TorrentDone: " + hash);
        if (!helper.IsLocalCall(HttpContext))
        {
            _logger.LogWarning("Request not sent from the server");
            return StatusCode(403, null);
        }

        var item = _repository.FindAllUnarchived().FirstOrDefault(x => x.Hash == hash);
        if (item == null)
        {
            _logger.LogInformation(hash + ": note found");
            return StatusCode(404, DownloadItemActionError.ItemNotFound);
        }

        var authorizationResult = _authorizationService.AuthorizeAsync(User, item, DownloadItemPolicies.TorrentDonePolicy).Result;
        if (!authorizationResult.Succeeded)
        {
            var requirement = authorizationResult.Failure.FailedRequirements.First() as DownloadItemBaseRequirement;
            return StatusCode(requirement.HttpCode, requirement.Error);
        }

        _torrentDaemonService.StopTorrent(hash);
        _ = _dispatcher.Broadcast(new ItemDownloaded(item.Id));

        return StatusCode(200, new { Processed = true });
    }
}