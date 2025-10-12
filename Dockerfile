# syntax=docker/dockerfile:1

# ============================================================================
# Build arguments for frontend source
# ============================================================================
ARG WEB_REPO=synka-org/Synka.Web
ARG WEB_RELEASE=nightly

# ============================================================================
# Stage 1: Download pre-built frontend release
# ============================================================================
FROM alpine:latest AS web-release

ARG WEB_REPO
ARG WEB_RELEASE

RUN apk add --no-cache wget && \
    wget -O synka-web.tar.gz \
      "https://github.com/${WEB_REPO}/releases/download/${WEB_RELEASE}/synka-web-${WEB_RELEASE}.tar.gz" && \
    mkdir -p /web/dist/synka/browser && \
    tar -xzf synka-web.tar.gz -C /web/dist/synka/browser

# ============================================================================
# Stage 2: Build the .NET backend
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS backend-build

WORKDIR /src

# Copy solution and project files
COPY Directory.Build.props global.json dotnet-tools.json Synka.slnx ./
COPY src/Synka.Server/Synka.Server.csproj ./src/Synka.Server/
COPY tests/Synka.Server.Tests/Synka.Server.Tests.csproj ./tests/Synka.Server.Tests/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY src/ ./src/
COPY tests/ ./tests/

# Build and publish the application
RUN dotnet publish src/Synka.Server/Synka.Server.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ============================================================================
# Stage 3: Final runtime image
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final

WORKDIR /app

# Install SQLite runtime (needed for SQLite provider)
RUN apt-get update && \
    apt-get install -y sqlite3 libsqlite3-0 && \
    rm -rf /var/lib/apt/lists/*

# Create a non-root user
RUN groupadd -r synka && useradd -r -g synka synka

# Copy published .NET application
COPY --from=backend-build /app/publish .

# Copy Angular built files into wwwroot
COPY --from=web-release /web/dist/synka/browser ./wwwroot

# Create directory for SQLite database with proper permissions
RUN mkdir -p /app/data && chown -R synka:synka /app/data

# Switch to non-root user
USER synka

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Database__Provider=Sqlite
ENV ConnectionStrings__Sqlite=Data Source=/app/data/synka.db

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Synka.Server.dll"]
