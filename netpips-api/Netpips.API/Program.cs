using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Netpips.API.Core;
using Netpips.API.Core.Model;
using Netpips.API.Core.Settings;
using Serilog;
using Serilog.Events;


CultureInfo.CurrentCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();


var builder = WebApplication.CreateBuilder(args);

builder
    .Configuration
    .AddEnvironmentVariables("NETPIPS");

// var netpipsSettings = builder.Configuration["Netpips"];
// AppAsserter.AssertSettings(netpipsSettings);

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console();

if (Netpips.API.Program.App.Env >= EnvType.Dev)
{
    loggerConfig.WriteTo.File(builder.Configuration.Get<NetpipsSettings>().LogsPath);
}
Log.Logger = loggerConfig.CreateLogger();
Log.Logger.Information("Netpips API v{Version} on {Env}", Netpips.API.Program.App.Version, Netpips.API.Program.App.Env.ToString("G").ToLower());
builder.Host.UseSerilog(Log.Logger);
builder.Services.Configure<NetpipsSettings>(builder.Configuration.GetSection("Netpips"));
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddCheck<FileSystemWritebleCheck>("filesystem_writeable")
    .AddTypeActivatedCheck<CommandlineDependencyCheck>("filebot", "filebot", "-version")
    .AddTypeActivatedCheck<CommandlineDependencyCheck>("aria2c", "aria2c", "--version")
    .AddTypeActivatedCheck<CommandlineDependencyCheck>("mediainfo", "mediainfo", "--version")
    .AddTypeActivatedCheck<CommandlineDependencyCheck>("transmission", "transmission-remote", "--version");

builder.Services.AddMemoryCache();
builder.Services.AddOptions();
builder.Services.AddControllers();  
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>((sp, optionsAction) =>
{
    optionsAction.UseSqlServer(sp.GetRequiredService<IConfiguration>()["SqlServer:ConnectionString"]);
    optionsAction.EnableSensitiveDataLogging();
});

var app = builder.Build();


CreateDbIfNotExists(app);

static void CreateDbIfNotExists(IHost host)
{
    using var scope = host.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}


app.UseSwagger();
app.UseSwaggerUI();
// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/_system/health", new HealthCheckOptions
{
    ResponseWriter = (context, report) =>
    {
        return context.Response.WriteAsJsonAsync(report, new JsonSerializerOptions {WriteIndented = true, Converters = { new JsonStringEnumConverter() }});
    }
});
app.Run();

namespace Netpips.API
{
    public static class Program
    {
        internal static readonly AppInfo App = new()
        {
            Env = Enum.TryParse<EnvType>(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), out var env) ? env : EnvType.Local,
            Version = Assembly.GetCallingAssembly().GetName().Version?.ToString(3)
        };
    }
}