# Docker Deployment Guide for Synka

This guide explains how to build and deploy Synka as a single Docker image that includes both the Angular frontend and .NET backend.

## Architecture

The Docker build process uses a multi-stage Dockerfile that downloads pre-built frontend releases:

1. **Stage 1 (web-release)**: Downloads pre-built Angular frontend from Synka.Web GitHub releases
2. **Stage 2 (backend-build)**: Builds the .NET backend from `Synka.Server` source
3. **Stage 3 (final)**: Combines both into a single runtime image

The final image:

- Serves the Angular app as static files from `/wwwroot`
- Runs the ASP.NET Core backend on port 8080
- Uses SQLite by default (can be configured for PostgreSQL)
- Runs as a non-root user for security
- Includes health checks for container orchestration

## Prerequisites

### Directory Structure

Only Synka.Server is required:

```text
/path/to/projects/
└── Synka.Server/
    ├── Dockerfile
    ├── docker-compose.yml
    ├── build-docker.sh
    └── ...
```

### Required Software
/path/to/projects/
└── Synka.Server/
    ├── Dockerfile
    ├── docker-compose.yml
    ├── build-docker.sh
    └── ...
```

### Required Software

- Docker 20.10 or later
- Docker Compose 2.0 or later (optional)

## Quick Start

### Option 1: Using Docker Compose (Recommended)

From the `Synka.Server` directory:

```bash
docker-compose up -d
```

Access the application at `http://localhost:8080`.

### Option 2: Using the Build Script

From the `Synka.Server` directory:

```bash
./build-docker.sh
docker run -d -p 8080:8080 -v synka-data:/app/data synka:latest
```

### Option 3: Manual Docker Build

From the **parent directory** containing both projects:

```bash
docker build -f Synka.Server/Dockerfile -t synka:latest .
docker run -d -p 8080:8080 -v synka-data:/app/data synka:latest
```

## Build Script Options

The `build-docker.sh` script supports several options:

```bash
# Build with custom tag
./build-docker.sh --tag v1.0.0

# Build for ARM64 (e.g., Raspberry Pi, Apple Silicon)
./build-docker.sh --platform linux/arm64

# Build and push to registry
./build-docker.sh --tag v1.0.0 --push

# Custom image name
./build-docker.sh --name myregistry/synka --tag latest

# Use nightly build of Synka.Web (faster, no source needed)
./build-docker.sh --nightly --tag nightly

# Use specific Synka.Web release
./build-docker.sh --nightly --web-release v1.0.0
```

## Nightly Builds

### Using Pre-built Nightly Images

The easiest way to test Synka is using the pre-built nightly images:

```bash
# Pull and run the latest nightly
docker pull ghcr.io/synka-org/synka:nightly
docker run -d -p 8080:8080 -v synka-data:/app/data ghcr.io/synka-org/synka:nightly

# Use a specific dated snapshot
docker pull ghcr.io/synka-org/synka:nightly-20251012
docker run -d -p 8080:8080 -v synka-data:/app/data ghcr.io/synka-org/synka:nightly-20251012
```

### Building Your Own Nightly

To build a nightly image locally without cloning Synka.Web:

```bash
# Build using latest Synka.Web nightly release
./build-docker.sh --nightly

# Build using a specific Synka.Web release
./build-docker.sh --nightly --web-release v1.2.3

# Build using a different repository
./build-docker.sh --nightly --web-repo yourorg/Synka.Web --web-release custom
```

### Automated Nightly Builds

Nightly builds are automatically created:

- **Schedule:** Daily at 03:00 UTC (1 hour after Synka.Web nightly)
- **Platforms:** linux/amd64, linux/arm64
- **Tags:**
  - `ghcr.io/synka-org/synka:nightly` (rolling, always latest)
  - `ghcr.io/synka-org/synka:nightly-YYYYMMDD` (dated snapshot)
- **Retention:** Last 7 dated tags are kept

### Nightly Build Benefits

- ✅ **Faster builds:** No need to compile Angular (saves ~5 minutes)
- ✅ **Smaller context:** Only Synka.Server repository required
- ✅ **CI/CD friendly:** Reduces build time and resource usage
- ✅ **Testing latest:** Always uses the latest Synka.Web frontend

### When to Use Each Build Type

| Scenario | Nightly | Versioned Release |
|----------|---------|-------------------|
| Production deployment | | ✅ |
| Tagged/stable versions | | ✅ |
| Testing latest features | ✅ | |
| CI/CD pipelines | ✅ | ✅ |
| Quick testing | ✅ | |
| Reproducible builds | | ✅ |

⚠️ **Warning:** Nightly builds are unstable and for testing only. Use tagged releases for production.

## Versioned Releases

### Building Versioned Release Images

To build a production-ready image using a specific tagged version of Synka.Web:

```bash
# Build using a specific Synka.Web release tag (e.g., v1.0.0)
docker build \
  --build-arg WEB_REPO=synka-org/Synka.Web \
  --build-arg WEB_RELEASE=v1.0.0 \
  --build-arg WEB_ASSET=synka-web.tar.gz \
  -t synka:1.0.0 \
  -f Dockerfile \
  .

# Or use the build script
./build-docker.sh \
  --tag v1.0.0 \
  --web-release v1.0.0
```

### Creating a Release Workflow

When creating a versioned release:

1. **Tag Synka.Web** with semantic version (e.g., `v1.0.0`)
2. **Tag Synka.Server** with matching version
3. **Build Docker image** using both tags:
   ```bash
   docker build \
     --build-arg WEB_RELEASE=v1.0.0 \
     --build-arg WEB_ASSET=synka-web.tar.gz \
     -t ghcr.io/synka-org/synka:1.0.0 \
     -t ghcr.io/synka-org/synka:latest \
     .
   ```
4. **Push to registry**:
   ```bash
   docker push ghcr.io/synka-org/synka:1.0.0
   docker push ghcr.io/synka-org/synka:latest
   ```

### Release Asset Naming

The Dockerfile expects different asset names for nightly vs versioned releases:

- **Nightly releases:** `synka-web-nightly.tar.gz`
- **Versioned releases:** `synka-web.tar.gz`

Ensure Synka.Web releases publish the frontend tarball with the correct name.

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_URLS` | `http://+:8080` | URLs the server listens on |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment |
| `Database__Provider` | `Sqlite` | Database provider (`Sqlite` or `Postgres`) |
| `ConnectionStrings__Sqlite` | `Data Source=/app/data/synka.db` | SQLite connection string |
| `ConnectionStrings__Postgres` | - | PostgreSQL connection string |

### Using PostgreSQL

To use PostgreSQL instead of SQLite:

```bash
docker run -d \
  -p 8080:8080 \
  -e Database__Provider=Postgres \
  -e ConnectionStrings__Postgres="Host=postgres;Port=5432;Database=synka;Username=synka;Password=SecurePassword" \
  synka:latest
```

Or with docker-compose, uncomment the PostgreSQL section in `docker-compose.yml`.

### Persistent Data

The container stores SQLite database in `/app/data`. Mount a volume to persist data:

```bash
# Named volume (recommended)
docker run -v synka-data:/app/data synka:latest

# Bind mount to host directory
docker run -v /path/on/host:/app/data synka:latest
```

## Health Checks

The container includes a health check that pings `/health` every 30 seconds:

```bash
# Check container health
docker ps

# View health check logs
docker inspect --format='{{json .State.Health}}' synka | jq
```

## Troubleshooting

### Build Fails: "Synka.Web directory not found"

Ensure both `Synka.Server` and `Synka.Web` are sibling directories. The build script will verify this before building.

### Container Starts but App Not Accessible

1. Check container logs:
   ```bash
   docker logs synka
   ```

2. Verify the container is healthy:
   ```bash
   docker ps
   ```

3. Ensure port 8080 is not already in use:
   ```bash
   netstat -tuln | grep 8080
   ```

### Database Migration Issues

The application automatically runs migrations on startup. If migrations fail:

1. Check logs for error details
2. Ensure the data directory is writable
3. For PostgreSQL, verify connection string and database exists

### Permission Denied Errors

The container runs as user `synka` (non-root). If you see permission errors:

1. Ensure mounted volumes have correct permissions
2. For bind mounts, the host directory should be writable by UID 999 (synka user)

## Advanced Usage

### Multi-Platform Builds

Build for multiple platforms:

```bash
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -f Synka.Server/Dockerfile \
  -t myregistry/synka:latest \
  --push \
  .
```

### Custom HTTPS Certificate

To use HTTPS:

1. Mount certificate files
2. Set environment variables:

```bash
docker run -d \
  -p 8080:8080 \
  -p 8081:8081 \
  -v /path/to/certs:/https:ro \
  -e ASPNETCORE_URLS="https://+:8081;http://+:8080" \
  -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/cert.pfx \
  -e ASPNETCORE_Kestrel__Certificates__Default__Password=YourPassword \
  synka:latest
```

### Using with Kubernetes

Example deployment:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: synka
spec:
  replicas: 1
  selector:
    matchLabels:
      app: synka
  template:
    metadata:
      labels:
        app: synka
    spec:
      containers:
      - name: synka
        image: synka:latest
        ports:
        - containerPort: 8080
        env:
        - name: Database__Provider
          value: Postgres
        - name: ConnectionStrings__Postgres
          valueFrom:
            secretKeyRef:
              name: synka-db
              key: connection-string
        volumeMounts:
        - name: data
          mountPath: /app/data
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 30
      volumes:
      - name: data
        persistentVolumeClaim:
          claimName: synka-data
```

## CI/CD Integration

### GitHub Actions

The included `.github/workflows/docker-build.yml` automatically:

- Builds on push to main branch
- Builds on tags (e.g., `v1.0.0`)
- Pushes to GitHub Container Registry
- Supports multi-platform builds
- Generates build attestations

To use:

1. Ensure `Synka.Web` is accessible (same org or configure PAT)
2. Push a tag to trigger release:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

### Using the Built Image

Pull and run the image from GitHub Container Registry:

```bash
docker pull ghcr.io/yourusername/synka:latest
docker run -d -p 8080:8080 -v synka-data:/app/data ghcr.io/yourusername/synka:latest
```

## Makefile Commands

Quick reference for common tasks:

```bash
make help           # Show all available commands
make build-docker   # Build Docker image
make run-docker     # Start with docker-compose
make stop           # Stop containers
make clean          # Clean build artifacts and stop containers
```

## Security Considerations

1. **Non-root user**: Container runs as `synka` user (UID 999)
2. **Minimal base image**: Uses official .NET and Node Alpine images
3. **No secrets in image**: All sensitive data via environment variables
4. **Health checks**: Enables automatic recovery in orchestration platforms
5. **Read-only root**: Consider using `--read-only` flag with tmpfs mounts

Example with security hardening:

```bash
docker run -d \
  --read-only \
  --tmpfs /tmp \
  --tmpfs /app/tmp \
  -v synka-data:/app/data \
  -p 8080:8080 \
  --security-opt=no-new-privileges \
  synka:latest
```

## Performance Tuning

### Resource Limits

Set resource limits for production:

```bash
docker run -d \
  --memory="512m" \
  --cpus="1.0" \
  -p 8080:8080 \
  synka:latest
```

### Database Connection Pooling

For PostgreSQL, adjust connection pool settings:

```bash
-e ConnectionStrings__Postgres="Host=postgres;Port=5432;Database=synka;Username=synka;Password=pwd;Minimum Pool Size=5;Maximum Pool Size=20"
```

## Support

For issues or questions:
- Check container logs: `docker logs synka`
- Review health status: `docker ps`
- Verify configuration: `docker inspect synka`
