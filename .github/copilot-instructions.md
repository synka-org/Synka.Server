# Synka Backend AI Agent Guide

## Overview

Single ASP.NET Core 10 minimal API hosting authentication, synchronization, and health endpoints. Keep `Program.cs` lean—route all setup through extension helpers in `Extensions/`.

**Tech stack:**
- ASP.NET Identity + EF Core for authentication
- SQLite (default) or PostgreSQL via `DatabaseProviderAccessor` (controlled by `Database__Provider` env var)
- Minimal APIs with extension-based bootstrap pattern

## Architecture & Patterns

### Extension-based bootstrap
- **Service registration** (`WebApplicationBuilderExtensions`):
  - `AddSynkaCoreServices` → ProblemDetails, OpenAPI, authorization policies
  - `AddSynkaDatabase` → provider-aware DbContext (SQLite/PostgreSQL)
  - `AddSynkaAuthentication` → Identity API + optional OIDC
  - `AddSynkaApplicationServices` → domain services (`ConfigurationService`, `ConfigurationStateService`)

- **Runtime wiring** (`WebApplicationExtensions`):
  - `MapOpenApiDocument` → dev-only unless `OpenApi__Expose=true`
  - `EnsureDatabaseIsMigrated` → apply EF migrations on start
  - `MapAuthenticationEndpoints` → Identity API with admin-only `/auth/register`
  - `MapServiceManifestEndpoint` → root service manifest
  - `MapConfigurationEndpoint` → initial/required configuration endpoint

### Separation of concerns
- **Endpoint handlers MUST be lightweight**—business logic belongs in `Services/`. Handlers only orchestrate service calls and map results to HTTP responses.
- **DTOs** live in `Contracts/` with `Request`/`Response` suffixes (e.g., `ConfigurationRequest`, `ServiceManifestResponse`)
- **Entities** live in `Data/Entities/` (e.g., `ApplicationUserEntity`)
- **Services** use primary constructors and access constructor parameters directly without creating private fields:
  ```csharp
  public sealed class MyService(IDependency dependency) : IMyService
  {
      // Use 'dependency' directly, no private fields needed
      public Task DoWork() => dependency.PerformAsync();
  }
  ```

### Authorization
- Admin-only endpoints use `AuthorizationPolicies.AdministratorOnly`
- Registration endpoint (`/auth/register`) is admin-only
- Fallback policy requires authenticated users unless explicitly marked `AllowAnonymous`

## Configuration & Integrations

- **Database**: `appsettings.json` defines SQLite and Postgres connection strings; `Database:Provider` selects one
- **OIDC**: Federation enabled when `Authentication:OIDC:Authority` is set; configure scopes and callback path
- **OpenAPI**: Exposed in dev by default; set `OpenApi__Expose=true` for production

## Developer Workflow

**Build & run:**
```bash
dotnet restore Synka.slnx
dotnet build src/Synka.Server
dotnet run --project src/Synka.Server  # Listens on 8080/HTTPS
```

**Testing:**
- Tests in `tests/Synka.Server.Tests` use TUnit + `WebApplicationFactory<Program>`
- Follow AAA structure (`// Arrange // Act // Assert`)
- For authenticated tests, extend `AuthenticatedSchemeWebApplicationFactory` or use `TestAuthHandler`
- Run: `dotnet test`

**EF Migrations:**
```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add <Name> --project src/Synka.Server
dotnet tool run dotnet-ef database update
```

## Quality Gates

**Code quality:**
- Roslynator analyzers enforced; run `dotnet format analyzers` before committing
- Remove unused usings
- Target .NET 10 (per `global.json`)

**Git conventions:**
- Conventional Commits: `feat:`, `fix:`, `docs:`, `ci:`, `test:`, `style:`, etc.
- Branch naming: `feat/...`, `fix/...`, `ci/...`, `docs/...`, `style/...`
- **🚨 CRITICAL: Branch workflow** (NEVER skip these steps):
  1. `git checkout main` — switch to main branch
  2. `git pull origin main` — get latest changes
  3. `git checkout -b <type>/<description>` — create new branch from updated main
  4. Make your changes and commit
  5. `git push -u origin <branch-name>` — push branch
  6. Create PR via GitHub
  
  **🛑 ABSOLUTE RULES:**
  - **NEVER COMMIT DIRECTLY TO MAIN** — All changes MUST go through PRs
  - **NEVER commit to an existing feature branch** — Each change needs its own branch
  - **ALWAYS create a new branch from updated main** — Ensures clean git history
  - **ALWAYS verify current branch before committing** — Use `git branch --show-current`

**Documentation:**
- Update tests when endpoint behavior changes
- Document new config in `README.md`
- Keep OpenAPI exposure rules accurate
