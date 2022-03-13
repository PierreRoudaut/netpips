using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Netpips.API.Core.Settings;
using Netpips.API.Media.Filebot;
using Netpips.Tests.Core;
using NUnit.Framework;

namespace Netpips.Tests.Media.Service;

[TestFixture]
[Category(TestCategory.Filebot)]
[Category(TestCategory.LocalDependency)]
[Category(TestCategory.ThirdParty)]
public class FilebotServiceTests
{
    private NetpipsSettings _settings;

    [SetUp]
    public void SetUp()
    {
        _settings = TestHelper.CreateNetpipsAppSettings();
    }

    [Test]
    public void RenameTest_Case_Success()
    {
        const string p = nameof(RenameTest_Case_Success);
        TestContext.Progress.WriteLine($"{p} START");
        var path = Path.Combine(_settings.DownloadsPath,
            "The.Big.Bang.Theory.S10E01.FASTSUB.VOSTFR.HDTV.x264-FDS.mkv");
        TestHelper.CreateFile(path);
        var loggerMock = new Mock<ILogger<IFilebotService>>();
        var filebot = new FilebotService(loggerMock.Object);
        var expectedPath = Path.Combine(
            _settings.MediaLibraryPath,
            "TV Shows",
            "The Big Bang Theory",
            "Season 10",
            "The Big Bang Theory - S10E01 - The Conjugal Conjecture.mkv");

        var result = filebot.Rename(new RenameRequest {Path = path, BaseDestPath = _settings.MediaLibraryPath});
            
        Assert.IsTrue(result.Succeeded, $"Failed to rename using Filebot{Environment.NewLine + result.ToStringOfProperties()}");
        Assert.AreEqual(expectedPath, result.DestPath, "Expected path and dest path should be identical");
    }

    [Test]
    public void RenameTest_Case_FileAlreadyExists()
    {
        const string p = nameof(RenameTest_Case_FileAlreadyExists);
        TestContext.Progress.WriteLine($"{p} START");
            
        var path = Path.Combine(_settings.DownloadsPath,
            "The.Big.Bang.Theory.S10E01.FASTSUB.VOSTFR.HDTV.x264-FDS.mp4");
        TestHelper.CreateFile(path);

        var alreadyExistingFilePath = Path.Combine(
            _settings.MediaLibraryPath,
            "TV Shows",
            "The Big Bang Theory",
            "Season 10",
            "The Big Bang Theory - S10E01 - The Conjugal Conjecture.mp4");
        TestHelper.CreateFile(alreadyExistingFilePath);

        var loggerMock = new Mock<ILogger<IFilebotService>>();
        var filebot = new FilebotService(loggerMock.Object);
            
        var result = filebot.Rename(new RenameRequest {Path = path, BaseDestPath = _settings.MediaLibraryPath});
            
        Assert.IsTrue(result.Succeeded, $"Failed to rename using Filebot{Environment.NewLine + result.ToStringOfProperties()}");
        Assert.AreEqual(alreadyExistingFilePath, result.DestPath);
    }


    [Test]
    public void RenameTest_Case_Failure()
    {
        var path = Path.Combine(_settings.DownloadsPath, Guid.NewGuid().ToString("N") + ".mp4");
        TestHelper.CreateFile(path);
        var loggerMock = new Mock<ILogger<IFilebotService>>();
        var filebot = new FilebotService(loggerMock.Object);
            
        var result = filebot.Rename(new RenameRequest {Path = path, BaseDestPath = _settings.MediaLibraryPath});
        Assert.IsFalse(result.Succeeded, $"Filebot should not have succeeded{Environment.NewLine + result.ToStringOfProperties()}");
    }

    [Test]
    public void GetSubtitlesTest_Case_NonStrict()
    {
        var itemDir = Path.Combine(_settings.DownloadsPath, TestHelper.Uid());
        Directory.CreateDirectory(itemDir);
        var itemPath = Path.Combine(itemDir, "The Big Bang Theory - S11E17 - The Athenaeum Allocation.mkv");
        TestHelper.CreateFile(itemPath);
        var loggerMock = new Mock<ILogger<IFilebotService>>();
        var filebot = new FilebotService(loggerMock.Object);
        Assert.IsTrue(filebot.GetSubtitles(itemPath, out var srtPath, nonStrict: true),
            "filebot -get-subtitles failed");
        Assert.IsTrue(File.Exists(srtPath), ".srt not found");
    }

    [Test]
    public void GetSubtitlesTest_Case_StrictOn_ShouldFail()
    {
        var itemDir = Path.Combine(_settings.DownloadsPath, TestHelper.Uid());
        Directory.CreateDirectory(itemDir);
        var itemPath = Path.Combine(itemDir, "The Big Bang Theory - S11E17 - The Athenaeum Allocation.mkv");
        TestHelper.CreateFile(itemPath);
        var loggerMock = new Mock<ILogger<IFilebotService>>();
        var filebot = new FilebotService(loggerMock.Object);
        var result = filebot.GetSubtitles(itemPath, out var srtPath);
        Assert.IsFalse(result);
    }

    [Test]
    public void GetSubtitlesTest_Case_WithLang_NonStrict()
    {
        var itemDir = Path.Combine(_settings.DownloadsPath, TestHelper.Uid());
        Directory.CreateDirectory(itemDir);
        var path = Path.Combine(itemDir, "The Big Bang Theory - S11E17 - The Athenaeum Allocation.mkv");
        TestHelper.CreateFile(path);
        var loggerMock = new Mock<ILogger<IFilebotService>>();
        var filebot = new FilebotService(loggerMock.Object);
        Assert.IsTrue(filebot.GetSubtitles(path, out var srtPath, "eng", nonStrict: true),
            "filebot -get-subtitles failed");
        Assert.IsTrue(File.Exists(srtPath), ".srt not found");
    }
}