# Observability

The API ships logs to three sinks. The first two are always on. The third is optional.

| Sink | Where | When |
|------|-------|------|
| **Console** | stdout | Always — handy when running `dotnet run` |
| **Rolling file** | `logs/pos-system-YYYYMMDD.log` | Always — daily rotation, 10 MB cap, 30 days retained |
| **Seq** | HTTP endpoint you specify | **Only** when `Serilog:Seq:ServerUrl` is set |

## Why Seq

Console + file is fine for "is the app running" but useless for "why did this customer's checkout fail at 14:32 yesterday." Seq stores logs as structured events and lets you query them with SQL-like syntax in a browser. Filtering by `OrderNumber`, `UserId`, `RequestPath`, etc. is one click instead of grepping rolled log files.

## Local Seq (free, 1 minute)

Run a Seq container:

```bash
docker run -d --name seq -p 5341:5341 -e ACCEPT_EULA=Y datalust/seq:latest
```

Open <http://localhost:5341> in a browser to confirm it's up.

Tell the app to ship to it. In `appsettings.Development.json` (gitignored):

```json
{
  "Serilog": {
    "Seq": {
      "ServerUrl": "http://localhost:5341"
    }
  }
}
```

Or via env var (PowerShell):

```powershell
$env:Serilog__Seq__ServerUrl = "http://localhost:5341"
```

Restart the app. Every log event now appears in the Seq UI within a second.

## Production

Use a hosted Seq, an Elasticsearch sink, or any other Serilog sink. The pattern is the same — set `Serilog:Seq:ServerUrl` (and optionally `Serilog:Seq:ApiKey` if your Seq instance requires authentication) via environment variables on the host. Don't put production secrets in committed config files.

## What's logged

The app's existing Serilog pipeline already enriches every event with:

- `MachineName`, `EnvironmentName` (set at startup)
- `RequestHost`, `RequestScheme`, `UserAgent` (set per HTTP request via `UseSerilogRequestLogging`)
- `UserName`, `UserId` (set per request when the user is authenticated)
- `SourceContext` (set automatically by Serilog when injected as `ILogger<T>`)

These are all queryable in Seq.

## Levels

Default minimum level is `Information`. Microsoft and System namespaces are clamped to `Warning` so framework noise doesn't drown out application events. Adjust in `Program.cs` if you need to debug a specific area.

## Disabling

To turn Seq off, leave `Serilog:Seq:ServerUrl` empty (the default). The app falls back to console + file silently — no error, no startup failure.
