using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Coravel.Events.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Netpips.API.Core.Settings;
using Netpips.API.Download.Controller;
using Netpips.API.Download.DownloadMethod;
using Netpips.API.Download.Exception;
using Netpips.API.Download.Model;
using Netpips.API.Download.Service;
using Netpips.API.Identity.Authorization;
using Netpips.API.Identity.Model;
using Netpips.API.Identity.Service;
using Netpips.Tests.Core;
using NUnit.Framework;

namespace Netpips.Tests.Download.Service;

[TestFixture]
public class DownloadItemServiceTest
{
    private Mock<IDispatcher> _dispatcher;
    private Mock<ILogger<DownloadItemService>> _logger;
    private Mock<IOptions<NetpipsSettings>> _options;
    private Mock<IDownloadItemRepository> _repository;
    private Mock<IDownloadMethod> _downloadMethod;

    public static User ItemOwner = new User { Email = "owner@example.com" };
    public static User NotAnOwner = new User { Email = "notanowner@example.com" };

    private readonly HttpContext _ownerHttpContext =
        new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] {
                        new Claim(JwtRegisteredClaimNames.Email, ItemOwner.Email),
                        new Claim(JwtRegisteredClaimNames.FamilyName, ""),
                        new Claim(JwtRegisteredClaimNames.GivenName, "") ,
                        new Claim(AppClaims.Picture, ""),
                        new Claim(ClaimsIdentity.DefaultRoleClaimType, Role.User.ToString()),
                    })
            )
        };

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<DownloadItemService>>();
        _dispatcher = new Mock<IDispatcher>();
        _options = new Mock<IOptions<NetpipsSettings>>();
        _options.Setup(x => x.Value).Returns(TestHelper.CreateNetpipsAppSettings());
        _repository = new Mock<IDownloadItemRepository>();
        _downloadMethod = new Mock<IDownloadMethod>();
    }

    [Test]
    public void StartDownloadTestCaseUrlNotHandled()
    {
        _downloadMethod.Setup(x => x.CanHandle(It.IsAny<string>())).Returns(false);
        var services = new ServiceCollection();
        services.AddScoped(typeof(IDownloadMethod), delegate { return _downloadMethod.Object; });

        var serviceProvider = services.BuildServiceProvider();

        var service = new DownloadItemService(_logger.Object, serviceProvider, _options.Object, _dispatcher.Object, _repository.Object);

        Assert.IsFalse(service.StartDownload(new DownloadItem { FileUrl = "not-handled-url.com/123" }, out var err));
        Assert.AreEqual(err, DownloadItemActionError.UrlNotHandled);
    }


    [Test]
    public void StartDownloadTestCaseFileNotDownaloadable()
    {
        _downloadMethod.Setup(x => x.CanHandle(It.IsAny<string>())).Returns(true);
        _downloadMethod
            .Setup(m => m.Start(It.IsAny<DownloadItem>()))
            .Throws(new FileNotDownloadableException("File not downloadable"));

        var services = new ServiceCollection();
        services.AddScoped(typeof(IDownloadMethod), delegate { return _downloadMethod.Object; });
        var serviceProvider = services.BuildServiceProvider();

        var service = new DownloadItemService(_logger.Object, serviceProvider, _options.Object, _dispatcher.Object, _repository.Object);
        var result = service.StartDownload(new DownloadItem { FileUrl = "handled-url.com/notAFile" }, out var err);

        Assert.IsFalse(result);
        Assert.AreEqual(err, DownloadItemActionError.DownloadabilityFailure);
    }

    [Test]
    public void StartDownloadTestCaseStartDownloadFailure()
    {
        _downloadMethod.Setup(x => x.CanHandle(It.IsAny<string>())).Returns(true);
        _downloadMethod
            .Setup(m => m.Start(It.IsAny<DownloadItem>()))
            .Throws(new StartDownloadException("Failed to start download"));

        var services = new ServiceCollection();
        services.AddScoped(typeof(IDownloadMethod), delegate { return _downloadMethod.Object; });
        var serviceProvider = services.BuildServiceProvider();

        var service = new DownloadItemService(_logger.Object, serviceProvider, _options.Object, _dispatcher.Object, _repository.Object);
        var result = service.StartDownload(new DownloadItem { FileUrl = "handled-url.com" }, out var err);

        Assert.IsFalse(result);
        Assert.AreEqual(err, DownloadItemActionError.StartDownloadFailure);
    }


    [Test]
    public void StartDownloadTestCaseOk()
    {
        _downloadMethod.Setup(x => x.CanHandle(It.IsAny<string>())).Returns(true);

        var services = new ServiceCollection();
        services.AddScoped(typeof(IDownloadMethod), delegate { return _downloadMethod.Object; });
        var serviceProvider = services.BuildServiceProvider();

        var service = new DownloadItemService(_logger.Object, serviceProvider, _options.Object, _dispatcher.Object, _repository.Object);
        var result = service.StartDownload(new DownloadItem { FileUrl = "handled-url.com" }, out _);
        Assert.True(result);
        _downloadMethod.Verify(x => x.Start(It.IsAny<DownloadItem>()), Times.Once);
    }

    [Test]
    public void CancelDownload()
    {
        _downloadMethod.Setup(x => x.CanHandle(It.IsAny<DownloadType>())).Returns(true);

        var services = new ServiceCollection();
        services.AddScoped(typeof(IDownloadMethod), delegate { return _downloadMethod.Object; });
        var serviceProvider = services.BuildServiceProvider();

        var service = new DownloadItemService(_logger.Object, serviceProvider, _options.Object, _dispatcher.Object, _repository.Object);

        var item = new DownloadItem();
        service.CancelDownload(item);

        _downloadMethod.Verify(x => x.Cancel(item), Times.Once);
    }

    [Test]
    public void ArchiveDownload()
    {
        _downloadMethod.Setup(x => x.CanHandle(It.IsAny<DownloadType>())).Returns(true);

        var services = new ServiceCollection();
        services.AddScoped(typeof(IDownloadMethod), delegate { return _downloadMethod.Object; });
        var serviceProvider = services.BuildServiceProvider();

        var service = new DownloadItemService(_logger.Object, serviceProvider, _options.Object, _dispatcher.Object, _repository.Object);

        var item = new DownloadItem { Token = "ABCD" };
        service.ArchiveDownload(item);

        _downloadMethod.Verify(x => x.Archive(item), Times.Once);
    }
}