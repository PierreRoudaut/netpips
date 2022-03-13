using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Netpips.API.Core.Model;
using Netpips.API.Core.Settings;
using Netpips.API.Download.Model;
using Netpips.Tests.Core;
using NUnit.Framework;

namespace Netpips.Tests.Download.Model;

[TestFixture]
public class DownloadItemRepositoryTest
{
    private Mock<ILogger<DownloadItemRepository>> _logger;
    private Mock<AppDbContext> _dbContext;
    private NetpipsSettings _settings;

    [SetUp]
    public void Setup()
    {
        _settings = TestHelper.CreateNetpipsAppSettings();
        _dbContext = new Mock<AppDbContext>();
        _logger = new Mock<ILogger<DownloadItemRepository>>();
    }

    [Test]
    public void GetPassedItemsToArchiveTests()
    {
        const int expectedItemsCountToArchive = 4;
        var items = new List<DownloadItem>
        {
            new DownloadItem { Archived = false, CanceledAt = DateTime.Now.AddDays(-7), State = DownloadState.Canceled },
            new DownloadItem { Archived = true, CanceledAt = DateTime.Now.AddDays(-6), State = DownloadState.Canceled },
            new DownloadItem { Archived = false, CanceledAt = DateTime.Now.AddDays(-5), State = DownloadState.Canceled },
            new DownloadItem { Archived = false, CanceledAt = DateTime.Now.AddDays(-1), State = DownloadState.Canceled },
            new DownloadItem { Archived = false, CompletedAt = DateTime.Now.AddDays(-7), State = DownloadState.Completed },
            new DownloadItem { Archived = true, CompletedAt = DateTime.Now.AddDays(-6), State = DownloadState.Completed },
            new DownloadItem { Archived = false, CompletedAt = DateTime.Now.AddDays(-5), State = DownloadState.Completed },
            new DownloadItem { Archived = false, CompletedAt = DateTime.Now.AddDays(-1), State = DownloadState.Completed },
        };


        var itemsQueryable = items.AsQueryable();

        var mockSet = new Mock<DbSet<DownloadItem>>();
        mockSet.As<IQueryable<DownloadItem>>().Setup(m => m.Provider).Returns(itemsQueryable.Provider);
        mockSet.As<IQueryable<DownloadItem>>().Setup(m => m.Expression).Returns(itemsQueryable.Expression);
        mockSet.As<IQueryable<DownloadItem>>().Setup(m => m.ElementType).Returns(itemsQueryable.ElementType);
        mockSet.As<IQueryable<DownloadItem>>().Setup(m => m.GetEnumerator()).Returns(itemsQueryable.GetEnumerator());


        _dbContext.SetupGet(c => c.DownloadItems).Returns(mockSet.Object);

        var repo = new DownloadItemRepository(_logger.Object, _dbContext.Object);
        var toArchive = repo.GetPassedItemsToArchive(expectedItemsCountToArchive);

        Assert.AreEqual(expectedItemsCountToArchive, toArchive.Count);
    }
}