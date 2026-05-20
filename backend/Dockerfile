FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY CoachOS.slnx .
COPY CoachOS.Domain/CoachOS.Domain.csproj CoachOS.Domain/
COPY CoachOS.Application/CoachOS.Application.csproj CoachOS.Application/
COPY CoachOS.Infrastructure/CoachOS.Infrastructure.csproj CoachOS.Infrastructure/
COPY CoachOS.API/CoachOS.API.csproj CoachOS.API/
COPY CoachOS.Tests/CoachOS.Tests.csproj CoachOS.Tests/
RUN dotnet restore CoachOS.slnx

# Copy everything and build
COPY . .

# Remove directories with literal backslashes in their names (created by dotnet-ef on macOS).
# These break MSBuild glob expansion on Linux, causing MSB3552.
RUN find . -type d -name '*\\*' -exec rm -rf {} + 2>/dev/null || true

RUN dotnet publish CoachOS.API/CoachOS.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# aspnet:10.0 ships with a non-root "app" user (uid 1654); just use it.
# WORKDIR creates /app owned by root — chown it so the app user can create
# subdirectories at runtime (e.g. LodeKennes.Extensions.Scaleway.SecretManager
# caches fetched secrets to /app/lk-scw-secrets).
COPY --from=build --chown=app:app /app .
RUN chown app:app /app

# DataProtection keystore. Mollie OAuth tokens are encrypted at rest with keys
# stored here; container restarts must not invalidate them. In production a
# named docker volume is mounted on this path (see infrastructure/docker-compose.yml).
# Pre-creating the dir with app ownership ensures the non-root user can write
# when docker creates an empty named volume on first start.
RUN mkdir -p /data-protection-keys && chown app:app /data-protection-keys

USER app

ENV ASPNETCORE_URLS=http://+:5142
EXPOSE 5142

ENTRYPOINT ["dotnet", "CoachOS.API.dll"]
