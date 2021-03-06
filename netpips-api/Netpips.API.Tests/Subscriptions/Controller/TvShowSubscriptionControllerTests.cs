using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Netpips.API.Download.Model;
using Netpips.API.Download.Service;
using Netpips.API.Identity.Authorization;
using Netpips.API.Identity.Model;
using Netpips.API.Subscriptions.Controller;
using Netpips.API.Subscriptions.Model;
using Netpips.API.Subscriptions.Service;
using NUnit.Framework;

namespace Netpips.Tests.Subscriptions.Controller;

[TestFixture]
public class TvShowSubscriptionControllerTests
{
    private Mock<ILogger<TvShowController>> _logger;

    private Mock<IShowRssGlobalSubscriptionService> _showRssService;
    private Mock<IUserRepository> _userRepository;
    private Mock<IMemoryCache> _memoryCache;
    private Mock<IDownloadItemService> _downloadItemService;
    private Mock<IDownloadItemRepository> _downloadItemRepository;


    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<TvShowController>>();
        _showRssService = new Mock<IShowRssGlobalSubscriptionService>();
        _memoryCache = new Mock<IMemoryCache>();
        _downloadItemService = new Mock<IDownloadItemService>();
        _downloadItemRepository = new Mock<IDownloadItemRepository>();
        _userRepository = new Mock<IUserRepository>();
    }

    #region Unsubscribe

    [Test]
    public void UnsubscribeTest_Case_Show_Not_Subscribed()
    {
        var user = new User
        {
            Email = "userwithouttvshowsubscriptions@example.com",
            Role = Role.User,
            FamilyName = "",
            GivenName = "",
            Picture = "",
            Id = Guid.NewGuid(),
            TvShowSubscriptions = new List<TvShowSubscription>()
        };

        _userRepository.Setup(c => c.FindUser(It.IsAny<Guid>())).Returns(user);
        var controller = new TvShowController(_logger.Object, _showRssService.Object, _userRepository.Object, _memoryCache.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user.MapToClaimPrincipal() } }
        };

        var res = controller.Unsubscribe(123);
        Assert.AreEqual(400, res.StatusCode);
        Assert.AreEqual("Show not subscribed", res.Value);
    }

    [Test]
    public void UnsubscribeTest_Case_ShowRssUnsubscribeFailed()
    {
        var user = new User
        {
            Email = "userwithouttvshowsubscriptions@example.com",
            Role = Role.User,
            FamilyName = "",
            GivenName = "",
            Picture = "",
            Id = Guid.NewGuid(),
            TvShowSubscriptions =
                new List<TvShowSubscription>
                {
                    new TvShowSubscription
                    {
                        Id = new Guid(),
                        ShowRssId = 123,
                        ShowTitle = "ABC"
                    }
                }
        };

        _userRepository.Setup(c => c.FindUser(It.IsAny<Guid>())).Returns(user);
        _userRepository.Setup(c => c.IsTvShowSubscribedByOtherUsers(It.IsAny<int>(), It.IsAny<Guid>())).Returns(false);

        var emptySummary = new SubscriptionsSummary();
        _showRssService.Setup(c => c.Authenticate(out emptySummary)).Returns(new ShowRssAuthenticationContext());
        _showRssService
            .Setup(c => c.UnsubscribeToShow(It.IsAny<ShowRssAuthenticationContext>(), It.IsAny<int>()))
            .Returns(new ShowRssGlobalSubscriptionService.UnsubscriptionResult { Succeeded = false });

        var controller = new TvShowController(_logger.Object, _showRssService.Object, _userRepository.Object, _memoryCache.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user.MapToClaimPrincipal() } }
        };

        var res = controller.Unsubscribe(123);
        Assert.AreEqual(400, res.StatusCode);
        Assert.AreEqual("Internal error, failed to unsubscribe to show", res.Value);
    }

    [Test]
    public void UnsubscribeTest_Case_Ok()
    {
        var user = new User
        {
            Email = "userwithouttvshowsubscriptions@example.com",
            Role = Role.User,
            FamilyName = "",
            GivenName = "",
            Picture = "",
            Id = Guid.NewGuid(),
            TvShowSubscriptions =
                new List<TvShowSubscription>
                {
                    new TvShowSubscription
                    {
                        Id = new Guid(),
                        ShowRssId = 123,
                        ShowTitle = "ABC"
                    }
                }
        };

        _userRepository.Setup(c => c.FindUser(It.IsAny<Guid>())).Returns(user);
        _userRepository.Setup(c => c.IsTvShowSubscribedByOtherUsers(It.IsAny<int>(), It.IsAny<Guid>())).Returns(false);

        var emptySummary = new SubscriptionsSummary();
        _showRssService.Setup(c => c.Authenticate(out emptySummary)).Returns(new ShowRssAuthenticationContext());
        _showRssService
            .Setup(c => c.UnsubscribeToShow(It.IsAny<ShowRssAuthenticationContext>(), It.IsAny<int>()))
            .Returns(new ShowRssGlobalSubscriptionService.UnsubscriptionResult { Succeeded = true });

        var controller = new TvShowController(_logger.Object, _showRssService.Object, _userRepository.Object, _memoryCache.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user.MapToClaimPrincipal() } }
        };

        var res = controller.Unsubscribe(123);
        Assert.AreEqual(200, res.StatusCode);
        Assert.AreEqual("Unsubscribed", res.Value);
        Assert.AreEqual(0, user.TvShowSubscriptions.Count);
    }
    #endregion

    #region Subscribe

    [Test]
    public void SubscribeTest_Case_Show_Already_Subscribed()
    {
        var user = new User
        {
            Email = "userwithouttvshowsubscriptions@example.com",
            Role = Role.User,
            FamilyName = "",
            GivenName = "",
            Picture = "",
            Id = Guid.NewGuid(),
            TvShowSubscriptions =
                new List<TvShowSubscription>
                {
                    new TvShowSubscription
                    {
                        Id = new Guid(),
                        ShowRssId = 123,
                        ShowTitle = "ABC"
                    }
                }
        };

        _userRepository.Setup(c => c.FindUser(It.IsAny<Guid>())).Returns(user);
        var controller = new TvShowController(_logger.Object, _showRssService.Object, _userRepository.Object, _memoryCache.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user.MapToClaimPrincipal() } }
        };

        var res = controller.Subscribe(123);
        Assert.AreEqual(400, res.StatusCode);
        Assert.AreEqual("Show already subscribed", res.Value);
    }


    [Test]
    public void SubscribeTest_Case_ShowRssSubscribeFailed()
    {
        var user = new User
        {
            Email = "userwithouttvshowsubscriptions@example.com",
            Role = Role.User,
            FamilyName = "",
            GivenName = "",
            Picture = "",
            Id = Guid.NewGuid(),
            TvShowSubscriptions = new List<TvShowSubscription>()
        };

        _userRepository.Setup(c => c.FindUser(It.IsAny<Guid>())).Returns(user);
        _userRepository.Setup(c => c.IsTvShowSubscribedByOtherUsers(It.IsAny<int>(), It.IsAny<Guid>())).Returns(false);

        var emptySummary = new SubscriptionsSummary { SubscribedShows = new List<TvShowRss>() };
        _showRssService.Setup(c => c.Authenticate(out emptySummary)).Returns(new ShowRssAuthenticationContext());
        _showRssService
            .Setup(c => c.SubscribeToShow(It.IsAny<ShowRssAuthenticationContext>(), It.IsAny<int>()))
            .Returns(new ShowRssGlobalSubscriptionService.SubscriptionResult { Succeeded = false });

        var controller = new TvShowController(_logger.Object, _showRssService.Object, _userRepository.Object, _memoryCache.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user.MapToClaimPrincipal() } }
        };

        var res = controller.Subscribe(123);
        Assert.AreEqual(400, res.StatusCode);
        Assert.AreEqual("Internal error, failed to subscribe to show", res.Value);
    }

    [Test]
    public void SubscribeTest_Case_Ok()
    {
        var user = new User
        {
            Email = "userwithouttvshowsubscriptions@example.com",
            Role = Role.User,
            FamilyName = "",
            GivenName = "",
            Picture = "",
            Id = Guid.NewGuid(),
            TvShowSubscriptions = new List<TvShowSubscription>()
        };

        _userRepository.Setup(c => c.FindUser(It.IsAny<Guid>())).Returns(user);
        _userRepository.Setup(c => c.IsTvShowSubscribedByOtherUsers(It.IsAny<int>(), It.IsAny<Guid>())).Returns(false);

        var emptySummary = new SubscriptionsSummary { SubscribedShows = new List<TvShowRss>() };
        _showRssService.Setup(c => c.Authenticate(out emptySummary)).Returns(new ShowRssAuthenticationContext());
        _showRssService
            .Setup(c => c.SubscribeToShow(It.IsAny<ShowRssAuthenticationContext>(), It.IsAny<int>()))
            .Returns(new ShowRssGlobalSubscriptionService.SubscriptionResult { Succeeded = true, Summary = new SubscriptionsSummary { SubscribedShows = new List<TvShowRss> { new TvShowRss { ShowRssId = 123 } } } });

        var controller = new TvShowController(_logger.Object, _showRssService.Object, _userRepository.Object, _memoryCache.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user.MapToClaimPrincipal() } }
        };

        var res = controller.Subscribe(123);
        Assert.AreEqual(200, res.StatusCode);
        Assert.AreEqual("Subscribed", res.Value);
        Assert.AreEqual(1, user.TvShowSubscriptions.Count);
        Assert.AreEqual(123, user.TvShowSubscriptions.First().ShowRssId);
    }

    #endregion

    //[Test]
    //public void GetTvMazeIdTest()
    //{
    //    var user = new User
    //    {
    //        Email = "userwithouttvshowsubscriptions@example.com",
    //        Role = Role.User,
    //        FamilyName = "",
    //        GivenName = "",
    //        Picture = "",
    //        Id = Guid.NewGuid(),
    //        TvShowSubscriptions = new List<TvShowSubscription>()
    //    };

    //    var controller = new TvShowController(this.logger.Object, this.showRssService.Object, this.userRepository.Object, this.memoryCache.Object)
    //    {
    //        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user.MapToClaimPrincipal() } }
    //    };

    //    var res = controller.GetTvMazeId(150);
    //    Assert.AreEqual(63, res.Value);

    //}

}