# Security Setup

This project does **not** ship real secrets in `appsettings.json`. You must provide them locally and in production.

## Required secrets

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Jwt:SecretKey` | HMAC key for signing JWT access/refresh tokens. Min 32 chars, recommended 64+ |

The application will refuse to start if either is missing, too short, or set to a placeholder value.

## Local development

Pick **one** of these — all three work, in this priority order (later overrides earlier):

### Option A — `appsettings.Development.json` (simplest)

This file is gitignored. Copy `appsettings.Example.json` to `appsettings.Development.json` and fill in real values:

```bash
cp appsettings.Example.json appsettings.Development.json
```

Then edit it locally. It will never be committed.

### Option B — .NET User Secrets (recommended for shared dev machines)

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=pos_dev;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Prefer"
dotnet user-secrets set "Jwt:SecretKey" "$(openssl rand -base64 64)"
```

Stored outside the repo in `%APPDATA%\Microsoft\UserSecrets\` (Windows) or `~/.microsoft/usersecrets/` (Linux/Mac).

### Option C — Environment variables

In .NET, nested config keys use double underscores:

```powershell
# PowerShell (Windows)
$env:ConnectionStrings__DefaultConnection = "Host=localhost;..."
$env:Jwt__SecretKey = "long-random-string-here"
```

```bash
# Bash (Linux/Mac/WSL)
export ConnectionStrings__DefaultConnection="Host=localhost;..."
export Jwt__SecretKey="long-random-string-here"
```

## Production

**Always use environment variables** (or a secret manager like Azure Key Vault / AWS Secrets Manager). Do not deploy `appsettings.Development.json`.

For IIS hosting, set environment variables in `web.config` `<environmentVariables>` or in IIS Manager → Configuration Editor.

## Generating a strong JWT secret

```bash
# Linux/Mac/WSL
openssl rand -base64 64

# PowerShell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

## If you previously committed secrets

Real secrets that were ever committed to git **must be rotated**, even after the file is sanitized. Git history still contains them. For this project:

1. **Rotate the database password** in your Neon/Postgres console
2. **Generate a new JWT secret** — this invalidates all existing access tokens (users will need to log in again)
3. Optionally rewrite history with `git filter-repo` to scrub the old values, but rotation is what actually matters

## Verification

After setup, run the app. You should see it start normally. If you see:

```
Application cannot start due to missing or invalid configuration:
  - Jwt:SecretKey is not set...
```

the validator is doing its job — provide the missing values and try again.
