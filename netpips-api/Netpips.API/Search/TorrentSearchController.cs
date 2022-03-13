using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Netpips.API.Core.Extensions;
using Netpips.API.Search.Model;
using Netpips.API.Search.Service;

namespace Netpips.API.Search;

[Produces("application/json")]
[Route("api/[controller]")]
[Authorize]
public class TorrentSearchController : Controller
{
    private readonly ILogger<TorrentSearchController> _logger;

    private readonly IMemoryCache _memoryCache;

    private readonly IServiceProvider _serviceProvider;

    private static readonly TimeSpan SearchTimeout = TimeSpan.FromMilliseconds(3500);

    public TorrentSearchController(ILogger<TorrentSearchController> logger, IMemoryCache memoryCache, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _serviceProvider = serviceProvider;
    }

    [HttpGet("", Name = "SearchAsyncParallel")]
    [ProducesResponseType(typeof(IList<TorrentSearchItem>), 200)]
    public async Task<ObjectResult> SearchAsyncParallel([FromQuery] string q)
    {
        q = q.Replace("'", "");
        var cacheSearchKey = $"[torrent-search][{q}]";
        _logger.LogInformation(cacheSearchKey);
        var items = new List<TorrentSearchItem>();
        if (!_memoryCache.TryGetValue(cacheSearchKey, out items))
        {
            _logger.LogInformation("No items retrieved from cache");
            var searchScrappers = _serviceProvider.GetServices<ITorrentSearchScrapper>().ToList();
            _logger.LogInformation($"Using [{searchScrappers.Count}] scrappers [{string.Join(", ", searchScrappers.Select(x => x.GetType().Name))}] with timeout {SearchTimeout.TotalMilliseconds} ms");

            var tasks = searchScrappers.Select(s => s.SearchAsync(q));
            var agregatedResults = await tasks.WhenAll(SearchTimeout);
            _logger.LogInformation($"[{agregatedResults.Length}] scrappers completed within timeout");
            items = agregatedResults.Where(x => x.Succeeded).SelectMany(c => c.Items).OrderByDescending(r => r.Seeders).Take(20).ToList();

            _logger.LogInformation($"[{agregatedResults.Length}] scrappers completed within timeout [{items.Count}] total");
            var breakdown = items.GroupBy(c => new Uri(c.ScrapeUrl).Host);
            _logger.LogInformation(string.Join(", ", breakdown.Select(g => $"[{g.Key} => {g.Count()}]")));
            _memoryCache.Set(cacheSearchKey, items, TimeSpan.FromMinutes(5));
        }
        else
        {
            _logger.LogInformation($"[{items.Count}] items retrieved from cache");
        }
        return Ok(items);
    }

    [HttpPost("scrapeTorrentUrl", Name = "ScrapeTorrentUrl")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 404)]
    public async Task<ObjectResult> ScrapeTorrentUrlAsync([FromBody] string scrapeUrl)
    {
        var scrapeUri = new Uri(scrapeUrl);
        var torrentDetailScrapper = _serviceProvider.GetServices<ITorrentDetailScrapper>().FirstOrDefault(s => s.CanScrape(scrapeUri));
        if (torrentDetailScrapper == null)
        {
            return BadRequest("Not handled url");
        }

        var torrentUrl = await torrentDetailScrapper.ScrapeTorrentUrlAsync(scrapeUrl);
        if (torrentUrl == null)
        {
            return NotFound("Torrent link not found. Please try another link");
        }
        return Ok(torrentUrl);
    }
}