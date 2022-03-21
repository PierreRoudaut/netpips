using Netpips.API.Core.Model;

namespace Netpips.API;

public static class DbInitializer
{
    public static void Initialize(AppDbContext ctx)
    {
        ctx.Database.EnsureCreated();
    }
}