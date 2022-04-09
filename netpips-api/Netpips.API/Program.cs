using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Coravel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Netpips.API.Core;
using Netpips.API.Core.Extensions;
using Netpips.API.Core.Model;
using Netpips.API.Core.Service;
using Netpips.API.Core.Settings;
using Netpips.API.Download.Authorization;
using Netpips.API.Download.DownloadMethod;
using Netpips.API.Download.DownloadMethod.DirectDownload;
using Netpips.API.Download.DownloadMethod.PeerToPeer;
using Netpips.API.Download.Event;
using Netpips.API.Download.Job;
using Netpips.API.Download.Model;
using Netpips.API.Download.Service;
using Netpips.API.Identity.Model;
using Netpips.API.Identity.Service;
using Netpips.API.Media.Filebot;
using Netpips.API.Media.MediaInfo;
using Netpips.API.Media.Model;
using Netpips.API.Media.Service;
using Netpips.API.Search.Service;
using Netpips.API.Subscriptions.Job;
using Netpips.API.Subscriptions.Model;
using Netpips.API.Subscriptions.Service;
using Serilog;
using Serilog.Events;
using Netpips.API.Search.Service;


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
Log.Logger.Information("Netpips API v{Version} on {Env}", Netpips.API.Program.App.Version,
    Netpips.API.Program.App.Env.ToString("G").ToLower());
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

builder.Services.AddScheduler();
builder.Services.AddEvents();
builder.Services.AddScoped<IDownloadItemService, DownloadItemService>();
builder.Services.AddScoped<IMediaLibraryService, MediaLibraryService>();
builder.Services.AddTransient<IDownloadItemRepository, DownloadItemRepository>();
builder.Services.AddScoped<IDownloadMethod, DirectDownloadMethod>();
builder.Services.AddScoped<IDownloadMethod, P2PDownloadMethod>();
builder.Services.AddTransient<IMediaLibraryMover, MediaLibraryMover>();
builder.Services.AddTransient<IFilebotService, FilebotService>();
builder.Services.AddTransient<IMediaInfoService, MediaInfoService>();

builder.Services.AddTransient<ITorrentDaemonService, TransmissionRemoteDaemonService>();
builder.Services.AddTransient<IAria2CService, Aria2CService>();
builder.Services.AddScoped<IControllerHelperService, ControllerHelperService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IArchiveExtractorService, ArchiveExtractorService>();
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<IMediaItemRepository, MediaItemRepository>();
builder.Services.AddScoped<ISmtpService, GmailSmtpClient>();
builder.Services.AddScoped<IUserAdministrationService, UserAdministrationService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IShowRssGlobalSubscriptionService, ShowRssGlobalSubscriptionService>();

builder.Services.AddScoped<IShowRssItemRepository, ShowRssItemRepository>();
builder.Services.AddScoped<ITvShowSubscriptionRepository, TvShowSubscriptionRepository>();

//events listeners
builder.Services.AddTransient<NotifyUsersItemStarted>();
builder.Services.AddTransient<ProcessDownloadItem>();
builder.Services.AddTransient<SendItemCompletedEmail>();

//jobs
builder.Services.AddScoped<ShowRssFeedConsumerJob>();
builder.Services.AddScoped<ShowRssFeedSyncJob>();
builder.Services.AddScoped<ArchiveDownloadItemsJob>();

//scrappers
builder.Services.AddScoped<ITorrentSearchScrapper, _1337xScrapper>();
builder.Services.AddScoped<ITorrentDetailScrapper, _1337xScrapper>();
builder.Services.AddScoped<ITorrentSearchScrapper, MagnetDlScrapper>();
builder.Services.AddScoped<ITorrentDetailScrapper, MagnetDlScrapper>();

//appSettings
builder.Services.Configure<NetpipsSettings>(builder.Configuration.GetSection("Netpips"));
builder.Services.Configure<ShowRssSettings>(builder.Configuration.GetSection("ShowRss"));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("Auth"));
builder.Services.Configure<GmailMailerAccountSettings>(builder.Configuration.GetSection("GmailMailerAccount"));
builder.Services.Configure<DirectDownloadSettings>(builder.Configuration.GetSection("DirectDownload"));
builder.Services.Configure<TransmissionSettings>(builder.Configuration.GetSection("Transmission"));

//policies
builder.Services.AddSingleton<IAuthorizationHandler, ItemOwnershipAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ArchiveItemAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ItemDownloadingAuthorizationHandler>();


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

// forward headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Serve medialibrary folder as static
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    FileProvider = new PhysicalFileProvider(app.Configuration.GetValue<NetpipsSettings>("Netpips").MediaLibraryPath),
    RequestPath = "/api/file",
    OnPrepareResponse = ctx =>
    {
        Log.Logger.Information("[SERVING] " + ctx.File.Name);
        ctx.Context.Response.Headers.Append("Content-Disposition",
            "attachment; filename=" + ctx.File.Name.RemoveDiacritics().Quoted());
    }
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "/_system/api-docs";
    options.SwaggerEndpoint("/_system/api-docs/v1/swagger.json", "Netpips API v1");
});

// CORS
app.UseCors(builder => builder
    //.WithOrigins(Configuration.GetValue<string>("Netpips:Domain"))
    .WithOrigins("*")
    .AllowAnyMethod()
    .AllowAnyHeader()
);


app.UseAuthentication();
app.UseRequestLocalization(builder => { builder.DefaultRequestCulture = new RequestCulture(CultureInfo.CurrentCulture); });

// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/_system/health", new HealthCheckOptions
{
    ResponseWriter = (context, report) =>
    {
        return context.Response.WriteAsJsonAsync(report,
            new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });
    }
});

// coravel scheduler
// Microsoft.Extensions.Hosting.IHost host = app;
// host.Services.use
app.Services.UseScheduler(
    scheduler =>
    {
        // Start download from synced feed
        // scheduler
        //     .Schedule<ShowRssFeedConsumerJob>()
        //     .EveryThirtyMinutes()
        //     .PreventOverlapping(nameof(ShowRssFeedConsumerJob));

        // Sync items from feed
        // scheduler
        //     .Schedule<ShowRssFeedSyncJob>()
        //     .Hourly()
        //     .PreventOverlapping(nameof(ShowRssFeedSyncJob));

        // Archive passed download items
        // scheduler
        //     .Schedule<ArchiveDownloadItemsJob>()
        //     .DailyAt(16, 0)
        //     .PreventOverlapping(nameof(ArchiveDownloadItemsJob));

        // Download missing subtitles for subscription items
        // scheduler
        //     .Schedule<GetMissingSubtitlesJob>()
        //     .Hourly()
        //     .PreventOverlapping(nameof(GetMissingSubtitlesJob));
    });

// coravel events
var registration = app.Services.ConfigureEvents();
registration
    .Register<ItemStarted>()
    .Subscribe<NotifyUsersItemStarted>();
registration
    .Register<ItemDownloaded>()
    .Subscribe<ProcessDownloadItem>()
    .Subscribe<SendItemCompletedEmail>();

app.Run();

namespace Netpips.API
{
    public static class Program
    {
        internal static readonly AppInfo App = new()
        {
            Env = Enum.TryParse<EnvType>(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), out var env)
                ? env
                : EnvType.Local,
            Version = Assembly.GetCallingAssembly().GetName().Version?.ToString(3)
        };
    }
}