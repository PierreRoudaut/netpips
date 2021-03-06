using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Netpips.API.Core.Settings;
using Netpips.API.Media.Filebot;
using Netpips.API.Media.MediaInfo;
using Netpips.API.Media.Service;
using Netpips.Tests.Core;
using NUnit.Framework;

namespace Netpips.Tests.Media.Service;

[TestFixture]
[Category(TestCategory.Filesystem)]
public class MediaLibraryMoverTests
{
    private Mock<ILogger<MediaLibraryMover>> _loggerMock;
    private Mock<IOptions<NetpipsSettings>> _settingsMock;
    private Mock<IFilebotService> _filebotMock;
    private Mock<IMediaInfoService> _mediaInfoMock;
    private Mock<IArchiveExtractorService> _archiveMock;
    private NetpipsSettings _settings;

    [SetUp]
    public void Setup()
    {
        _settings = TestHelper.CreateNetpipsAppSettings();

        _loggerMock = new Mock<ILogger<MediaLibraryMover>>();
        _settingsMock = new Mock<IOptions<NetpipsSettings>>();
        _settingsMock.Setup(x => x.Value).Returns(_settings);
        _filebotMock = new Mock<IFilebotService>();
        _mediaInfoMock = new Mock<IMediaInfoService>();
        _archiveMock = new Mock<IArchiveExtractorService>();
    }

    [Test]
    public void MoveMusicItemTest()
    {

        var mover = new MediaLibraryMover(_settingsMock.Object, _loggerMock.Object, _filebotMock.Object, _mediaInfoMock.Object, _archiveMock.Object);

        var musicFilename = "Cosmic Gate - Be Your Sound.mp3";
        var musicSrcPath = Path.Combine(_settings.DownloadsPath, TestHelper.Uid(), musicFilename);

        TestHelper.CreateFile(musicSrcPath);
        var musicDestPath = mover.MoveMusicFile(musicSrcPath);
        Assert.IsTrue(File.Exists(musicDestPath.FullName), "music test file was not moved");
        Assert.AreEqual(Path.Combine(_settings.MediaLibraryPath, "Music", musicFilename), musicDestPath.FullName);

    }

    [Test]
    public void MoveVideoItemTestCaseRenameOk()
    {

        var videoSrcPath = Path.Combine(_settings.DownloadsPath, TestHelper.Uid(), "the.big.bang.theory.s10.e01.mp4");
        TestHelper.CreateFile(videoSrcPath);

        var videoDestPath = Path.Combine(_settings.MediaLibraryPath, "TV Shows", "The Big Bang Theory", "Season 10", "The Big Bang Theory - S10E01 - The Conjugal Conjecture.mp4");

        _filebotMock.Setup(x => x.Rename(It.IsAny<RenameRequest>())).Returns(new RenameResult {Succeeded = true, DestPath = videoDestPath });
        var mover = new MediaLibraryMover(_settingsMock.Object, _loggerMock.Object, _filebotMock.Object, _mediaInfoMock.Object, _archiveMock.Object);
        var fsItems = mover.MoveVideoFile(videoSrcPath);

        Assert.AreEqual(3, fsItems.Count, "Tbbt folder and season folder should be created");
        Assert.IsTrue(File.Exists(videoDestPath), "Dest video file was not created");
        Assert.IsFalse(File.Exists(videoSrcPath), "Src video is still present");

    }

    [TestCase(0, "Others")]
    [TestCase(1, "TV Shows")]
    [TestCase(40, "TV Shows")]
    [TestCase(105, "Movies")]
    [TestCase(135, "Movies")]
    public void MoveVideoItemTestCaseRenameKo(int minutes, string fallbackDir)
    {

        const string filename = "Unknown Video.mkv";

        var videoSrcPath = Path.Combine(_settings.DownloadsPath, TestHelper.Uid(), filename);
        TestHelper.CreateFile(videoSrcPath);

        var duration = TimeSpan.FromMinutes(minutes);
        _mediaInfoMock.Setup(x => x.TryGetDuration(It.IsAny<string>(), out duration)).Returns(true);

        _filebotMock.Setup(x => x.Rename(It.IsAny<RenameRequest>())).Returns(new RenameResult {Succeeded = false});

        var mover = new MediaLibraryMover(_settingsMock.Object, _loggerMock.Object, _filebotMock.Object, _mediaInfoMock.Object, _archiveMock.Object);
        var movedFsItems = mover.MoveVideoFile(videoSrcPath);

        var videoDestPath = Path.Combine(_settings.MediaLibraryPath, fallbackDir, filename);

        Assert.IsTrue(File.Exists(videoDestPath), "Dest video file was not created");
        Assert.IsFalse(File.Exists(videoSrcPath), "Src video is still present");
        Assert.AreEqual(1, movedFsItems.Count);
        Assert.AreEqual(videoDestPath, movedFsItems.First().FullName);
    }

    [Test]
    public void MoveMatchingSubtitlesOfTest()
    {

        var srcFilename = "the.big.bang.theory.s10.e01.mp4";
        //var handledSubs = new List<string> { ".srt", ".en.srt", ".fr.srt", ".eng.srt", ".fra.srt" };
        var handledSubs = new Dictionary<string, string>
        {
            { ".srt", ".srt" },
            { ".en.srt", ".en.srt" },
            { ".eng.srt", ".en.srt" },
            { ".fra.srt", ".fr.srt" },
            { ".fr.srt", ".fr.srt" },
        };
        var videoSrcPath = Path.Combine(_settings.DownloadsPath, TestHelper.Uid(), srcFilename);

        TestHelper.CreateFile(videoSrcPath);
        handledSubs.ToList().ForEach(subExt =>
        {
            TestHelper.CreateFile(videoSrcPath.GetPathWithoutExtension() + subExt.Key);
        });

        var videoDestPath = Path.Combine(_settings.MediaLibraryPath, "TV Shows", "The Big Bang Theory", "Season 10", "The Big Bang Theory - S10E01 - The Conjugal Conjecture.mp4");
        TestHelper.CreateFile(videoDestPath);

        var downloadCompletedHandler = new MediaLibraryMover(_settingsMock.Object, _loggerMock.Object, _filebotMock.Object, _mediaInfoMock.Object, _archiveMock.Object);
        var movedSubs = downloadCompletedHandler.MoveMatchingSubtitlesOf(videoSrcPath, videoDestPath);

        handledSubs.ToList().ForEach(subExt =>
        {
            Assert.IsTrue(movedSubs.Any(sub => sub.FullName == videoDestPath.GetPathWithoutExtension() + subExt.Value));
            Assert.IsFalse(File.Exists(videoSrcPath.GetPathWithoutExtension() + subExt));
        });
    }

    [Test]
    public void ProcessItemTest()
    {
        var filenameFormat = "Video {0}.mkv";
        int dirCount = 3;
        var itemPath = Path.Combine(_settings.DownloadsPath, TestHelper.Uid());
        var tempPath = itemPath;
        for (int i = 1; i <= dirCount; i++)
        {
            Directory.CreateDirectory(tempPath);
            TestHelper.CreateFile(Path.Combine(tempPath, string.Format(filenameFormat, i)));
            tempPath = Path.Combine(tempPath, TestHelper.Uid());
        }
        var _ = "";
        var duration = TimeSpan.FromMinutes(0);
        _filebotMock.Setup(x => x.Rename(It.IsAny<RenameRequest>())).Returns(new RenameResult {Succeeded = false});
        _filebotMock.Setup(x => x.GetSubtitles(It.IsAny<string>(), out _, It.IsAny<string>(), It.IsAny<bool>())).Returns(false);
        _mediaInfoMock.Setup(x => x.TryGetDuration(It.IsAny<string>(), out duration)).Returns(true);

        var mediaLibraryMover = new MediaLibraryMover(_settingsMock.Object, _loggerMock.Object, _filebotMock.Object, _mediaInfoMock.Object, _archiveMock.Object);

        mediaLibraryMover.ProcessDir(itemPath);
        for (int i = 1; i <= dirCount; i++)
        {
            Assert.IsTrue(File.Exists(Path.Combine(_settings.MediaLibraryPath, "Others", string.Format(filenameFormat, i))));
        }
    }
}