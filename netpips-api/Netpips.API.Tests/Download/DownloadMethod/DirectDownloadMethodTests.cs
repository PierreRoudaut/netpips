using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coravel.Events.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using Netpips.API.Core.Settings;
using Netpips.API.Download.DownloadMethod.DirectDownload;
using Netpips.API.Download.Model;
using Netpips.Tests.Core;
using NUnit.Framework;

namespace Netpips.Tests.Download.DownloadMethod;

[TestFixture]
[Category(TestCategory.ThirdParty)]
public class DirectDownloadMethodTest
{
    private DirectDownloadMethod _downloadMethod;
    private Mock<IOptions<NetpipsSettings>> _settingsMock;
    private Mock<IOptions<DirectDownloadSettings>> _directDownloadSettings;
    private AutoMocker _autoMocker;

    [SetUp]
    public void Setup()
    {
        _settingsMock = new Mock<IOptions<NetpipsSettings>>();
        _settingsMock.Setup(x => x.Value).Returns(TestHelper.CreateNetpipsAppSettings());
        _directDownloadSettings = new Mock<IOptions<DirectDownloadSettings>>();
        _directDownloadSettings.Setup(x => x.Value).Returns(TestHelper.CreateDirectDownloadSettings());

        _autoMocker = new AutoMocker();
        _autoMocker.Use(new Mock<IDispatcher>().Object);
        _autoMocker.Use(new Mock<ILogger<DirectDownloadMethod>>().Object);
        _autoMocker.Use(_settingsMock.Object);
        _autoMocker.Use(_directDownloadSettings.Object);

        _downloadMethod = _autoMocker.CreateInstance<DirectDownloadMethod>();
    }

    [Test]
    public void CheckDownloabilityTestCaseValid()
    {

        var item = new DownloadItem
        {
            FileUrl = "http://test-debit.free.fr/1024.rnd"
        };

        var result = _downloadMethod.CheckDownloadability(item, new List<string>());
        Assert.IsTrue(result);
        Assert.IsFalse(string.IsNullOrEmpty(item.Name));
        Assert.AreEqual(1048576, item.TotalSize);
    }

    [TestCase("http://test-debit.free.fr")]
    [TestCase("http://test-debit.free.fr/lalala")]
    public void CheckDownloabilityTestCaseInvalid(string url)
    {

        var item = new DownloadItem
        {
            FileUrl = url
        };

        var result = _downloadMethod.CheckDownloadability(item, new List<string>());
        Assert.IsFalse(result);
    }

    [Test]
    public void Authenticate1FichierTestCaseValid()
    {
        var filehoster = _autoMocker.Get<IOptions<DirectDownloadSettings>>().Value.Filehosters.First(f => f.Name == "1fichier");
        List<string> cookies = null;
        Assert.DoesNotThrow(() => cookies = _downloadMethod.Authenticate(filehoster));
        Assert.GreaterOrEqual(cookies.Count, 1, "Authentication on " + filehoster.Name + " should have retrieved at least 1 cookie");
    }

    // multiple wrong login attempts locks the IP on 1fichier.com
    //[Test]
    //public void Authenticate1FichierTestCaseInvalid()
    //{
    //    var filehoster = DirectDownloadMethod.FileHosterInfos.First(f => f.Name == "1fichier");
    //    filehoster.CredentialsData["pass"] = "wrongpassword";

    //    Assert.Throws<StartDownloadException>(() => this.downloadMethod.Authenticate(filehoster));
    //}

    [Test]
    public void StartDownloadTest()
    {
        //todo: implement
        var beforeCount = DirectDownloadMethod.DirectDownloadTasks.Count();
    }

    [Test]
    public void CancelTest()
    {
        var item = new DownloadItem { Token = "ABCD" };
        Assert.False(_downloadMethod.Cancel(item));

        var task = new Mock<IDirectDownloadTask>();
        DirectDownloadMethod.DirectDownloadTasks[item.Token] = task.Object;
        Assert.True(_downloadMethod.Cancel(item));
        task.Verify(t => t.Cancel(), Times.Once);
    }

    [Test]
    public void GetDownloadedSizeTest()
    {
        var item = new DownloadItem { Token = TestHelper.Uid() };

        const string filenameFormat = "Video {0}.mkv";
        const int dirCount = 3;
        var itemPath = Path.Combine(_settingsMock.Object.Value.DownloadsPath, item.Token);
        var tempPath = itemPath;
        for (var i = 1; i <= dirCount; i++)
        {
            Directory.CreateDirectory(tempPath);
            File.WriteAllText(Path.Combine(tempPath, string.Format(filenameFormat, i)), i.ToString());
        }

        Assert.AreEqual(dirCount, _downloadMethod.GetDownloadedSize(item));
    }
}