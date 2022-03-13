using Microsoft.Extensions.Logging;
using Moq;
using Netpips.API.Download.Controller;
using Netpips.API.Download.Model;
using Netpips.API.Download.Service;
using Netpips.API.Identity.Model;
using Netpips.API.Subscriptions.Job;
using Netpips.API.Subscriptions.Model;
using NUnit.Framework;

namespace Netpips.Tests.Subscriptions.Job;

[TestFixture]
public class ShowRssFeedConsumerJobTests
{
    private Mock<ILogger<ShowRssFeedConsumerJob>> _logger;


    private Mock<IShowRssItemRepository> _showRssItemrepository;
    private Mock<IUserRepository> _userRepository;

    private Mock<IDownloadItemService> _downloadItemService;


    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<ShowRssFeedConsumerJob>>();
        _showRssItemrepository = new Mock<IShowRssItemRepository>();
        _userRepository = new Mock<IUserRepository>();
        _downloadItemService = new Mock<IDownloadItemService>();
           
    }

    [Test]
    public void InvokeTest_NoItemToConsume()
    {
        _showRssItemrepository.Setup(x => x.FindFirstQueuedItem()).Returns((ShowRssItem)null);
        var service = new ShowRssFeedConsumerJob(
            _logger.Object,
            _showRssItemrepository.Object,
            _downloadItemService.Object,
            _userRepository.Object);
        service.Invoke();
        DownloadItemActionError error;
        _downloadItemService.Verify(x => x.StartDownload(It.IsAny<DownloadItem>(), out error), Times.Never);

    }

    [Test]
    public void InvokeTest_StartFailed()
    {
        _userRepository.Setup(c => c.GetDaemonUser()).Returns(new User());
        _showRssItemrepository.Setup(x => x.FindFirstQueuedItem()).Returns(new ShowRssItem());
        DownloadItemActionError error;
        _downloadItemService.Setup(x => x.StartDownload(It.IsAny<DownloadItem>(), out error)).Returns(false);
        var service = new ShowRssFeedConsumerJob(
            _logger.Object,
            _showRssItemrepository.Object,
            _downloadItemService.Object,
            _userRepository.Object);
        service.Invoke();
        _showRssItemrepository.Verify(x => x.Update(It.IsAny<ShowRssItem>()), Times.Never);

    }

    [Test]
    public void InvokeTest_Ok()
    {
        _userRepository.Setup(c => c.GetDaemonUser()).Returns(new User());
        _showRssItemrepository.Setup(x => x.FindFirstQueuedItem()).Returns(new ShowRssItem());
        var item = new DownloadItem();
        DownloadItemActionError error;
        _downloadItemService.Setup(x => x.StartDownload(It.IsAny<DownloadItem>(), out error)).Returns(true);
        var service = new ShowRssFeedConsumerJob(
            _logger.Object,
            _showRssItemrepository.Object,
            _downloadItemService.Object,
            _userRepository.Object);
        service.Invoke();
        _showRssItemrepository.Verify(x => x.Update(It.IsAny<ShowRssItem>()), Times.Once);

    }
}