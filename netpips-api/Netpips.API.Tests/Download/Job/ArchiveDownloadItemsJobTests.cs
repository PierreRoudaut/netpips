using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Netpips.API.Download.Job;
using Netpips.API.Download.Model;
using Netpips.API.Download.Service;
using NUnit.Framework;

namespace Netpips.Tests.Download.Job;

[TestFixture]
public class ArchiveDownloadItemsJobTests
{
    private  Mock<ILogger<ArchiveDownloadItemsJob>> _logger;

    private Mock<IDownloadItemRepository> _repository;

    private Mock<IDownloadItemService> _service;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<ArchiveDownloadItemsJob>>();
        _repository = new Mock<IDownloadItemRepository>();
        _service = new Mock<IDownloadItemService>();
    }

    [Test]
    public void Invoke()
    {
        var items = new List<DownloadItem>
        {
            new DownloadItem { Archived = false, CanceledAt = DateTime.Now.AddDays(-7), State = DownloadState.Canceled },
        };
        _repository.Setup(x => x.GetPassedItemsToArchive(It.IsAny<int>())).Returns(items);

        var job = new ArchiveDownloadItemsJob(_logger.Object, _repository.Object, _service.Object);
        job.Invoke();
        _service.Verify(x => x.ArchiveDownload(It.IsAny<DownloadItem>()), Times.Once);

    }
}