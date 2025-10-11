# Synka Backend AI agent guide

## Big picture

- Use ASP.NET Core to expose storage, sync, and authentication APIs consumed by Synka Web.
- SQLite is the default datastore, but keep PostgreSQL fully supported; surface connection strings via environment variables so container deployments (`ghcr.io/synka-org/synka`) stay drop-in.

## Runtimes & tooling

- Target .NET 10 preview. Use the `dotnet` CLI (`dotnet new`, `dotnet restore`, `dotnet run --project src/Synka.Server`). Prefer nullable reference types, async I/O, and minimal APIs/controllers where appropriate.
- When you need official Microsoft/Azure references (ASP.NET Core, EF Core, Azure storage, etc.), call the Microsoft Learn MCP: start with `microsoft_docs_search`, fetch the full article with `microsoft_docs_fetch`, and grab vetted snippets via `microsoft_code_sample_search`.

## Project conventions

- Structure the server with folders such as `Controllers/`, `Services/`, `Data/`, and `Contracts/` inside `src/Synka.Server`.
- Keep interfaces and their implementations in separate files—one type per file keeps diffs tight and satisfies analyzers.
- When adding new classes, prefer C# primary constructors to wire dependencies instead of manual fields/constructors when practical.
- Keep configuration in `appsettings*.json` and wire secrets through environment variables (remember Docker/Kubernetes compatibility).
- Maintain DTO parity with the frontend by generating TypeScript clients from C# OpenAPI documents whenever endpoints change.

## Integration hints

- Expose REST endpoints that default to port 8080 so existing Docker and frontend tooling keep working.
- Use EF Core migrations under `src/Synka.Server/Migrations` when modelling database changes.
- Authentication/authorisation should build on ASP.NET Identity to support sharing and permissions scenarios.

## Testing & quality

- Co-locate backend unit tests under `tests/` mirroring `src/` namespaces. Execute them with `dotnet test`.
- Provide integration tests or contract tests whenever you adjust public APIs consumed by the Angular client.
- Structure all test cases using the Arrange-Act-Assert pattern with explicit sections or comments that make each phase obvious.
- Update `README.md` whenever new commands, migrations, or configuration knobs are introduced.

## Workflow tips

- Run `dotnet build src/Synka.Server` before opening PRs.
- After every file edit, ensure formatting stays clean.
