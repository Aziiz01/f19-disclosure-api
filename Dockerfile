# syntax=docker/dockerfile:1.7

# --- build stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore: copy only csproj/sln first to maximize Docker layer caching.
COPY DisclosureEngine.sln ./
COPY src/DisclosureEngine.Domain/DisclosureEngine.Domain.csproj                   src/DisclosureEngine.Domain/
COPY src/DisclosureEngine.Application/DisclosureEngine.Application.csproj         src/DisclosureEngine.Application/
COPY src/DisclosureEngine.Infrastructure/DisclosureEngine.Infrastructure.csproj   src/DisclosureEngine.Infrastructure/
COPY src/DisclosureEngine.Api/DisclosureEngine.Api.csproj                         src/DisclosureEngine.Api/
COPY tests/DisclosureEngine.UnitTests/DisclosureEngine.UnitTests.csproj           tests/DisclosureEngine.UnitTests/
COPY tests/DisclosureEngine.IntegrationTests/DisclosureEngine.IntegrationTests.csproj tests/DisclosureEngine.IntegrationTests/
RUN dotnet restore DisclosureEngine.sln

# Build & publish the API.
COPY . .
RUN dotnet publish src/DisclosureEngine.Api/DisclosureEngine.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# --- runtime stage ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Run as a non-root user provided by the base image.
USER app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080
ENTRYPOINT ["dotnet", "DisclosureEngine.Api.dll"]
