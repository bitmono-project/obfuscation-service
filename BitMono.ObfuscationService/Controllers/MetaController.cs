using System.Reflection;
using BitMono.ObfuscationService.Models;
using BitMono.ObfuscationService.Protections;
using Microsoft.AspNetCore.Mvc;

namespace BitMono.ObfuscationService.Controllers;

[ApiController]
public sealed class MetaController : ControllerBase
{
    [HttpGet("version")]
    public VersionResponse Version() => new(BitMonoVersion.Current, BitMonoVersion.CurrentPacked);

    [HttpGet("protections")]
    public ProtectionInfo[] Protections() => ProtectionCatalog.All;
}

internal static class BitMonoVersion
{
    // The BitMono engine version this service is built against (its packaged version).
    public static string Current { get; } = Resolve();

    // Packed for cheap "older/current/newer" compares; mirrors Safeturned's VersionPackingHelper.
    public static uint CurrentPacked { get; } = Pack(Current);

    private static string Resolve()
    {
        var assembly = typeof(BitMono.Obfuscation.Starter.BitMonoStarter).Assembly;
        var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                   ?? assembly.GetName().Version?.ToString()
                   ?? "unknown";
        var plus = info.IndexOf('+'); // drop +<commit> build metadata
        return plus > 0 ? info[..plus] : info;
    }

    // (major << 16) | (minor << 8) | patch
    public static uint Pack(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return 0;
        }
        var parts = version.Split('.');
        var major = parts.Length > 0 && uint.TryParse(parts[0], out var ma) ? ma : 0;
        var minor = parts.Length > 1 && uint.TryParse(parts[1], out var mi) ? mi : 0;
        var patch = parts.Length > 2 && uint.TryParse(parts[2], out var pa) ? pa : 0;
        return (major << 16) | (minor << 8) | patch;
    }
}
