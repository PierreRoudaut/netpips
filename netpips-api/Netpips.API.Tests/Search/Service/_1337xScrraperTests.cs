using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Netpips.API.Search.Service;
using Netpips.Tests.Core;
using NUnit.Framework;

namespace Netpips.Tests.Search.Service;

[TestFixture]
public class _1337xScrraperTests
{
    public Mock<ILogger<_1337xScrapper>> Logger;

    [SetUp]
    public void Setup()
    {
        Logger = new Mock<ILogger<_1337xScrapper>>();
    }

    [Test]
    [Category(TestCategory.Integration)]
    [Category(TestCategory.ThirdParty)]
    [Ignore("1337x down")]
    public async Task SearchAsyncTest()
    {
        var service = new _1337xScrapper(Logger.Object);
        var result = await service.SearchAsync("Game of thrones");
        Assert.GreaterOrEqual(result.Items.Count, 10);
    }

    [Test]
    [Category(TestCategory.Integration)]
    [Category(TestCategory.ThirdParty)]
    [Ignore("1337x has SSL error when attempting Python cfscrape")]
    public async Task ScrapeTorrentUrlAsyncTest()
    {
        const string scrapeUrl = "https://1337x.to/torrent/1338262/Armageddon-1998-1080p-BrRip-x264-YIFY/";
        var service = new _1337xScrapper(Logger.Object);

        var torrentUrl = await service.ScrapeTorrentUrlAsync(scrapeUrl);
        Assert.NotNull(torrentUrl);
        Assert.IsTrue(torrentUrl.StartsWith("magnet:?xt=urn:btih:64962623F6A161302220AB7829BA08DC5AD040B8&dn=Armageddon+1998+1080p+BrRip+x264+YIFY"));
    }

    [Test]
    public void ParseTorrentSearchItemsTest()
    {
        var service = new _1337xScrapper(Logger.Object);
        var htmlRessourceContent = TestHelper.GetRessourceContent("1337x_search_results.html");

        var items = service.ParseTorrentSearchResult(htmlRessourceContent);
        Assert.AreEqual(16, items.Count);
    }

    [Test]
    public void ParseTorrentDetailResultTest()
    {
        var service = new _1337xScrapper(Logger.Object);
        var htmlRessourceContent = TestHelper.GetRessourceContent("1337x_torrent_detail_result.html");

        const string expectedTorrentUrl = "magnet:?xt=urn:btih:B94867C9021189F3FFAA3CAC56AA75FA1122CC46&dn=Game.of.Thrones.S01E09.2011.Multi.BluRay.2160p.x265.HDR.-DTOne&tr=udp%3A%2F%2Ftracker.leechers-paradise.org%3A6969&tr=udp%3A%2F%2Ftracker.coppersurfer.tk%3A6969&tr=udp%3A%2F%2Ftracker.coppersurfer.tk%3A6969%2Fannounce&tr=udp%3A%2F%2F9.rarbg.to%3A2740%2Fannounce&tr=udp%3A%2F%2F9.rarbg.me%3A2710%2Fannounce&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337%2Fannounce&tr=udp%3A%2F%2Feddie4.nl%3A6969%2Fannounce&tr=udp%3A%2F%2Fshadowshq.yi.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.leechers-paradise.org%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.tiny-vps.com%3A6969%2Fannounce&tr=udp%3A%2F%2Finferno.demonoid.pw%3A3391%2Fannounce&tr=udp%3A%2F%2Ftracker.pirateparty.gr%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.vanitycore.co%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce&tr=udp%3A%2F%2Ftracker.zer0day.to%3A1337%2Fannounce&tr=udp%3A%2F%2Ftracker.leechers-paradise.org%3A6969%2Fannounce&tr=udp%3A%2F%2Fcoppersurfer.tk%3A6969%2Fannounce";
        var url = service.ParseFirstMagnetLinkOrDefault(htmlRessourceContent);
        Assert.AreEqual(expectedTorrentUrl, url);
    }
}