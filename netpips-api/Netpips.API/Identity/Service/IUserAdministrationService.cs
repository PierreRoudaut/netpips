using Netpips.API.Identity.Authorization;

namespace Netpips.API.Identity.Service;

public interface IUserAdministrationService
{
    bool CanUpdate(Role actor, Role subject, Role subjectNewRole);
    bool CanUpdate(Role actor, Role subject);
}