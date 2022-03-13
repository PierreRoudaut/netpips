using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Netpips.API.Download.Model;
using Netpips.API.Identity.Authorization;

namespace Netpips.API.Core.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool Owns(this ClaimsPrincipal user, DownloadItem item) =>
        user.GetId() == item.OwnerId;

    public static Role GetRole(this ClaimsPrincipal user) =>
        user.Claims.First(c => c.Type == ClaimsIdentity.DefaultRoleClaimType).Value.ToEnum<Role>();

    public static Guid GetId(this ClaimsPrincipal user) =>
        new(user.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);

    public static bool IsInRoles(this ClaimsPrincipal user, params Role[] roles) =>
        roles.Contains(user.GetRole());
}