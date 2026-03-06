# The LSM Archive

The LSM Archive is a comprehensive system designed to archive, summarize, and search through episodes, topics, and people from **Last Stand Media** (LSM) podcasts. It ingests content directly from Patreon RSS feeds, uses Google Gemini AI to generate structured summaries, and provides a modern web interface for exploration.

## Project Structure

The solution follows a modular architecture split into seven distinct projects:

| Project | Role |
| :--- | :--- |
| `TheLsmArchive.Models` | Shared DTOs, request/response records, and enums. |
| `TheLsmArchive.Common` | Shared utility foundation and constants. |
| `TheLsmArchive.Database` | EF Core `DbContext`, entities, and migrations for PostgreSQL. |
| `TheLsmArchive.ApiClient` | A typed HTTP client for the Web API, used by the frontend. |
| `TheLsmArchive.Web.Api` | ASP.NET Core Minimal API providing the backend services. |
| `TheLsmArchive.Web.Frontend` | Blazor WebAssembly SPA using MudBlazor for the UI. |
| `TheLsmArchive.Patreon.Ingestion` | A worker application that parses Patreon RSS feeds and summarizes them via Gemini. |

## Prerequisites

- **.NET SDK**: 10.0.100+ (as specified in `global.json`).
- **Docker**: Required for running the PostgreSQL database via Testcontainers and `compose`.

## Getting Started

### 1. Database Setup
The project uses PostgreSQL 13. A `docker-compose.yml` file is provided to spin up the database locally.

```bash
docker compose up -d
```
*Note: The database is mapped to `localhost:20984`.*

### 2. Configuration & User Secrets
Several projects require sensitive configuration. Use `dotnet user-secrets` to set these during development.

#### Database Connection
Set the connection string in both `TheLsmArchive.Web.Api` and `TheLsmArchive.Patreon.Ingestion`:
```bash
dotnet user-secrets set "ConnectionStrings:thelsmarchive" "Host=localhost;Port=20984;Database=thelsmarchive;Username=sa;Password=YourStrong@Passw0rd" --project src/TheLsmArchive.Web.Api
```
```bash
dotnet user-secrets set "ConnectionStrings:thelsmarchive" "Host=localhost;Port=20984;Database=thelsmarchive;Username=sa;Password=YourStrong@Passw0rd" --project src/TheLsmArchive.Patreon.Ingestion
```

#### Patreon Ingestion
The ingestion project requires a Google Gemini API key and your private Patreon RSS feeds.

**Gemini API Key:**
```bash
dotnet user-secrets set "GeminiOptions:ApiKey" "YOUR_GEMINI_API_KEY" --project src/TheLsmArchive.Patreon.Ingestion
```

**RSS Feeds:**
The feeds are configured as a list of `RssFeedSource` objects. You can set them via JSON in user secrets:
```bash
dotnet user-secrets set "RssFeedSources" '[{"Name": "YOUR_RSS_FEED_NAME", "Url": "YOUR_PRIVATE_RSS_URL"}, {"Name": "ANOTHER_RSS_FEED_NAME", "Url": "YOUR_PRIVATE_RSS_URL"}]' --project src/TheLsmArchive.Patreon.Ingestion
```

### 3. Build & Run
Restore dependencies and build the solution:
```bash
dotnet restore
dotnet build
```

Run the API:
```bash
dotnet run --project src/TheLsmArchive.Web.Api
```

Run the Ingestion tool:
```bash
dotnet run --project src/TheLsmArchive.Patreon.Ingestion
```

Run the Frontend:
```bash
dotnet run --project src/TheLsmArchive.Web.Frontend
```
*Note: The frontend is pre-configured to connect to the API at `https://localhost:7229` (the default HTTPS profile). Ensure the API is running before launching the frontend.*

## Testing

Tests utilize **Testcontainers** to run against a real PostgreSQL instance. Ensure Docker is running before executing tests.

```bash
dotnet test
```

## Contributing

Contributions are welcome! To maintain quality and consistency, please follow these guidelines:

1. **Issues First**: Before starting any major work, please raise a new issue or comment on an existing one to discuss the proposed changes.
2. **Code Style**: 
   - Respect the `.editorconfig` settings.
   - Run `dotnet format` before submitting your changes to ensure consistent formatting.
3. **Pull Requests**: Target the `develop` branch for all PRs.

---
*This project is an unofficial fan-made archive for Last Stand Media.*
