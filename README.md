# BitMono obfuscation service

This is the thing that actually does the obfuscation behind the BitMono website. It wraps the BitMono obfuscator (the `BitMono.*` NuGet packages) and puts it behind a small HTTP API.

It's internal. The website ([bitmono-web](https://github.com/bitmono-project)) calls it over the Aspire network. You're not meant to hit it directly.

## Why it's its own service

It's split out from the website on purpose:

- If someone uploads a broken or giant assembly, the obfuscation can blow up here instead of taking the whole site down.
- BitMono ships new versions often, so this service can be bumped and redeployed on its own without touching the website.

Obfuscation runs in-process, with static analysis only ([AsmResolver](https://github.com/Washi1337/AsmResolver)). It never runs the assembly you upload.

## Endpoints

- `POST /obfuscate` — multipart form: one `file` plus repeated `protections` fields (the protection names to enable). Returns the obfuscated assembly as `application/octet-stream`. If no known protections are sent it falls back to the default level.
- `GET /protections` — the catalog: every protection, what it does, its category, whether it's stable, any caveat, and which level it belongs to.
- `GET /version` — the BitMono version this service is built against.
- `GET /health` — health check.

The protections come per request from the website — the user picks them there. Nothing about which protections to run is configured in this repo.

## Running it

Normally you don't run this by hand — the bitmono-web Aspire AppHost starts it for you.

If you do want it standalone:

```bash
cd BitMono.ObfuscationService
dotnet run
```

Set `ASPNETCORE_URLS` if you want a fixed port, e.g. `ASPNETCORE_URLS=http://localhost:5080 dotnet run`.

Or build the container:

```bash
docker build -t bitmono-obfuscation-service .
docker run -p 8743:8743 -e HTTP_PORTS=8743 bitmono-obfuscation-service
```

Aspire sets `HTTP_PORTS` automatically. Standalone, pick any free port via `HTTP_PORTS` or `ASPNETCORE_URLS`.
