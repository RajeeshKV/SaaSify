#!/usr/bin/env sh
set -e

export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://0.0.0.0:${PORT:-10000}}"

if [ "${RUN_MIGRATIONS:-true}" = "true" ]; then
  echo "Applying database migrations..."
  /app/efbundle
fi

exec dotnet /app/WebAPI.dll
