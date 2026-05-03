#!/bin/sh
# Workaround for an SDK 10.0.203 bug: cold builds of CoachOS.Infrastructure
# literalize default-item globs (**/*.resx, **/*.cs) and fail with MSB3552 /
# CS2001. The first `dotnet build` warms Domain + Application before failing
# on Infrastructure (acceptable). With those prereqs warm, a standalone
# Infrastructure build then succeeds — which warms enough state for the
# subsequent `dotnet watch run` graph build to work.
cd /src
dotnet build CoachOS.API/CoachOS.API.csproj || true
dotnet build CoachOS.Infrastructure/CoachOS.Infrastructure.csproj
exec dotnet watch run --project CoachOS.API/CoachOS.API.csproj --no-launch-profile
