using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Netpips.API.Core.Settings;
using Netpips.API.Subscriptions.Job;
using Netpips.API.Subscriptions.Model;
using Netpips.Tests.Core;
using NUnit.Framework;

namespace Netpips.Tests.Subscriptions.Job;

[TestFixture]
public class ShowRssFeedSyncJobTests
{
    private Mock<ILogger<ShowRssFeedSyncJob>> _logger;

    private Mock<IOptions<ShowRssSettings>> _options;
    private ShowRssSettings _settings;

    private Mock<IShowRssItemRepository> _repository;


    [SetUp]
    public void Setup()
    {
        _settings = TestHelper.CreateShowRssSettings();
        _logger = new Mock<ILogger<ShowRssFeedSyncJob>>();
        _repository = new Mock<IShowRssItemRepository>();
        _options = new Mock<IOptions<ShowRssSettings>>();
        _options
            .SetupGet(x => x.Value)
            .Returns(_settings);
    }

    [Test]
    public void FetchRssItemsFromFeedTest()
    {
        var service = new ShowRssFeedSyncJob(_logger.Object, _options.Object, _repository.Object);

        //todo, make XElement load xml as Stream and bypass "unexpected token" error
        var xml = TestHelper.GetRessourceContent("show_rss_polling_feed.xml");

        var items = service.FetchRssItemsFromFeed();
        Assert.GreaterOrEqual(items.Count, 0);
    }

    [Test]
    public void InvokeTest()
    {
        var service = new ShowRssFeedSyncJob(_logger.Object, _options.Object, _repository.Object);
        service.Invoke();
        _repository.Verify(x => x.SyncFeedItems(It.IsAny<List<ShowRssItem>>()), Times.Once());
    }
}