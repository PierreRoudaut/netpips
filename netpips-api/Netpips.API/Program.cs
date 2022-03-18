using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Netpips.API;
using Netpips.API.Core;
using Netpips.API.Core.Model;
using Serilog;
using Serilog.Events;

CultureInfo.CurrentCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();


var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddEnvironmentVariables("NETPIPS");

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console();
if (Netpips.API.Program.App.Env >= EnvType.Dev)
{
    loggerConfig.WriteTo.File(builder.Configuration["LogFolder"]);
}
Log.Logger = loggerConfig.CreateLogger();

builder.Logging.AddSerilog();
builder.Services.AddControllers();  
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var connection = @"Server=127.0.0.1:1433;Database=master;User=SA;Password=Password1;";
builder.Services.AddDbContext<AppDbContext>(optionsAction =>
{
    optionsAction.UseSqlServer(connection);
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
        DbInitializer.Initialize(context);
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

// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

namespace Netpips.API
{
    public static partial class Program
    {
        internal static AppInfo App = new()
        {
            Env = Enum.TryParse<EnvType>(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), out var env) ? env : EnvType.Local,
            Version = Assembly.GetCallingAssembly().GetName().Version?.ToString(3)
        };
    }
}