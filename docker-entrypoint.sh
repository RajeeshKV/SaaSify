#!/usr/bin/env sh
set -e

export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://0.0.0.0:${PORT:-10000}}"

if [ "${RUN_MIGRATIONS:-true}" = "true" ]; then
  echo "Applying database migrations..."
  dotnet ef database update \
    --project /src/Infrastructure/Infrastructure.csproj \
    --startup-project /src/WebAPI/WebAPI.csproj \
    --configuration Release
fi

exec dotnet /app/WebAPI.dll
