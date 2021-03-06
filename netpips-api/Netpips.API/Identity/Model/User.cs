using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Google.Apis.Auth;
using Netpips.API.Download.Model;
using Netpips.API.Identity.Authorization;
using Netpips.API.Identity.Service;
using Netpips.API.Subscriptions.Model;

namespace Netpips.API.Identity.Model;

public class User
{
    [Key]
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Picture { get; set; }
    public Role Role { get; set; }

    public bool ManualDownloadEmailNotificationEnabled { get; set; }
    public bool TvShowSubscriptionEmailNotificationEnabled { get; set; }


    // nav property
    public virtual List<DownloadItem> DownloadItems { get; set; }
    public virtual List<TvShowSubscription> TvShowSubscriptions { get; set; }


    public void UpdateInfos(GoogleJsonWebSignature.Payload payload)
    {
        GivenName = payload.GivenName;
        FamilyName = payload.FamilyName;
        Picture = payload.Picture;
    }

    public ClaimsPrincipal MapToClaimPrincipal() => new(
        new ClaimsIdentity(
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Id.ToString()), 
                new Claim(JwtRegisteredClaimNames.Email, Email),
                new Claim(JwtRegisteredClaimNames.FamilyName, FamilyName),
                new Claim(JwtRegisteredClaimNames.GivenName, GivenName),
                new Claim(AppClaims.Picture, Picture),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, Role.ToString())
            }
        )
    );
}