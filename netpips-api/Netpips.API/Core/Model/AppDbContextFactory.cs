using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Netpips.API.Core.Settings;

namespace Netpips.API.Core.Model;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{

    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var configuration = AppSettingsFactory.BuildConfigurationOld();

        var connectionString = configuration.GetConnectionString("Default");
        // optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}