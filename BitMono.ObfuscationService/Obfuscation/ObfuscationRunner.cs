using BitMono.Host;
using BitMono.Host.Extensions;
using BitMono.Host.Modules;
using BitMono.Obfuscation.Files;
using BitMono.Obfuscation.Starter;
using BitMono.ObfuscationService.Protections;
using BitMono.Shared.Models;

namespace BitMono.ObfuscationService.Obfuscation;

// Runs BitMono in-process (static AsmResolver analysis — never executes the upload). This whole
// service exists to isolate that work from the website. Protections are chosen per request and
// passed in-memory as ProtectionSettings — no protections.json file.
public sealed class ObfuscationRunner
{
    private static readonly string ConfigDir = Path.Combine(AppContext.BaseDirectory, "BitMonoConfig");

    // Rename pool (the Renamer needs a non-empty set).
    private static readonly string[] RenamerStrings =
    [
        "Initialize", "Awake", "Start", "FixedUpdate", "Reload", "Execute", "Load", "Save",
        "GetPermissions", "HasPermission", "Register", "Invoke", "TryInvoke", "Send", "Read",
        "Close", "Broadcast", "OnEnable", "OnDisable", "GetName", "SetName", "ParseString",
        "ParseBool", "LoadPlugin", "UnloadPlugin", "Translate", "Enqueue", "RunAsync", "Log",
        "LogError", "LogWarning", "GetGroups", "AddGroup", "DeleteGroup", "GetState"
    ];

    public async Task<byte[]> ObfuscateAsync(string fileName, byte[] input, IReadOnlyList<string> protections, CancellationToken ct)
    {
        var outputDir = Path.Combine(Path.GetTempPath(), "bitmono-obf", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDir);
        try
        {
            var module = new BitMonoModule(
                configureContainer: container => container.AddProtections(),
                obfuscationSettings: new ObfuscationSettings
                {
                    ForceObfuscation = true,
                    Tips = false,
                    OutputDirectoryName = outputDir,
                    ReferencesDirectoryName = string.Empty,
                    RandomStrings = RenamerStrings,
                },
                protectionSettings: BuildProtectionSettings(protections),
                criticalsFile: Path.Combine(ConfigDir, "criticals.json"));

            var provider = await new BitMonoApplication().RegisterModule(module).BuildAsync(ct);
            var starter = new BitMonoStarter(provider);

            var info = new CompleteFileInfo(fileName, input, new List<byte[]>(), outputDir);
            if (!await starter.StartAsync(info, ct))
                throw new InvalidOperationException("Obfuscation failed.");

            var outputFile = Directory.EnumerateFiles(outputDir, "*", SearchOption.AllDirectories).First();
            return await File.ReadAllBytesAsync(outputFile, ct);
        }
        finally
        {
            try { Directory.Delete(outputDir, recursive: true); } catch { /* best effort */ }
        }
    }

    // Full catalog in canonical order with only the requested protections enabled.
    private static ProtectionSettings BuildProtectionSettings(IReadOnlyList<string> selected)
    {
        var enabled = new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase);
        return new ProtectionSettings
        {
            Protections = ProtectionCatalog.OrderedNames
                .Select(name => new ProtectionSetting { Name = name, Enabled = enabled.Contains(name) })
                .ToList()
        };
    }
}
