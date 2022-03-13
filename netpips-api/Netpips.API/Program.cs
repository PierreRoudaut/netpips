using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using Netpips.API;
using Netpips.API.Core;
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

var app = builder.Build();

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