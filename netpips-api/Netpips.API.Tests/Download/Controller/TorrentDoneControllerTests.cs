using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Coravel.Events.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Netpips.API.Core.Service;
using Netpips.API.Download.Authorization;
using Netpips.API.Download.Controller;
using Netpips.API.Download.DownloadMethod.PeerToPeer;
using Netpips.API.Download.Model;
using NUnit.Framework;

namespace Netpips.Tests.Download.Controller;

[TestFixture]
public class TorrentDoneControllerTest
{
    private Mock<ILogger<TorrentDoneController>> _logger;
    private Mock<IControllerHelperService> _helper;
    private Mock<IAuthorizationService> _authorizationService;
    private Mock<ITorrentDaemonService> _torrentService;
    private Mock<IDownloadItemRepository> _repository;
    private Mock<IDispatcher> _dispatcher;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<TorrentDoneController>>();
        _authorizationService = new Mock<IAuthorizationService>();
        _torrentService = new Mock<ITorrentDaemonService>();
        _helper = new Mock<IControllerHelperService>();
        _repository = new Mock<IDownloadItemRepository>();
        _dispatcher = new Mock<IDispatcher>();
    }

    [Test]
    public void TorrentDoneTestCaseNotLocallCall()
    {
        _helper.Setup(x => x.IsLocalCall(It.IsAny<HttpContext>())).Returns(false);

        var controller = new TorrentDoneController(_logger.Object, _repository.Object, _authorizationService.Object,
            _torrentService.Object, _dispatcher.Object);

        var response = controller.TorrentDone(_helper.Object, "ABCD");
        Assert.AreEqual(403, response.StatusCode);
    }

    [Test]
    public void TorrentDoneTestCaseNotFound()
    {
        _helper.Setup(x => x.IsLocalCall(It.IsAny<HttpContext>())).Returns(true);

        _repository.Setup(x => x.FindAllUnarchived()).Returns(new List<DownloadItem>());

        var controller = new TorrentDoneController(_logger.Object, _repository.Object, _authorizationService.Object,
            _torrentService.Object, _dispatcher.Object);

        var response = controller.TorrentDone(_helper.Object, "ABCD");

        Assert.AreEqual(404, response.StatusCode);
    }

    [Test]
    public void TorrentDoneTestCaseOk()
    {
        var item = new DownloadItem { Type = DownloadType.Ddl, State = DownloadState.Downloading, Hash = "ABCD" };
        _helper.Setup(x => x.IsLocalCall(It.IsAny<HttpContext>())).Returns(true);

        _repository.Setup(x => x.FindAllUnarchived()).Returns(new List<DownloadItem> {item});

        _authorizationService
            .Setup(
                x => x.AuthorizeAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<DownloadItem>(),
                    DownloadItemPolicies.TorrentDonePolicy))
            .Returns(Task.FromResult(AuthorizationResult.Success()));

        var controller = new TorrentDoneController(_logger.Object, _repository.Object, _authorizationService.Object,
            _torrentService.Object, _dispatcher.Object);

        var response = controller.TorrentDone(_helper.Object, "ABCD");

        Assert.AreEqual(200, response.StatusCode);
    }
}