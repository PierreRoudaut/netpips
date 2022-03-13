using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netpips.API.Core.Extensions;
using Netpips.API.Identity.Authorization;
using Netpips.API.Identity.Model;
using Netpips.API.Identity.Service;

namespace Netpips.API.Identity.Controller;

[Produces("application/json")]
[Route("api/[controller]")]
[Authorize(Policy = IdentityPolicies.AdminOrHigherPolicy)]
public class UserController : Microsoft.AspNetCore.Mvc.Controller
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserController> _logger;
    private readonly IUserAdministrationService _service;
    public UserController(IUserRepository repository, ILogger<UserController> logger, IUserAdministrationService service)
    {
        _repository = repository;
        _logger = logger;
        _service = service;
    }

    [HttpGet("", Name = "GetUsers")]
    [ProducesResponseType(200)]
    public ObjectResult GetUsers()
    {
        var administrableUsers = _repository.GetUsers(User.GetId());
        return StatusCode(200, administrableUsers);
    }

    [HttpPost("create", Name = "CreateUser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public ObjectResult CreateUser([FromBody] User user)
    {
        var userToCreate = new User
        {
            Role = user.Role,
            Email = user.Email
        };
        if (!_service.CanUpdate(User.GetRole(), userToCreate.Role))
        {
            return StatusCode(403, new { Error = "NotAllowed", Message = "User does not have permission" });
        }
        if (!_repository.CreateUser(userToCreate))
        {
            return StatusCode(400, new { Error = "AddressAlreadyInUse", Message = "Address email already in use" });
        }
        return StatusCode(201, user);
    }

    [HttpPost("update", Name = "UpdateUser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public ObjectResult UpdateUser([FromBody] User user)
    {
        var userToUpdate = _repository.FindUser(user.Id);
        if (userToUpdate == null)
        {
            return StatusCode(404, new { Error = "UserNotFound", Message = "User not found" });
        }
        if (!_service.CanUpdate(User.GetRole(), userToUpdate.Role, subjectNewRole: user.Role))
        {
            return StatusCode(403, new { Error = "NotAllowed", Message = "User does not have permission" });
        }
        userToUpdate.Email = user.Email;
        userToUpdate.Role = user.Role;
        if (!_repository.UpdateUser(userToUpdate))
        {
            return StatusCode(400, new { Error = "AddressAlreadyInUse", Message = "Address email already in use" });
        }
        return StatusCode(200, true);
    }

    [HttpPost("delete", Name = "DeleteUser")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public ObjectResult DeleteUser([FromBody] Guid id)
    {
        var userId = id;
        var userToDelete = _repository.FindUser(userId);
        if (userToDelete == null)
        {
            return StatusCode(404, new { Error = "UserNotFound", Message = "User not found" });
        }

        if (!_service.CanUpdate(User.GetRole(), userToDelete.Role))
        {
            return StatusCode(403, new { Error = "NotAllowed", Message = "User does not have permission" });
        }
        _repository.DeleteUser(userToDelete);
        return StatusCode(200, new { Status = "UserDeleted", Message = "User deleted" });

    }
}