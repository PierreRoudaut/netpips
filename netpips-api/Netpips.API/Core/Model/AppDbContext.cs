using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Netpips.API.Download.Model;
using Netpips.API.Identity.Authorization;
using Netpips.API.Identity.Model;
using Netpips.API.Subscriptions.Model;

namespace Netpips.API.Core.Model;

[ExcludeFromCodeCoverage]
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public AppDbContext()
    {
            
    }

    public virtual DbSet<DownloadItem> DownloadItems { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<TvShowSubscription> TvShowSubscriptions { get; set; }
    public virtual DbSet<ShowRssItem> ShowRssItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
         // unique email constraint
         modelBuilder.Entity<User>().HasIndex(e => e.Email).IsUnique();

        //  // unique userId/showRssId constraint
        //  modelBuilder.Entity<TvShowSubscription>().HasIndex(e => new {e.UserId, e.ShowRssId}).IsUnique();
        //
        //
        // // backing fields
        //  modelBuilder.Entity<DownloadItem>().Property(e => e.MovedFiles).HasColumnName("MovedFiles");
        //
        // // string to enum conversions
        //  modelBuilder
        //      .Entity<User>().Property(e => e.Role)
        //      .HasConversion(new EnumToStringConverter<Role>())
        //      .HasDefaultValue(Role.User);
        //  modelBuilder
        //      .Entity<DownloadItem>().Property(e => e.State)
        //      .HasConversion(new EnumToStringConverter<DownloadState>());
        //  modelBuilder
        //      .Entity<DownloadItem>().Property(e => e.Type)
        //      .HasConversion(new EnumToStringConverter<DownloadType>());
        //
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();
    }
}