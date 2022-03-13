using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Netpips.API.Core;
using Netpips.API.Core.Settings;
using Netpips.API.Identity.Model;

namespace Netpips.API.Identity.Service;

public enum AuthError
{
    InvalidToken,
    WrongAudience,
    WrongIssuer,
    UnregisteredUser,
    EmailNotVerified,
    TokenExpired
}

public class AuthErrorResponse
{
    public string Message { get; set; }
    public AuthError Error { get; set; }
    public int HttpCode { get; set; }
}

public static class AppClaims
{
    public const string Picture = "picture";
}

public class AuthService : IAuthService
{
    private readonly AuthSettings _settings;

    private readonly ILogger<AuthService> _logger;
    public static readonly string[] Issuers = { "accounts.google.com", "https://accounts.google.com" };

    private readonly IGoogleAuthService _googleAuthService;

    public AuthService(IOptions<AuthSettings> options, ILogger<AuthService> logger, IGoogleAuthService googleAuthService)
    {
        _settings = options.Value;
        _logger = logger;
        _googleAuthService = googleAuthService;
    }

    public bool ValidateGoogleIdToken(string idToken, out GoogleJsonWebSignature.Payload payload, out AuthError error)
    {
        error = AuthError.InvalidToken;
        AuthError? err = null;
        payload = null;
        try
        {
            payload = _googleAuthService.ValidateAsync(idToken).Result;
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to validate GoogleToken");
            _logger.LogError(e.Message);
            err = AuthError.InvalidToken;
            return false;
        }
        if (!payload.EmailVerified)
        {
            err = AuthError.EmailNotVerified;
        }
        // else if (payload.Audience.ToString() != this.settings.GoogleClientId)
        // {
        //     err = AuthError.WrongAudience;
        // }
        else if (payload.ExpirationTimeSeconds != null && ExtensionMethods.ConvertFromUnixTimestamp(payload.ExpirationTimeSeconds.Value) <= DateTime.Now)
        {
            err = AuthError.TokenExpired;
        }
        else if (!Issuers.Contains(payload.Issuer))
        {
            err = AuthError.WrongIssuer;
        }
        if (err.HasValue)
        {
            error = err.Value;
            _logger.LogWarning("VerifyIdToken: KO ({0})", err);
            return false;
        }
        _logger.LogInformation("VerifyIdToken: OK ({0})", payload.Email);
        return true;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.GivenName, user.GivenName),
            new(JwtRegisteredClaimNames.FamilyName, user.FamilyName),
            new(AppClaims.Picture, user.Picture),
            new(ClaimsIdentity.DefaultRoleClaimType, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddMinutes(Convert.ToDouble(_settings.JwtExpireMinutes));

        var token = new JwtSecurityToken(
            issuer: _settings.JwtIssuer,
            audience: _settings.JwtIssuer,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}