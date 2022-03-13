using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using Netpips.API.Core.Settings;
using Netpips.API.Download.DownloadMethod.PeerToPeer;
using Netpips.API.Download.Exception;
using Netpips.API.Download.Model;
using Netpips.Tests.Core;
using NUnit.Framework;

namespace Netpips.Tests.Download.DownloadMethod;

[TestFixture]
[Category(TestCategory.Integration)]
public class P2PDownloadMethodTests
{
    private P2PDownloadMethod _downloadMethod;
    private Mock<ILogger<P2PDownloadMethod>> _logger;
    private Mock<IOptions<NetpipsSettings>> _settingsMock;
    private Mock<IAria2CService> _ariaService;
    private Mock<ITorrentDaemonService> _torrentService;
    private NetpipsSettings _settings;


    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<P2PDownloadMethod>>();
        _settingsMock = new Mock<IOptions<NetpipsSettings>>();
        _ariaService = new Mock<IAria2CService>();
        _torrentService = new Mock<ITorrentDaemonService>();

        _settings = TestHelper.CreateNetpipsAppSettings();
        _settingsMock.Setup(x => x.Value).Returns(_settings);
    }

    [TestCase("magnet:?magnetLinkNotFound", P2PDownloadMethod.BrokenMagnetLinkMessage, 1)]
    [TestCase("magnet:?magnetLinkTimeout", P2PDownloadMethod.NoPeersFoundMessage, -1)]
    public void DownloadCaseInvalidMagnet(string magnetLink, string expectedExceptionMessage, int ariaReturnValue)
    {
        var item = new DownloadItem { FileUrl = magnetLink };

        _ariaService
            .Setup(x => x.DownloadTorrentFile(It.Is<string>(url => url == magnetLink), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(ariaReturnValue);
        _downloadMethod =
            new P2PDownloadMethod(_logger.Object, _settingsMock.Object, _ariaService.Object, _torrentService.Object);

        var ex = Assert.Throws<FileNotDownloadableException>(() => _downloadMethod.Start(item));
        Assert.AreEqual(expectedExceptionMessage, ex.Message);
    }

    [Test]
    public void DownloadCaseInvalidTorrent()
    {

        _downloadMethod =
            new P2PDownloadMethod(_logger.Object, _settingsMock.Object, _ariaService.Object, _torrentService.Object);

        var item = new DownloadItem
        {
            FileUrl = "http://invalid-torrent-url.com/file.torrent"
        };

        var ex = Assert.Throws<FileNotDownloadableException>(() => _downloadMethod.Start(item));
        Assert.AreEqual(P2PDownloadMethod.TorrentFileNotFoundMessage, ex.Message);
    }

    [Test]
    public void DownloadCaseCorruptedTorrent()
    {
        _downloadMethod =
            new P2PDownloadMethod(_logger.Object, _settingsMock.Object, _ariaService.Object, _torrentService.Object);

        var corruptedTorrentPath =
            TestHelper.GetRessourceFullPath("The.Big.Bang.Theory.S11E14.HDTV.x264-SVA[rarbg].corrupted.torrent");
        var item = new DownloadItem
        {
            FileUrl = corruptedTorrentPath
        };
        var ex = Assert.Throws<FileNotDownloadableException>(() => _downloadMethod.Start(item));
        Assert.AreEqual(P2PDownloadMethod.TorrentFileCorrupted, ex.Message);
    }

    [Test]
    public void DownloadCaseFailedToAddTorrentToDaemon()
    {
        _torrentService.Setup(x => x.AddTorrent(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _downloadMethod =
            new P2PDownloadMethod(_logger.Object, _settingsMock.Object, _ariaService.Object, _torrentService.Object);

        var torrentPath =
            TestHelper.GetRessourceFullPath("The.Big.Bang.Theory.S11E14.HDTV.x264-SVA[rarbg].torrent");

        var item = new DownloadItem
        {
            FileUrl = torrentPath
        };

        var ex = Assert.Throws<StartDownloadException>(() => _downloadMethod.Start(item));
        Assert.AreEqual(P2PDownloadMethod.TorrentDaemonAddFailureMessage, ex.Message);
    }

    [Test]
    public void DownloadCaseValid()
    {
        _torrentService.Setup(x => x.AddTorrent(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _downloadMethod =
            new P2PDownloadMethod(_logger.Object, _settingsMock.Object, _ariaService.Object, _torrentService.Object);

        var torrentPath =
            TestHelper.GetRessourceFullPath("The.Big.Bang.Theory.S11E14.HDTV.x264-SVA[rarbg].torrent");

        var item = new DownloadItem
        {
            FileUrl = torrentPath
        };

        Assert.DoesNotThrow(() => _downloadMethod.Start(item));
        Assert.AreEqual("The.Big.Bang.Theory.S11E14.HDTV.x264-SVA[rarbg]", item.Name);
        Assert.AreEqual(140940255, item.TotalSize);
        Assert.AreEqual("25c8f093021fd9d97087f9444c160d9bb3d70e35", item.Hash);
    }

    [TestCase("https://torrents.yts.rs/torrent/download/227F05638D05C6798B4D86E34429FB7D34474576", true)]
    [TestCase("magnet:?1234", true)]
    [TestCase("http://some-torrent-forum.com/file.torrent", false)]
    [TestCase("https://wikipedia.org", false)]
    [TestCase("https://en.wikipedia.org/404", false)]
    [TestCase("http://test-debit.free.fr/1024.rnd", false)]
    [TestCase("http://invalid-forum.com/1234", false)]
    public void CanHandle(string url, bool expectedResult)
    {
        var autoMocker = new AutoMocker();
        var method = autoMocker.CreateInstance<P2PDownloadMethod>();
        Assert.AreEqual(expectedResult, method.CanHandle(url));
    }
}