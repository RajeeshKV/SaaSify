# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MultiTenantSaaS.sln ./
COPY Domain/Domain.csproj Domain/
COPY Application/Application.csproj Application/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY WebAPI/WebAPI.csproj WebAPI/

RUN dotnet restore WebAPI/WebAPI.csproj

COPY . .

RUN dotnet publish WebAPI/WebAPI.csproj -c Release -o /app/publish --no-restore
RUN dotnet tool install --global dotnet-ef --version 8.* \
    && /root/.dotnet/tools/dotnet-ef migrations bundle \
        --project Infrastructure/Infrastructure.csproj \
        --startup-project WebAPI/WebAPI.csproj \
        --configuration Release \
        --runtime linux-x64 \
        --self-contained false \
        --output /app/publish/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=10000
EXPOSE 10000

COPY --from=build /app/publish .
COPY docker-entrypoint.sh /app/docker-entrypoint.sh

RUN chmod +x /app/docker-entrypoint.sh /app/efbundle

ENTRYPOINT ["/app/docker-entrypoint.sh"]
