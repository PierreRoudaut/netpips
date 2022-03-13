using System;
using Microsoft.Extensions.Logging;
using Moq;
using Netpips.API.Download.Event;
using Netpips.API.Download.Model;
using Netpips.API.Media.Service;
using NUnit.Framework;

namespace Netpips.Tests.Download.Event;

[TestFixture]
public class ProcessDownloadItemTests
{
    private Mock<ILogger<ProcessDownloadItem>> _logger;
    private Mock<IDownloadItemRepository> _repository;
    private Mock<IMediaLibraryMover> _mediaLibraryMover;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<ProcessDownloadItem>>();
        _repository = new Mock<IDownloadItemRepository>();
        _mediaLibraryMover = new Mock<IMediaLibraryMover>();
    }

    [Test]
    public void HandleAsyncTest()
    {
        var item = new DownloadItem { };
        _repository.Setup(x => x.Find(It.IsAny<Guid>())).Returns(item);

        var job = new ProcessDownloadItem(_logger.Object, _repository.Object, _mediaLibraryMover.Object);
        job.HandleAsync(new ItemDownloaded(new Guid()));
        _repository.Verify(x => x.Update(It.IsAny<DownloadItem>()), Times.Exactly(2));
    }
}