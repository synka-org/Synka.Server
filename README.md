# Synka Backend

This solution hosts the Synka ASP.NET Core backend targeting .NET 10.0. It ships with a SQLite-first configuration while keeping PostgreSQL fully supported so deployments can switch providers using environment variables.

**The backend also serves the Angular frontend** from the `Synka.Web` project when built as a Docker container, providing a complete single-image deployment solution.

---

## 🐳 Docker deployment (home lab)

Run Synka in a container with a single command:

```bash
docker run --rm -p 8080:80 ghcr.io/synka-org/synka
```

Point your browser at `http://localhost:8080`.

For comprehensive Docker documentation, see [DOCKER.md](DOCKER.md).

---

## 🛠️ Developer Prerequisites

### For Local Development

- [.NET SDK 10.0.100-rc.1](https://dotnet.microsoft.com/download) (matching `global.json`)
- SQLite 3 (bundled via EF Core provider)
- Optional: PostgreSQL 16+ for production parity

### For Docker Deployment

- Docker 20.10+ or compatible runtime
- Docker Compose 2.0+ (optional, for orchestration)

## 🚀 Getting started

### Running Locally

```bash
# Restore dependencies
dotnet restore Synka.sln

# Run the API (default SQLite)
dotnet run --project src/Synka.Server

# Execute the test suite
dotnet test Synka.sln
```

### Running with Docker

The Dockerfile builds both the Angular frontend (from `../Synka.Web`) and the .NET backend into a single optimized image.

#### Quick Start with Docker Compose

```bash
# Build and start the container
docker-compose up -d

# View logs
docker-compose logs -f

# Stop the container
docker-compose down
```

The application will be available at `http://localhost:8080`.

#### Manual Docker Build

```bash
# Build the image using the convenience script
./build-docker.sh

# Or build manually
docker build -t synka:latest .

# Run the container
docker run -d \
  -p 8080:8080 \
  -v synka-data:/app/data \
  --name synka \
  synka:latest

# Run with PostgreSQL (via environment variables)
docker run -d \
  -p 8080:8080 \
  -e Database__Provider=Postgres \
  -e ConnectionStrings__Postgres="Host=postgres;Port=5432;Database=synka;Username=synka;Password=ChangeMe123" \
  --name synka \
  synka:latest
```

#### Build Script Options

The `build-docker.sh` script supports several options:

```bash
./build-docker.sh --help

# Build with a specific tag
./build-docker.sh --tag v1.0.0

# Build and push to registry
./build-docker.sh --tag v1.0.0 --push

# Build for a different platform
./build-docker.sh --platform linux/arm64

# Use nightly build of Synka.Web (faster, no need for Synka.Web source)
./build-docker.sh --nightly --tag nightly
```

#### Nightly Builds

Synka.Server can be built using pre-built nightly releases from Synka.Web, eliminating the need to have both repositories checked out:

```bash
# Build using Synka.Web nightly release
./build-docker.sh --nightly

# Or pull the pre-built nightly image
docker pull ghcr.io/synka-org/synka:nightly
```

Nightly builds run automatically every day at 03:00 UTC and are available as:

- `ghcr.io/synka-org/synka:nightly` (always latest)
- `ghcr.io/synka-org/synka:nightly-YYYYMMDD` (dated snapshot)

⚠️ **Warning:** Nightly builds are unstable and intended for testing only. Use stable releases for production.

## API Endpoints

The API exposes:

- `GET /health` for health checks
- `GET /api/manifest` returning a service manifest with configuration status
- Identity API endpoints (register/login/token management) under `/auth/*`
- OpenAPI metadata at `/openapi.json` while running in `Development` or when `OpenApi__Expose=true`

The root path `/` serves the Angular frontend (when built with Docker).

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
