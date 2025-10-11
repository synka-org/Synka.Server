.PHONY: help build build-docker run run-docker stop clean test restore

# Default target
help:
	@echo "Synka Server - Makefile Commands"
	@echo "================================="
	@echo ""
	@echo "Development:"
	@echo "  make restore      - Restore .NET dependencies"
	@echo "  make build        - Build the .NET solution"
	@echo "  make run          - Run the server locally"
	@echo "  make test         - Run tests"
	@echo ""
	@echo "Docker:"
	@echo "  make build-docker - Build Docker image"
	@echo "  make run-docker   - Run container with docker-compose"
	@echo "  make stop         - Stop Docker containers"
	@echo "  make clean        - Clean build artifacts and stop containers"
	@echo ""
	@echo "Example:"
	@echo "  make build-docker && make run-docker"

# .NET Development Commands
restore:
	dotnet restore Synka.slnx

build: restore
	dotnet build Synka.slnx --no-restore

run: restore
	dotnet run --project src/Synka.Server

test:
	dotnet test Synka.slnx

# Docker Commands
build-docker:
	@echo "Building Synka Docker image..."
	./build-docker.sh

run-docker:
	@echo "Starting Synka with docker-compose..."
	docker-compose up -d
	@echo ""
	@echo "✅ Synka is running at http://localhost:8080"
	@echo "   View logs: docker-compose logs -f"

stop:
	@echo "Stopping Docker containers..."
	docker-compose down

# Clean up
clean:
	@echo "Cleaning build artifacts..."
	dotnet clean Synka.slnx
	rm -rf src/Synka.Server/bin src/Synka.Server/obj
	rm -rf tests/Synka.Server.Tests/bin tests/Synka.Server.Tests/obj
	@echo "Stopping Docker containers..."
	docker-compose down -v
	@echo "✅ Clean complete"
