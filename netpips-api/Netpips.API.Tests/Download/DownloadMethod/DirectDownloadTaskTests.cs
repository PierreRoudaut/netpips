using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coravel.Events.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Netpips.API.Core.Settings;
using Netpips.API.Download.DownloadMethod.DirectDownload;
using Netpips.API.Download.Event;
using Netpips.API.Download.Model;
using Netpips.Tests.Core;
using NUnit.Framework;

namespace Netpips.Tests.Download.DownloadMethod;

[TestFixture]
[Category(TestCategory.ThirdParty)]
public class DirectDownloadTaskTest
{
    private List<string> _cookies;
    private NetpipsSettings _settings;
    private Mock<IDispatcher> _dispatcher;

    [SetUp]
    public void Setup()
    {
        _settings = TestHelper.CreateNetpipsAppSettings();

        var loggerMock = new Mock<ILogger<DirectDownloadMethod>>();
        _cookies = new List<string>();
        _dispatcher = new Mock<IDispatcher>();
    }

    [Test]
    public async Task StartTest()
    {
        var testFile = new
        {
            Url = "http://test-debit.free.fr/1024.rnd",
            ExpectedSize = 1048576
        };

        var item = new DownloadItem
        {
            FileUrl = testFile.Url,
            Token = TestHelper.Uid()
        };

        var downloadDestPath = Path.Combine(_settings.DownloadsPath, item.Token, item.FileUrl.Split('/').Last());
        Directory.CreateDirectory(Path.GetDirectoryName(downloadDestPath));
        var directDownloadTask = new DirectDownloadTask(item, _dispatcher.Object, downloadDestPath, _cookies);
        DirectDownloadMethod.DirectDownloadTasks[item.Token] = directDownloadTask;
        await DirectDownloadMethod.DirectDownloadTasks[item.Token].StartAsync();
        Assert.AreEqual(testFile.ExpectedSize, new FileInfo(downloadDestPath).Length,
            "A file of size: " + testFile.ExpectedSize + " should have been downloaded");
        _dispatcher.Verify(x => x.Broadcast(It.IsAny<ItemDownloaded>()), Times.Once);
    }

    [Test]
    public async Task CancelTest()
    {
        var testFile = new
        {
            Url = "http://test-debit.free.fr/65536.rnd",
            ExpectedSize = 67108864
        };

        var item = new DownloadItem
        {
            FileUrl = testFile.Url,
            Token = Guid.NewGuid().ToString()
        };

        var downloadDestPath = Path.Combine(_settings.DownloadsPath, item.Token, item.FileUrl.Split('/').Last());
        Directory.CreateDirectory(Path.GetDirectoryName(downloadDestPath));

        var directDownloadTask = new DirectDownloadTask(item, _dispatcher.Object, downloadDestPath, _cookies);
        DirectDownloadMethod.DirectDownloadTasks[item.Token] = directDownloadTask;

        var task = DirectDownloadMethod.DirectDownloadTasks[item.Token].StartAsync();
        if (await Task.WhenAny(task, Task.Delay(800)) != task)
        {
            DirectDownloadMethod.DirectDownloadTasks[item.Token].Cancel();
            Assert.AreNotEqual(testFile.ExpectedSize, new FileInfo(downloadDestPath).Length, "The file was downloaded entirely despite cancellation");
            _dispatcher.Verify(x => x.Broadcast(It.IsAny<ItemDownloaded>()), Times.Never);
        }
    }
}