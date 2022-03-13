using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Netpips.API.Core;
using Netpips.API.Core.Settings;
using Netpips.API.Identity.Service;
using Netpips.Tests.Core;
using NUnit.Framework;

namespace Netpips.Tests.Identity.Service;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IOptions<AuthSettings>> _options;
    private Mock<ILogger<AuthService>> _logger;
    private Mock<IGoogleAuthService> _googleAuthService;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<AuthService>>();
        _options = new Mock<IOptions<AuthSettings>>();
        _googleAuthService = new Mock<IGoogleAuthService>();
    }

    [Test]
    public void ValidateGoogleIdTokenTest_CaseInvalidToken()
    {
        // invalid token
        _options.SetupGet(x => x.Value).Returns(TestHelper.CreateAuthSettings());
        _googleAuthService.Setup(x => x.ValidateAsync(It.IsAny<string>())).Throws<Exception>();
        var service = new AuthService(_options.Object, _logger.Object, _googleAuthService.Object);
        Assert.AreEqual(false, service.ValidateGoogleIdToken("abcd", out _, out var err), "google id Token should be invalid");
        Assert.AreEqual(AuthError.InvalidToken, err);

    }

    [Test]
    public void ValidateGoogleIdTokenTest_CaseEmailNotVerified()
    {
        // email not verified
        var expectedPayload = new GoogleJsonWebSignature.Payload { EmailVerified = false };
        _options.SetupGet(x => x.Value).Returns(TestHelper.CreateAuthSettings());
        _googleAuthService.Setup(x => x.ValidateAsync(It.IsAny<string>()))
            .Returns(Task.FromResult(expectedPayload));
        var service = new AuthService(_options.Object, _logger.Object, _googleAuthService.Object);
        Assert.IsFalse(service.ValidateGoogleIdToken(It.IsAny<string>(), out var payload, out var err));
        Assert.IsFalse(payload.EmailVerified);
        Assert.AreEqual(AuthError.EmailNotVerified, err);
    }


    [Test]
    [Ignore("Audience validation to be debugged")]
    public void ValidateGoogleIdTokenTest_CaseWrongAudience()
    {
        // wrong audience

        _options.SetupGet(x => x.Value).Returns(TestHelper.CreateAuthSettings());

        var expectedPayload = new GoogleJsonWebSignature.Payload { Audience = "abcd", EmailVerified = true };
        _googleAuthService.Setup(x => x.ValidateAsync(It.IsAny<string>()))
            .Returns(Task.FromResult(expectedPayload));

        var service = new AuthService(_options.Object, _logger.Object, _googleAuthService.Object);
        Assert.IsFalse(service.ValidateGoogleIdToken(It.IsAny<string>(), out var payload, out var err));
        Assert.AreEqual(AuthError.WrongAudience, err);
    }

    [Test]
    public void ValidateGoogleIdTokenTest_CaseTokenExpired()
    {
        // wrong audience
        _options.SetupGet(x => x.Value).Returns(TestHelper.CreateAuthSettings());

        var expectedPayload = new GoogleJsonWebSignature.Payload { ExpirationTimeSeconds = ExtensionMethods.ConvertToUnixTimestamp(DateTime.Now.Subtract(TimeSpan.FromHours(3))), EmailVerified = true, Audience = _options.Object.Value.GoogleClientId };

        _googleAuthService.Setup(x => x.ValidateAsync(It.IsAny<string>()))
            .Returns(Task.FromResult(expectedPayload));


        var service = new AuthService(_options.Object, _logger.Object, _googleAuthService.Object);
        Assert.IsFalse(service.ValidateGoogleIdToken(It.IsAny<string>(), out var payload, out var err));
        Assert.AreEqual(AuthError.TokenExpired, err);
    }

    [Test]
    public void ValidateGoogleIdTokenTest_CaseWrongIssuer()
    {
        // wrong audience
        _options.SetupGet(x => x.Value).Returns(TestHelper.CreateAuthSettings());

        var expectedPayload = new GoogleJsonWebSignature.Payload { Issuer = "abcd", EmailVerified = true, ExpirationTimeSeconds = ExtensionMethods.ConvertToUnixTimestamp(DateTime.Now.AddHours(1)), Audience = _options.Object.Value.GoogleClientId };

        _googleAuthService.Setup(x => x.ValidateAsync(It.IsAny<string>()))
            .Returns(Task.FromResult(expectedPayload));

        var service = new AuthService(_options.Object, _logger.Object, _googleAuthService.Object);
        Assert.IsFalse(service.ValidateGoogleIdToken(It.IsAny<string>(), out var payload, out var err));
        Assert.AreEqual(AuthError.WrongIssuer, err);
    }

    [Test]
    public void ValidateGoogleIdTokenTest_CaseValid()
    {
        // wrong audience
        _options.SetupGet(x => x.Value).Returns(TestHelper.CreateAuthSettings());

        var expectedPayload = new GoogleJsonWebSignature.Payload { Issuer = AuthService.Issuers.First(), EmailVerified = true, ExpirationTimeSeconds = ExtensionMethods.ConvertToUnixTimestamp(DateTime.Now.AddHours(1)), Audience = _options.Object.Value.GoogleClientId };
        _googleAuthService.Setup(x => x.ValidateAsync(It.IsAny<string>()))
            .Returns(Task.FromResult(expectedPayload));
        var service = new AuthService(_options.Object, _logger.Object, _googleAuthService.Object);
        Assert.IsTrue(service.ValidateGoogleIdToken(It.IsAny<string>(), out var payload, out var err));
    }

    [Test]
    public void GenerateAccessTokenTest()
    {
        _options.SetupGet(x => x.Value).Returns(TestHelper.CreateAuthSettings());
        var service = new AuthService(_options.Object, _logger.Object, _googleAuthService.Object);
        var token = service.GenerateAccessToken(TestHelper.Admin);
        Assert.NotNull(token);
    }
}