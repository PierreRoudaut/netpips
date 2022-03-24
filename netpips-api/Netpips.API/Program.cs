using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddTypeActivatedCheck<CommandlineDependencyCheck>("filebot", "filebot", "-version")
    .AddTypeActivatedCheck<CommandlineDependencyCheck>("aria2c", "aria2c", "--version")
    .AddTypeActivatedCheck<CommandlineDependencyCheck>("mediainfo", "mediainfo", "--version")
    .AddTypeActivatedCheck<CommandlineDependencyCheck>("transmission", "transmission-remote", "--version"); 

builder.Logging.AddSerilog();
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


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// AppAsserter.AssertCliDependencies();
// AppAsserter.AssertSettings(netpipsAppSettings);

// SetupLogger(netpipsAppSettings.LogsPath);

// removes default claim mapping
// AppAsserter.AssertCliDependencies();

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

public class CommandlineDependencyCheck : IHealthCheck
{
    private readonly string _command;
    private readonly string _arguments;

    public CommandlineDependencyCheck(string command, string arguments)
    {
        _command = command;
        _arguments = arguments;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        try
        {
            var code = OsHelper.ExecuteCommand(_command, _arguments, out var output, out var error);
            if (code != 0 && code != 255)
            {
                var desc = $@"[{_command}] code:[" + code + "]   out:[" + output + "]  error:[" + error + "]";
                return Task.FromResult(HealthCheckResult.Unhealthy(desc));
            }
            return Task.FromResult(HealthCheckResult.Healthy());

        }
        catch (Exception e)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy());
        }
    }
}

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