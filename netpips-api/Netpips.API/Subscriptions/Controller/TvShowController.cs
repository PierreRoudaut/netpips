using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Netpips.API.Core.Extensions;
using Netpips.API.Identity.Model;
using Netpips.API.Subscriptions.Model;
using Netpips.API.Subscriptions.Service;
using Newtonsoft.Json;
using Serilog;

namespace Netpips.API.Subscriptions.Controller;

using Controller = Microsoft.AspNetCore.Mvc.Controller;

[Produces("application/json")]
[Route("api/[controller]")]
[Authorize]
public class TvShowController : Controller
{
    private readonly ILogger<TvShowController> _logger;

    private readonly IShowRssGlobalSubscriptionService _showRssGlobalSubscriptionService;

    private readonly IUserRepository _userRepository;
    private readonly IMemoryCache _memoryCache;


    public TvShowController(ILogger<TvShowController> logger, IShowRssGlobalSubscriptionService showRssGlobalSubscriptionService, IUserRepository userRepository, IMemoryCache memoryCache)
    {
        _logger = logger;
        _showRssGlobalSubscriptionService = showRssGlobalSubscriptionService;
        _userRepository = userRepository;
        _memoryCache = memoryCache;
    }

    private User GetUser()
    {
        var userGuid = User.GetId();
        _logger.LogInformation("User id:" + userGuid);
        return _userRepository.FindUser(userGuid);
    }

    [HttpPost("unsubscribe/{showRssId}", Name = "Unsubscribe")]
    [ProducesResponseType(400)]
    [ProducesResponseType(200)]
    public ObjectResult Unsubscribe(int showRssId)
    {
        var user = GetUser();

        //check for user subscription
        var subscription = user.TvShowSubscriptions.FirstOrDefault(s => s.ShowRssId == showRssId);
        if (subscription == null)
        {
            return BadRequest("Show not subscribed");
        }

        // removing global subscription if no other users have subscribed to the show
        if (!_userRepository.IsTvShowSubscribedByOtherUsers(showRssId, user.Id))
        {
            var context = _showRssGlobalSubscriptionService.Authenticate(out _);
            var result = _showRssGlobalSubscriptionService.UnsubscribeToShow(context, showRssId);
            if (!result.Succeeded)
            {
                return BadRequest("Internal error, failed to unsubscribe to show");
            }
        }

        // removing user subscription
        user.TvShowSubscriptions.Remove(subscription);
        _userRepository.UpdateUser(user);
        return Ok("Unsubscribed");
    }

    [HttpPost("subscribe/{showRssId}", Name = "Subscribe")]
    [ProducesResponseType(400)]
    [ProducesResponseType(200)]
    public ObjectResult Subscribe(int showRssId)
    {
        var user = GetUser();
        // check for existing user subscription
        var subscription = user.TvShowSubscriptions.FirstOrDefault(s => s.ShowRssId == showRssId);
        if (subscription != null)
        {
            return BadRequest("Show already subscribed");
        }

        var context = _showRssGlobalSubscriptionService.Authenticate(out var subscriptionsSummary);
        var globalSubscription = subscriptionsSummary.SubscribedShows.FirstOrDefault(s => s.ShowRssId == showRssId);
        // Add to globalsubcription if show not globally subscribed
        if (globalSubscription == null)
        {
            var result = _showRssGlobalSubscriptionService.SubscribeToShow(context, showRssId);
            if (!result.Succeeded)
            {
                return BadRequest("Internal error, failed to subscribe to show");
            }
            globalSubscription = result.Summary.SubscribedShows.First(s => s.ShowRssId == showRssId);
        }

        user.TvShowSubscriptions.Add(new TvShowSubscription
        {
            UserId = user.Id,
            ShowRssId = globalSubscription.ShowRssId,
            ShowTitle = globalSubscription.ShowTitle
        });
        _userRepository.UpdateUser(user);

        return Ok("Subscribed");
    }

    [HttpGet("allShows")]
    [ProducesResponseType(typeof(List<TvShowRss>), 200)]
    public ObjectResult GetAllShows()
    {
        const string tvShowRssShows = "[tv-show-rss-all-shows]";
        if (!_memoryCache.TryGetValue(tvShowRssShows, out List<TvShowRss> allShows))
        {
            _showRssGlobalSubscriptionService.Authenticate(out var subscriptions);
            _logger.LogInformation("Subscribed shows: " + string.Join(", ", subscriptions.SubscribedShows.Select(s => s.ShowTitle)));
            _logger.LogInformation("Available shows: " + subscriptions.AvailableShows.Count);
            _logger.LogInformation("No items retrieved from cache");

            allShows = subscriptions.AvailableShows
                .Concat(subscriptions.SubscribedShows)
                .OrderBy(c => c.ShowTitle)
                .ToList();
            _memoryCache.Set(tvShowRssShows, allShows);
        }
        else
        {
            _logger.LogInformation($"[{allShows.Count}] showRss shows retrieved from cache");
        }

        return Ok(allShows);
    }

    [HttpGet("subscribedShows")]
    [ProducesResponseType(typeof(List<int>), 200)]
    public ObjectResult GetSubscribed()
    {
        var user = GetUser();
        return Ok(user.TvShowSubscriptions);
    }


    [HttpGet("{showRssId}")]
    public ObjectResult ShowDetails(int showRssId)
    {
        var tvShowRssShowCacheKey = $"[tv-show-rss-{showRssId}]";
        if (!_memoryCache.TryGetValue(tvShowRssShowCacheKey, out string json))
        {
            try
            {
                json = GetShowDetail(showRssId);
                if (string.IsNullOrEmpty(json))
                {
                    return BadRequest(false);
                }
                    
                _memoryCache.Set(tvShowRssShowCacheKey, json);
            }
            catch (Exception ex)
            {
                _memoryCache.Remove(tvShowRssShowCacheKey);
                _logger.LogError("Failed to fetch info for showRssId: " + showRssId);
                _logger.LogError(ex.Message);
                return BadRequest(false);
            }
        }
        else
        {
            _logger.LogInformation($"[{showRssId}] retrieved from cache");
        }
        return Ok(JsonConvert.DeserializeObject(json));
    }

    private string GetShowDetail(int showRssId)
    {
        //Getting tvMazeId
        var showRssUrl = $"https://showrss.info/show/{showRssId}.rss";
        _logger.LogInformation("Getting first show for: " + showRssUrl);
        var rootElement = XElement.Load(showRssUrl);
        XNamespace np = rootElement.Attributes().First(a => a.Value.Contains("showrss")).Value;
        var tvMazeIdContent = rootElement.Descendants(np + "external_id").FirstOrDefault()?.Value;
        Log.Information("tvMazeIdContent: " + tvMazeIdContent);
        if (string.IsNullOrEmpty(tvMazeIdContent) || !int.TryParse(tvMazeIdContent, out var tvMazeId))
        {
            return null;
        }

        //Fetching tvMazeContent
        using (var httpClient = new HttpClient())
        {
            var response = httpClient.GetAsync($"https://api.tvmaze.com/shows/{tvMazeId}?embed[]=nextepisode").Result;
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            var json = response.Content.ReadAsStringAsync().Result;
            return json;
        }

    }
}