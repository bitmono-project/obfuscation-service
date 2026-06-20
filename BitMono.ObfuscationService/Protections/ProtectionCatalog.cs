using BitMono.ObfuscationService.Models;

namespace BitMono.ObfuscationService.Protections;

// The protections this service exposes, grounded in BitMono's docs + source
// (docs/source/protections). Order matches the shipped protections.json — the engine builds its
// pipeline from the full set and we only flip Enabled per request.
//
// Strength ladder (cumulative, ConfuserEx-style): each protection has a MinLevel; picking a level
// enables every protection whose MinLevel is at or below it. Mono-only/packer protections have NO
// level (null) — they corrupt non-Mono PEs, so they're opt-in via the checklist, never via a level.
public static class ProtectionCatalog
{
    public static readonly string[] Levels = ["Minimum", "Normal", "Aggressive", "Maximum"];
    public const string DefaultLevel = "Normal";

    public static readonly ProtectionInfo[] All =
    [
        new("AntiILdasm", "Adds SuppressIldasmAttribute so ildasm refuses to disassemble the assembly.", "Anti-tooling", true, null, "Minimum"),
        new("AntiDe4dot", "Injects fake obfuscator markers to throw off the de4dot deobfuscator.", "Anti-tooling", false, "Weak — known bypasses exist.", "Normal"),
        new("ObjectReturnType", "Changes bool return types to System.Object to break static type analysis.", "Calls & types", true, "Non-virtual, non-async methods only.", "Aggressive"),
        new("NoNamespaces", "Strips all namespaces, flattening the type hierarchy.", "Renaming", true, null, "Minimum"),
        new("FullRenamer", "Renames types, methods and fields to unreadable names (handles WPF BAML).", "Renaming", true, "Reflection or string-keyed serialization can break; common [Serializable]/Json/Xml/Unity members are auto-kept.", "Minimum"),
        new("AntiDebugBreakpoints", "Timing checks that abort the app if a method runs over 5s (debugger detection).", "Anti-debug", true, "Skips constructors and properties.", "Aggressive"),
        new("BillionNops", "Adds a huge nop-filled method to crash decompilers.", "Anti-tooling", true, "Inflates file size.", "Maximum"),
        new("StringsEncryption", "Encrypts string literals (AES), decrypted at runtime.", "Strings", true, "Adds a runtime decryption cost.", "Normal"),
        new("UnmanagedString", "Moves strings into native code that rebuilds them at runtime.", "Strings", true, "Windows .NET only (not Mono); Windows-1252 strings.", "Maximum"),
        new("DotNetHook", "Redirects calls through dummy stubs hooked via JIT patching.", "Calls & types", true, "JIT only — not Mono or IL2CPP.", "Maximum"),
        new("CallToCalli", "Rewrites call into calli with function pointers resolved at runtime.", "Calls & types", true, "Skips generics and in/out params.", "Aggressive"),
        new("AntiDecompiler", "Sets odd <Module> type attributes to crash dnSpy's analyzer.", "Anti-tooling", false, "Mono only — breaks other runtimes; patched in new dnSpy.", null),
        new("BitMethodDotnet", "Inserts dead IL prefix opcodes to break decompilers.", "Calls & types", false, "Mono / old .NET Framework only.", null),
        new("BitDecompiler", "Corrupts CLR metadata headers so decompilers can't parse the PE.", "Packers (PE)", false, "Mono only — breaks other runtimes. Runs last.", null),
        new("BitTimeDateStamp", "Zeroes the PE timestamp to hide the build date.", "Packers (PE)", true, "Packer — runs last.", null),
        new("BitDotNet", "Replaces the PE signature with an invalid byte to fool decompilers.", "Packers (PE)", true, "Mono only — breaks other runtimes. Runs last.", null),
        new("BitMono", "Zeroes the .NET data directory so PE parsers can't recognise the file.", "Packers (PE)", true, "Mono only — breaks other runtimes. Runs last.", null),
    ];

    // Engine pipeline order (= protections.json order); we flip Enabled per request.
    public static readonly string[] OrderedNames = All.Select(p => p.Name).ToArray();

    // Cumulative set for a level: names whose MinLevel is at or below the given level.
    public static string[] ForLevel(string level)
    {
        var ceiling = Array.IndexOf(Levels, level);
        if (ceiling < 0)
        {
            return [];
        }
        return All.Where(p => p.MinLevel is not null && Array.IndexOf(Levels, p.MinLevel) <= ceiling)
                  .Select(p => p.Name)
                  .ToArray();
    }

    // Used when a request selects nothing — the default level.
    public static readonly string[] DefaultSelection = ForLevel(DefaultLevel);

    public static bool IsKnown(string name) =>
        OrderedNames.Contains(name, StringComparer.OrdinalIgnoreCase);
}
