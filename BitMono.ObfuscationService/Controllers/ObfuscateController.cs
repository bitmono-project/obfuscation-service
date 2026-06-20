using BitMono.ObfuscationService.Obfuscation;
using BitMono.ObfuscationService.Protections;
using Microsoft.AspNetCore.Mvc;

namespace BitMono.ObfuscationService.Controllers;

// Internal endpoint — only the web API calls it (across the Aspire network). Synchronous:
// obfuscate and return the bytes. The queue/storage/rate-limiting live in the web API.
[ApiController]
[Route("obfuscate")]
public sealed class ObfuscateController(ObfuscationRunner runner) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Obfuscate(IFormFile file, [FromForm] string[] protections, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest("Empty file.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var selected = (protections ?? [])
            .Where(ProtectionCatalog.IsKnown)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (selected.Length == 0)
            selected = ProtectionCatalog.DefaultSelection;

        var output = await runner.ObfuscateAsync(file.FileName, ms.ToArray(), selected, ct);
        return File(output, "application/octet-stream", file.FileName);
    }
}
