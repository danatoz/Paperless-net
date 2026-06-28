# Multi-stage build for Paperless .NET API
#
# Build:   mcr.microsoft.com/dotnet/sdk:10.0
# Runtime: mcr.microsoft.com/dotnet/aspnet:10.0
#
# Usage:
#   docker build -t paperless-api -f dotnet/Dockerfile .

# ==============================================================
# Stage 1: Build
# ==============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

# Copy solution-level files
COPY Directory.Build.props .
COPY Paperless.slnx .

# Copy project files (.csproj) — preserve directory structure for restore
# Source projects
COPY src/Paperless.Shared/Paperless.Shared.csproj src/Paperless.Shared/
COPY src/Paperless.Core/Paperless.Core.csproj src/Paperless.Core/
COPY src/Paperless.Infrastructure/Paperless.Infrastructure.csproj src/Paperless.Infrastructure/
COPY src/Paperless.Api/Paperless.Api.csproj src/Paperless.Api/
COPY src/Paperless.Mail/Paperless.Mail.csproj src/Paperless.Mail/
COPY src/Paperless.AI/Paperless.AI.csproj src/Paperless.AI/

# Test projects
COPY tests/Paperless.Shared.Tests/Paperless.Shared.Tests.csproj tests/Paperless.Shared.Tests/
COPY tests/Paperless.Core.Tests/Paperless.Core.Tests.csproj tests/Paperless.Core.Tests/
COPY tests/Paperless.Infrastructure.Tests/Paperless.Infrastructure.Tests.csproj tests/Paperless.Infrastructure.Tests/
COPY tests/Paperless.Api.Tests/Paperless.Api.Tests.csproj tests/Paperless.Api.Tests/
COPY tests/Paperless.AI.Tests/Paperless.AI.Tests.csproj tests/Paperless.AI.Tests/

# Restore NuGet packages (cached separately for layer caching)
RUN dotnet restore Paperless.slnx

# Copy all remaining source files
COPY src/ src/
COPY tests/ tests/

# Build all projects in Release mode
# NOTE: --no-restore is intentionally omitted here due to NETSDK1064
# in Docker builds with .NET 10 SDK. Restore is cached via the
# project-file-only COPY layers above.
RUN dotnet build -c Release

# Publish the API project to /app/publish
RUN dotnet publish src/Paperless.Api/Paperless.Api.csproj \
    -c Release \
    -o /app/publish

# ==============================================================
# Stage 2: Runtime
# ==============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Install system dependencies:
#   - tesseract-ocr: OCR engine for text extraction
#   - ocrmypdf: PDF/A generation with OCR layer
#   - curl: health check probe
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        tesseract-ocr \
        tesseract-ocr-eng \
        tesseract-ocr-rus \
        ocrmypdf \
        curl \
        && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# ASP.NET Core listens on port 8000 (matches paperless-ngx convention)
EXPOSE 8000

ENV ASPNETCORE_URLS=http://+:8000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check — requires a /health endpoint in the API
# (implemented in task M8-03; will return 502 until then)
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8000/health || exit 1

ENTRYPOINT ["dotnet", "Paperless.Api.dll"]
