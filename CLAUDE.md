# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run API (HTTP: localhost:5173, HTTPS: localhost:7283)
dotnet run --project src/Api

# Run all tests
dotnet test

# Run a single test
dotnet test tests/GuildManagerApi.Tests/GuildManagerApi.Tests.csproj --filter "FullyQualifiedName~TestName"

# Add a migration
dotnet ef migrations add MigrationName --project src/Infrastructure --startup-project src/Api

# Apply migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

Migrations are stored in `src/Api/Migrations/` and run automatically on startup.

Swagger UI is available at `/swagger` in development only.

## Architecture

Clean Architecture with four layers — dependency flow is Domain ← Application ← Infrastructure ← Api:

- **Domain** (`src/Domain/`) — Entities, enums, repository interfaces, service contracts, domain exceptions. No external dependencies.
- **Application** (`src/Application/`) — Business logic (import, scoring, guild sync), JWT/Auth services, WarcraftLogs GraphQL client, DTOs, background workers, WebSocket hubs.
- **Infrastructure** (`src/Infrastructure/`) — EF Core `AppDbContext`, repository implementations, WCL OAuth token service, AES-GCM field encryption.
- **Api** (`src/Api/`) — ASP.NET Core controllers, middleware, DI wiring in `Program.cs`, migrations.
- **Tests** (`tests/GuildManagerApi.Tests/`) — xUnit with EF Core InMemory provider.

## Key Domain Concepts

**WarcraftLogs Integration:** The API acts as a middleware between clients and the WarcraftLogs GraphQL API. It supports two modes resolved automatically:
- **Public** — Client Credentials OAuth → `/api/v2/client` (WCL public endpoint)
- **Private** — Authorization Code OAuth per user → `/api/v2/user` (WCL private endpoint)

The `WclGraphQLClient` in Application handles automatic endpoint resolution based on whether the authenticated user has an active WCL token.

**Report Import:** Async — requests are queued via `ImportWorker` (background queue). Progress is pushed to clients over WebSockets (`ImportProgressHub`). Reports are idempotently upserted.

**Player Scoring:** Converts WarcraftLogs `rankPercent` into points using configurable `ScoringTier` thresholds stored in `ScoringSettings`. Weekly `PenaltyEvent` deductions are applied on top.

**Guild Sync:** `GuildSyncWorker` background queue, paged retrieval from WCL, with WebSocket progress notifications via `GuildSyncHub`.

## Authentication

- **Local:** JWT Bearer with refresh token rotation. Rate-limited login endpoint.
- **WarcraftLogs OAuth:** Authorization Code Flow stored per user as `WclUserToken` (fields encrypted via AES-GCM). State nonces stored in `IMemoryCache` to prevent CSRF.
- **WebSocket connections** require the JWT as a query parameter: `?access_token=<jwt>`.

## Infrastructure Notes

- **Database:** PostgreSQL 15+ via EF Core 10. Connection string configured in `appsettings.json`.
- **Rate Limiting:** Redis-backed (`AspNetCoreRateLimit`) — IP-based and client-ID-based. `ClientIdInjectionMiddleware` injects the tracking header.
- **Error Handling:** `GlobalExceptionMiddleware` converts exceptions to ProblemDetails format.
- **CORS:** Configured for a Tauri desktop client (origins: `localhost:1420`, `tauri://`).
- **HTTPS Redirect:** Only applied when not in development (environment-checked in `Program.cs`).

## Configuration

Copy `src/Api/appsettings.example.json` to `appsettings.json` (not committed). Key sections: `ConnectionStrings`, `JwtSettings`, `WclCredentials`, `RateLimiting`, `Redis`.
