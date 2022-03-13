using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Netpips.API.Core.Settings;
using Netpips.API.Media.Filebot;
using Netpips.API.Media.Model;
using Netpips.API.Media.Service;
using Netpips.Tests.Core;
using NUnit.Framework;

namespace Netpips.Tests.Media.Service;

[TestFixture]
public class MediaServiceTests
{
    private Mock<ILogger<MediaLibraryService>> _logger;

    private Mock<IOptions<NetpipsSettings>> _settings;

    private Mock<IMediaLibraryMover> _mover;
    private Mock<IFilebotService> _filebot;

    [SetUp]
    public void Setup()
    {
        _settings = new Mock<IOptions<NetpipsSettings>>();
        _mover = new Mock<IMediaLibraryMover>();
        _settings.SetupGet(x => x.Value).Returns(TestHelper.CreateNetpipsAppSettings());
        _logger = new Mock<ILogger<MediaLibraryService>>();
        _filebot = new Mock<IFilebotService>();
    }

    [Test]
    public void AutoRenameTest()
    {
        var fileInfo = new FileInfo(
            Path.Combine(
                _settings.Object.Value.MediaLibraryPath,
                "TV Shows",
                "The Big Bang Theory",
                "Season 01",
                "The Big Bang Theory - S10E01 - The Conjugal Conjecture.mp4"));
        fileInfo.Directory.Create();
        File.WriteAllText(fileInfo.FullName, "abcd");


        // directory
        var service = new MediaLibraryService(_logger.Object, _settings.Object, _mover.Object, _filebot.Object);
        var item = new PlainMediaItem(fileInfo.Directory, _settings.Object.Value.MediaLibraryPath);
        Assert.Null(service.AutoRename(item));

        _mover.Setup(x => x.MoveVideoFile(fileInfo.FullName)).Returns(new List<FileSystemInfo>());
        service = new MediaLibraryService(_logger.Object, _settings.Object, _mover.Object, _filebot.Object);
        service.AutoRename(new PlainMediaItem(fileInfo, _settings.Object.Value.MediaLibraryPath));
        _mover.Verify(x => x.MoveVideoFile(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void GetSubtitlesTest()
    {
        var fileInfo = new FileInfo(
            Path.Combine(
                _settings.Object.Value.MediaLibraryPath,
                "TV Shows",
                "The Big Bang Theory",
                "Season 01",
                "The Big Bang Theory - S10E01 - The Conjugal Conjecture.mp4"));
        fileInfo.Directory.Create();
        File.WriteAllText(fileInfo.FullName, "abcd");


        // directory
        var service = new MediaLibraryService(_logger.Object, _settings.Object, _mover.Object, _filebot.Object);
        var item = new PlainMediaItem(fileInfo.Directory, _settings.Object.Value.MediaLibraryPath);
        Assert.Null(service.GetSubtitles(item, "eng"));

        // no subtitles found
        string _;
        _filebot.Setup(x => x.GetSubtitles(It.IsAny<string>(), out _, It.IsAny<string>(), It.IsAny<bool>())).Returns(false);
        service = new MediaLibraryService(_logger.Object, _settings.Object, _mover.Object, _filebot.Object);
        item = new PlainMediaItem(fileInfo, _settings.Object.Value.MediaLibraryPath);
        Assert.Null(service.GetSubtitles(item, "eng"));


        // subtitles found
        var srtPath = Path.Combine(
            Path.GetDirectoryName(fileInfo.FullName),
            Path.GetFileNameWithoutExtension(fileInfo.Name) + ".eng.srt");
        File.WriteAllText(srtPath, "aaa");
        _filebot.Setup(x => x.GetSubtitles(It.IsAny<string>(), out srtPath, It.IsAny<string>(), It.IsAny<bool>())).Returns(true);
        service = new MediaLibraryService(_logger.Object, _settings.Object, _mover.Object, _filebot.Object);
        item = new PlainMediaItem(fileInfo, _settings.Object.Value.MediaLibraryPath);
        var srtItem = service.GetSubtitles(item, "eng");
        Assert.AreEqual(3, srtItem.Size);
        Assert.AreEqual("TV Shows/The Big Bang Theory/Season 01", srtItem.Parent);
    }
}