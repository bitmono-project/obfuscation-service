using System.Text.Json;
using System.Text.Json.Serialization;
using BitMono.ObfuscationService.Obfuscation;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// The web API forwards the assembled assembly + its dependencies + an optional signing key in one
// internal multipart POST. Lift the body cap well above Kestrel's ~28 MB default so large inputs and
// dependency bundles aren't rejected (only the web API can reach this service, and it caps sizes).
const long MaxRequestBytes = 300L * 1024 * 1024;
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = MaxRequestBytes);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = MaxRequestBytes);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<ObfuscationRunner>();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
