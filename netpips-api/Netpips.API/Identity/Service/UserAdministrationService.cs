using Netpips.API.Identity.Authorization;

namespace Netpips.API.Identity.Service;

public class UserAdministrationService : IUserAdministrationService
{
    public bool CanUpdate(Role actor, Role subject) => subject < actor;

    public bool CanUpdate(Role actor, Role subject, Role subjectNewRole) => CanUpdate(actor, subject) && subjectNewRole < actor;
}