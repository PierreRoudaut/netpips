namespace Netpips.API.Core.Service;

public interface IControllerHelperService
{
    bool IsLocalCall(HttpContext context);
}