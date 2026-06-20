namespace BitMono.ObfuscationService.Models;

public sealed record ProtectionInfo(
    string Name, string Description, string Category, bool Stable, string? Note, string? MinLevel);

public sealed record VersionResponse(string BitMono, uint Packed);
