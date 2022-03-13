using Google.Apis.Auth;
using Netpips.API.Identity.Model;

namespace Netpips.API.Identity.Service;

public interface IAuthService
{
    bool ValidateGoogleIdToken(string idToken, out GoogleJsonWebSignature.Payload payload, out AuthError err);
    string GenerateAccessToken(User user);
}