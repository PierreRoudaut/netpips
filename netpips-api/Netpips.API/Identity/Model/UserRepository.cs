using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netpips.API.Core.Model;
using Netpips.API.Core.Settings;
using Netpips.API.Identity.Authorization;

namespace Netpips.API.Identity.Model;

public class UserRepository : IUserRepository
{
    private readonly ILogger<UserRepository> _logger;
    private readonly AppDbContext _dbContext;
    private readonly NetpipsSettings _settings;

    public UserRepository(ILogger<UserRepository> logger, AppDbContext dbContext, IOptions<NetpipsSettings> options)
    {
        _logger = logger;
        _dbContext = dbContext;
        _settings = options.Value;
    }

    public User GetDaemonUser()
    {
        return _dbContext.Users.First(u => u.Email == _settings.DaemonUserEmail);
    }

    public User FindUser(string email) => _dbContext.Users
        .Include(i => i.TvShowSubscriptions)
        .FirstOrDefault(u => email == u.Email);

    public User FindUser(Guid id) => _dbContext.Users
        .Include(i => i.TvShowSubscriptions)
        .FirstOrDefault(u => id == u.Id);

    public void DeleteUser(User user)
    {
        _dbContext.Users.Remove(user);
        _dbContext.SaveChanges();
    }

    public List<User> GetUsers(Guid userToExclude)
    {
        return _dbContext.Users
            .Where(u => u.Role != Role.SuperAdmin && u.Id != userToExclude)
            .OrderBy(u => u.Email)
            .ToList();
    }

    public bool UpdateUser(User user)
    {
        try
        {
            _dbContext.Entry(user).State = EntityState.Modified;
            _dbContext.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("Failed to update user: " + user.Email);
            _logger.LogError(ex.Message);
            return false;
        }
        return true;
    }

    public bool CreateUser(User user)
    {
        try
        {
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("Failed to add user: " + user.Email);
            _logger.LogError(ex.Message);
            return false;
        }
        return true;
    }

    public bool IsTvShowSubscribedByOtherUsers(int showRssId, Guid id) => 
        _dbContext.Users
            .Where(u => u.Id != id)
            .Any(u => u.TvShowSubscriptions.Any(s => s.ShowRssId == showRssId));

}