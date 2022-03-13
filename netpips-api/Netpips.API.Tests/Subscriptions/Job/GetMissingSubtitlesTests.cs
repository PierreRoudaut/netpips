using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using Netpips.Tests.Core;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using Netpips.API.Core.Settings;
using Netpips.API.Download.Model;
using Netpips.API.Media.Filebot;
using Netpips.API.Media.Model;
using Netpips.API.Subscriptions.Job;
using Netpips.API.Subscriptions.Model;

namespace Netpips.Tests.Subscriptions.Job;

[TestFixture]
public class GetMissingSubtitlesTests
{
    private Mock<IOptions<NetpipsSettings>> _options;
    private NetpipsSettings _settings;
    private Mock<IFilebotService> _filebot;

    private Mock<IShowRssItemRepository> _repository;

    private AutoMocker _autoMocker;

    [SetUp]
    public void Setup()
    {
        _autoMocker = new AutoMocker();
        _filebot = new Mock<IFilebotService>();
        _options = new Mock<IOptions<NetpipsSettings>>();
        _settings = TestHelper.CreateNetpipsAppSettings();
        _options.SetupGet(x => x.Value).Returns(_settings);
        _autoMocker.Use(_options.Object);
        _autoMocker.Use(_filebot.Object);
        _repository = new Mock<IShowRssItemRepository>();
    }

    [Test]
    public void InvokeTest()
    {
        // finish implementation

        var movedItem = new DownloadItem
        {
            MovedFiles = new List<MediaItem>
            {
                new MediaItem
                {
                    Path = "TV Shows/Suits/Season 01/Suits S01E01 Episode Name.mkv"
                }
            }
        };

        var path = Path.Combine(_options.Object.Value.MediaLibraryPath,
            "TV Shows", "Game Of Thrones", "Season 01", "Game Of Thrones S01E01 Episode Name.mkv");
        TestHelper.CreateFile(path);
        var pmi = new PlainMediaItem(new FileInfo(path), _options.Object.Value.MediaLibraryPath);

        var missingSubItem = new DownloadItem
        {
            MovedFiles = new List<MediaItem>
            {
                new MediaItem
                {
                    Path = pmi.Path
                }
            }
        };

        var items = new List<DownloadItem> { movedItem, missingSubItem };
        _repository.Setup(x => x.FindRecentCompletedItems(It.IsAny<int>())).Returns(items);
        _autoMocker.Use(_repository.Object);

        var job = _autoMocker.CreateInstance<GetMissingSubtitlesJob>();
        job.Invoke();
        var outSrtPath = It.IsAny<string>();
        _filebot.Verify(x => x.GetSubtitles(It.IsAny<string>(), out outSrtPath, It.IsAny<string>(), false), Times.Exactly(2));
    }
}