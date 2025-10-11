# Synka Backend

This solution hosts the Synka ASP.NET Core backend targeting .NET 10.0. It ships with a SQLite-first configuration while keeping PostgreSQL fully supported so deployments can switch providers using environment variables.

---

## 🐳 Docker deployment (home lab)

Run Synka in a container with a single command:

```bash
docker run --rm -p 8080:80 ghcr.io/synka-org/synka
```

Point your browser at `http://localhost:8080`.

---

## 🛠️ Developer Prerequisites

- [.NET SDK 10.0.100-rc.1](https://dotnet.microsoft.com/download) (matching `global.json`)
- SQLite 3 (bundled via EF Core provider)
- Optional: PostgreSQL 16+ for production parity

## 🚀 Getting started

```bash
# Restore dependencies
 dotnet restore Synka.sln

# Run the API (default SQLite)
 dotnet run --project src/Synka.Server

# Execute the test suite
 dotnet test Synka.sln
```

The API exposes:

- `GET /health` for health checks
- `GET /` returning a small service manifest
- Identity API endpoints (register/login/token management) under `/auth/*`
- OpenAPI metadata at `/openapi.json` while running in `Development` or when `OpenApi__Expose=true`

## 🗄️ Database configuration

By default, the application reads the following configuration from `appsettings.json`:

```json
{
  "Database": {
    "Provider": "Sqlite"
  },
  "ConnectionStrings": {
    "Sqlite": "Data Source=synka.db",
    "Postgres": "Host=localhost;Port=5432;Database=synka;Username=synka;Password=ChangeMe;Ssl Mode=Prefer"
  }
}
```

Override the provider via environment variables when deploying:

```bash
export Database__Provider=Postgres
export ConnectionStrings__Postgres="Host=postgres;Port=5432;Database=synka;Username=synka;Password=SuperSecret;Ssl Mode=Require"
```

Entity Framework Core 10 packages for both SQLite and PostgreSQL are pre-installed. The repository also bundles a [local tool manifest](./dotnet-tools.json) so you can run migration commands with `dotnet tool restore && dotnet tool run dotnet-ef`. Generate migrations inside `src/Synka.Server/Data/Migrations` as the model evolves.

## 🔐 Authentication

The backend ships with ASP.NET Core Identity using Entity Framework Core stores:

- Username/password accounts are persisted in the configured database (SQLite by default).
- Identity API endpoints are mapped under `/auth` for registration, login, logout, password reset, etc.
- Cookie authentication is used for session management.
- Optional OpenID Connect federation can be enabled by providing configuration under `Authentication:OIDC`.

Example configuration (environment variables shown for deployment):

```bash
export Authentication__OIDC__Authority="https://login.contoso.com"
export Authentication__OIDC__ClientId="synka-backend"
export Authentication__OIDC__ClientSecret="<client-secret>"
export Authentication__OIDC__CallbackPath="/signin-oidc"
export Authentication__OIDC__ResponseType="code"
export Authentication__OIDC__Scopes__0="openid"
export Authentication__OIDC__Scopes__1="profile"
export Authentication__OIDC__Scopes__2="email"
```

Set `OpenApi__Expose=true` to serve `openapi.json` outside of development when necessary; otherwise the document remains hidden in production for security.
