using Microsoft.Extensions.Diagnostics.HealthChecks;
using Netpips.API.Core;

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