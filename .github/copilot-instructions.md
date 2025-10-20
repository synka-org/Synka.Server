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
- **Project query results directly to contracts.** When database data flows into an HTTP response, use EF Core projection (`Select`) or dedicated mappers at query time so the database call materializes the response shape instead of returning tracked entities.
- **Services** use primary constructors and access constructor parameters directly without creating private fields:
  ```csharp
  public sealed class MyService(IDependency dependency) : IMyService
  {
      // Use 'dependency' directly, no private fields needed
      public Task DoWork() => dependency.PerformAsync();
  }
  ```

### Authorization
- Admin-only endpoints use `AuthorizationPolicies.AdminOnly`
- Registration endpoint (`/auth/register`) is admin-only
- Fallback policy requires authenticated users unless explicitly marked `AllowAnonymous`

### C# Coding Style
- **Use `var` for local variable declarations** instead of explicit types when the type is clear from the assignment
- Example: `var factory = new WebApplicationFactory<Program>()` instead of `WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()`
- Exception: Use explicit types when the type is not obvious from the right-hand side or when it improves readability
- **One type per file.** When adding or modifying types, ensure each record/class/struct/interface lives in its own file; do not group multiple type declarations together.
- **Contracts never depend on entity types.** Do not reference `Synka.Server.Data` or entity-layer enums from `Contracts/`; declare contract-specific equivalents as needed.

### Logging
- **ALWAYS use LoggerMessage delegates** for structured logging instead of `ILogger` extension methods
- Create logger delegates in `Services/Logging/[ServiceName]Loggers.cs` files
- Example:
  ```csharp
  internal static partial class MyServiceLoggers
  {
      [LoggerMessage(
          EventId = 1,
          Level = LogLevel.Information,
          Message = "Processing item {ItemId}")]
      public static partial void LogItemProcessing(
          ILogger logger,
          Guid itemId,
          Exception? exception);
  }
  ```
- Use the delegates in your service:
  ```csharp
  MyServiceLoggers.LogItemProcessing(logger, itemId, null);
  ```
- Benefits: compile-time safety, better performance, consistent structured logging

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

Migrations are stored in `src/Synka.Server/Data/Migrations/` and applied automatically on app start via `EnsureDatabaseIsMigrated()`.

```bash
# Restore EF Core tools
dotnet tool restore

# Create a new migration (run from project directory)
cd src/Synka.Server
dotnet ef migrations add <MigrationName> --output-dir Data/Migrations

# Or from repository root:
dotnet ef migrations add <MigrationName> --project src/Synka.Server --output-dir Data/Migrations

# Apply migrations to database (usually automatic via app startup)
dotnet ef database update --project src/Synka.Server

# Remove last migration (if not yet applied)
cd src/Synka.Server
dotnet ef migrations remove
```

**Migration naming conventions:**
- Use PascalCase: `AddUserPreferences`, `UpdateProductSchema`
- Be descriptive: what changed, not how
- Avoid generic names like `Update1` or `Changes`

## Quality Gates

**Code quality:**
- Roslynator analyzers enforced; run `dotnet format analyzers` before committing
- Remove unused usings
- Target .NET 10 (per `global.json`)

**🚨 CRITICAL: Pre-commit validation** (ALWAYS run before committing):
```bash
# Verify code formatting
dotnet format --verify-no-changes --verbosity diagnostic

# Verify analyzer rules
dotnet format analyzers --verify-no-changes --verbosity diagnostic
```
**If either command fails:**
1. Run `dotnet format` to fix formatting issues
2. Run `dotnet format analyzers` to fix analyzer violations
3. Review changes and stage fixed files
4. Re-run verification commands before committing

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

## AI Agent Workflow Rules

**🚨 CRITICAL: Feature Branch Requirement**

- **ALWAYS create a feature branch BEFORE making any code changes**
- **NEVER work directly on main branch** - all development MUST happen on feature branches
- **Branch creation is MANDATORY** - do not proceed with changes until a feature branch exists

**Feature Branch Workflow:**

1. **Before Making ANY Changes:**
   - Check current branch: `git branch --show-current`
   - If on `main`: STOP and create feature branch first
   - Branch naming: `feat/<description>`, `fix/<description>`, `docs/<description>`, etc.
   - Example: `git checkout -b feat/add-folder-support`

2. **After Creating Feature Branch:**
   - Make your code changes
  - Run tests to verify
  - Run `dotnet format --verify-no-changes` and `dotnet format analyzers --verify-no-changes`
    - If either command fails, fix the issues locally using `dotnet format` and/or `dotnet format analyzers`
    - Re-run both verification commands until they succeed before asking about commits
   - Present summary of changes to user

3. **When User Says "commit" or "yes" to commit:**
   - **Verify you're on a feature branch** (not main)
   - **Immediately commit the staged changes** without asking again
   - After committing, **ask once** if user wants a PR created
   - Do NOT ask for confirmation before committing if user already said yes

4. **When User Says "commit and pr" or "yes and pr":**
   - **Verify you're on a feature branch** (not main)
   - **Immediately commit the staged changes** 
   - **Immediately create a Pull Request** using GitHub MCP
   - Do NOT ask for any confirmations

5. **When Making Changes:**
   - **FIRST: Ensure you're on a feature branch**
   - Make the changes
   - Present a summary of changes made
   - Ask: "Should I commit these changes?" (or "commit and pr?")
   - Wait for explicit user response
   - Then follow the rules above based on their response

6. **Markdown File Editing:**
   - **ALWAYS check for markdown linter errors after editing markdown files**
   - Run validation or report any formatting issues found
   - Fix critical formatting issues before committing

**Example Workflows:**

*Scenario 1: User wants to review before PR*
```
AI: I've made the following changes:
    - Modified: Program.cs
    - Added: NewFeature.cs
    
    Should I commit these changes?

User: yes

AI: [Commits immediately]
    Changes committed successfully.
    
    Would you like me to create a Pull Request?

User: yes

AI: [Creates PR immediately]
```

*Scenario 2: User wants both immediately*
```
AI: I've made the following changes:
    - Modified: Program.cs
    
    Should I commit and create a PR?

User: yes

AI: [Commits and creates PR immediately without asking again]
```
