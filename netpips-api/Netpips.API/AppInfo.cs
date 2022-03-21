using Netpips.API.Core;

namespace Netpips.API;

public record AppInfo
{
    public EnvType Env { get; init; }
    public string? Version { get; init; }
}