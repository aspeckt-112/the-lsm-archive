# Copilot Instructions

## Build & Run

```bash
dotnet restore && dotnet build
docker compose up -d          # Postgres on localhost:20984 (mapped from container 5432)
dotnet run --project src/TheLsmArchive.Web.Api
dotnet run --project src/TheLsmArchive.Patreon.Ingestion
```

## Tests

```bash
dotnet test                                                       # all tests
dotnet test --filter "FullyQualifiedName~EpisodeServiceTests"     # single class
dotnet test --filter "DisplayName~GetById_WithInvalidId"          # single test
```

Tests use **Testcontainers** (real Postgres, no mocking of the DB). Docker must be running.

## Architecture

Seven projects:

| Project | Role |
|---|---|
| `TheLsmArchive.Models` | Shared DTOs/records (requests, responses, enums). No project dependencies. |
| `TheLsmArchive.Common` | Shared utility foundation (currently minimal). |
| `TheLsmArchive.Database` | EF Core DbContext, entities, configurations, migrations. |
| `TheLsmArchive.ApiClient` | Typed HTTP client wrapping the Web API (used by the Frontend). |
| `TheLsmArchive.Web.Api` | ASP.NET Core minimal API — the backend. |
| `TheLsmArchive.Web.Frontend` | Blazor WebAssembly SPA using MudBlazor. |
| `TheLsmArchive.Patreon.Ingestion` | Console app: parses Patreon RSS, calls Google Gemini for summaries, writes to DB. |

The **database** runs EF Core 10 on Postgres 13 with `UseSnakeCaseNamingConvention()`. All entity type configurations live in `TheLsmArchive.Database/Configurations/` and are auto-discovered via `ApplyConfigurationsFromAssembly`. EF migrations are in `TheLsmArchive.Database/Migrations/`.

The API auto-migrates on startup in Development (`await dbContext.Database.MigrateAsync()`).

The connection string key is `"thelsmarchive"` (configured in `appsettings.json` or User Secrets).

## Key Conventions

### Minimal API structure (Web.Api)

Endpoints live in `Features/<Domain>/` alongside a service interface and implementation. The standard pattern per feature:

```
Features/
  Episodes/
    EpisodeEndpoints.cs   ← MapGroup("/episode"), extension method on WebApplication
    EpisodeService.cs
    IEpisodeService.cs
```

Endpoint files use the new C# `extension` block syntax (not static methods directly):

```csharp
internal static class EpisodeEndpoints
{
    extension(WebApplication app)
    {
        internal WebApplication AddEpisodeEndpoints() { ... }
    }

    private static async Task<Results<Ok<Episode>, NotFound>> GetEpisodeById(...) { ... }
}
```

Always use `TypedResults` and `Results<T1, T2>` return types on handlers.

### Database context

Use the single `LsmArchiveDbContext` everywhere. Read-heavy services should call `AsNoTracking()` explicitly at query roots instead of relying on a separate read-only context type, and background ingestion creates short-lived contexts via `IDbContextFactory<LsmArchiveDbContext>`.

### Centralized NuGet versions

All package versions are in `Directory.Packages.props`. Do not specify versions in individual `.csproj` files.

`Directory.Build.props` automatically injects xUnit dependencies into any project whose name ends with `.Tests`.

### Integration test pattern

```csharp
[Collection(nameof(ServiceIntegrationTestFixture))]
public class MyServiceTests : BaseServiceIntegrationTest, IClassFixture<ServiceIntegrationTestFixture>
{
    public MyServiceTests(ServiceIntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task SomeTest()
    {
        // Use ReadOnlyDbContext / ReadWriteDbContext from base class
        // Use InsertSingleInstanceOfEntityAsync<T> to seed data
        // Use TestContext.Current.CancellationToken for cancellation
    }
}
```

`ServiceIntegrationTestFixture` starts a `postgres:13.22-alpine3.22` Testcontainer, creates the schema via `EnsureCreatedAsync`, and is shared across a collection to avoid re-spinning the container per test.

### Paging

Use the `WithPaging(pagedRequest)` extension on `IQueryable<T>` (defined in `Web.Api/Infrastructure/`) when returning paginated results. Return a `PagedResponse<T>` from `TheLsmArchive.Models`.

### EF projection pattern

Services project directly to model records inside LINQ `.Select()` using inline `Expression<Func<TEntity, TModel>>` variables rather than mapping after materialisation.

### Code style

- Nullable reference types are enabled globally.
- XML doc comments on all public and `internal` API surface.
- PRs target the `develop` branch.

## MCP Servers

### Available MCP Servers
- `microsoft_learn` — Official Microsoft Learn content (documentation, tutorials, api reference, etc.)