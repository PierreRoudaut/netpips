namespace Netpips.API.Core.Settings;

public class AuthSettings
{
    public string JwtKey { get; set; }
    public string GoogleClientId { get; set; }
    public string JwtIssuer { get; set; }
    public int JwtExpireMinutes { get; set; }

}