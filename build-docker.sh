#!/bin/bash
set -e

echo "🔨 Building Synka Docker Image"
echo "================================"

# Get the directory where the script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# The build context needs to be the parent directory
BUILD_CONTEXT="$(dirname "$SCRIPT_DIR")"
cd "$BUILD_CONTEXT"

# Verify Synka.Server directory exists
if [ ! -d "Synka.Server" ]; then
  echo "❌ Error: Synka.Server directory not found at $BUILD_CONTEXT/Synka.Server"
  exit 1
fi

# Default values
IMAGE_NAME="synka"
IMAGE_TAG="latest"
PLATFORM="linux/amd64"
WEB_REPO="synka-org/Synka.Web"
WEB_RELEASE="nightly"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --tag|-t)
      IMAGE_TAG="$2"
      shift 2
      ;;
    --name|-n)
      IMAGE_NAME="$2"
      shift 2
      ;;
    --platform|-p)
      PLATFORM="$2"
      shift 2
      ;;
    --web-repo)
      WEB_REPO="$2"
      shift 2
      ;;
    --web-release)
      WEB_RELEASE="$2"
      shift 2
      ;;
    --push)
      PUSH=true
      shift
      ;;
    --help|-h)
      echo "Usage: ./build-docker.sh [options]"
      echo ""
      echo "Options:"
      echo "  --tag, -t <tag>           Tag for the Docker image (default: latest)"
      echo "  --name, -n <name>         Name for the Docker image (default: synka)"
      echo "  --platform, -p <platform> Platform to build for (default: linux/amd64)"
      echo "  --web-repo <repo>         Synka.Web repository (default: synka-org/Synka.Web)"
      echo "  --web-release <release>   Synka.Web release tag to use (default: nightly)"
      echo "  --push                    Push the image after building"
      echo "  --help, -h                Show this help message"
      echo ""
      echo "Examples:"
      echo "  ./build-docker.sh --tag v1.0.0 --push"
      echo "  ./build-docker.sh --web-release nightly --tag nightly"
      echo "  ./build-docker.sh --web-release v1.0.0 --tag v1.0.0"
      echo ""
      echo "Note: This script builds using pre-built Synka.Web releases from GitHub."
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      echo "Use --help for usage information"
      exit 1
      ;;
  esac
done

FULL_IMAGE_NAME="${IMAGE_NAME}:${IMAGE_TAG}"

echo "Build context: $BUILD_CONTEXT"
echo "Building: $FULL_IMAGE_NAME"
echo "Platform: $PLATFORM"
echo "Web repository: $WEB_REPO"
echo "Web release: $WEB_RELEASE"
echo ""

# Determine asset name based on release type
if [ "$WEB_RELEASE" = "nightly" ]; then
  WEB_ASSET="synka-web-nightly.tar.gz"
else
  WEB_ASSET="synka-web.tar.gz"
fi

# Build the Docker image
docker build \
  --platform "$PLATFORM" \
  --tag "$FULL_IMAGE_NAME" \
  --file Synka.Server/Dockerfile \
  --build-arg WEB_REPO="$WEB_REPO" \
  --build-arg WEB_RELEASE="$WEB_RELEASE" \
  --build-arg WEB_ASSET="$WEB_ASSET" \
  .

echo ""
echo "✅ Build complete: $FULL_IMAGE_NAME"

# Push if requested
if [ "$PUSH" = true ]; then
  echo ""
  echo "📤 Pushing image to registry..."
  docker push "$FULL_IMAGE_NAME"
  echo "✅ Push complete"
fi

echo ""
echo "🚀 To run the container:"
echo "   docker run -p 8080:8080 -v synka-data:/app/data $FULL_IMAGE_NAME"
echo ""
echo "🐳 To run with docker-compose:"
echo "   cd Synka.Server && docker-compose up -d"
