namespace Netpips.API.Core.CommandLine;

public class CommandLineRequest
{
    public string Command { get; set; }
    public string Arguments { get; set; }
    public TimeSpan? Timeout { get; set; }
}