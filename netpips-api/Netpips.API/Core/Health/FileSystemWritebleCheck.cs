using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Netpips.API.Core.Settings;
using ILogger = Serilog.ILogger;

public class FileSystemWritebleCheck: IHealthCheck
{
    private readonly IOptionsMonitor<NetpipsSettings> _options;
    private readonly ILogger _logger;

    public FileSystemWritebleCheck(IOptionsMonitor<NetpipsSettings> options, ILogger logger)
    {
        _options = options;
        _logger = logger.ForContext<FileSystemWritebleCheck>();
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        _logger.Information("Health check");
        
        var result = new Dictionary<string, bool>
        {
            { "downloads", IsDirectoryWritable(_options.CurrentValue.DownloadsPath) },
            { "medialibrary", IsDirectoryWritable(_options.CurrentValue.MediaLibraryPath) },
            { "logs", IsDirectoryWritable(_options.CurrentValue.LogsPath) }
        };
        var unhealthyResult = result.Where(x => !x.Value).Select(x => x.Key).ToArray();
        if (unhealthyResult.Any())
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(string.Join(", ", unhealthyResult)));
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
    
    private bool IsDirectoryWritable(string dirPath)
    {
        try
        {
            if (!Directory.Exists(dirPath))
            {
                return false;
            }

            using var stream = File.Create(Path.Combine(dirPath, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Directory {Directory} is not writeable", dirPath);
            return false;
        }
    }

}