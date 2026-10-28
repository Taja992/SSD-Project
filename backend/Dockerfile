# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS build
WORKDIR /src

# Project files and lock files FIRST, then restore. Docker caches each step,
# so while these files are unchanged the slow restore is skipped - even when
# the source code changes.
COPY global.json ./
COPY src/Vms.Domain/Vms.Domain.csproj                 src/Vms.Domain/packages.lock.json                 src/Vms.Domain/
COPY src/Vms.Infrastructure/Vms.Infrastructure.csproj src/Vms.Infrastructure/packages.lock.json src/Vms.Infrastructure/
COPY src/Vms.Api/Vms.Api.csproj                       src/Vms.Api/packages.lock.json                       src/Vms.Api/

# --locked-mode: fail if anything would resolve differently from the
# reviewed lock files (T-SC-01). Same rule as CI.
RUN dotnet restore src/Vms.Api/Vms.Api.csproj --locked-mode

COPY src/ src/
RUN dotnet publish src/Vms.Api/Vms.Api.csproj --configuration Release --output /app --no-restore

# =====================================================================
# Runtime stage
# =====================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4 AS runtime
WORKDIR /app
COPY --from=build /app .

# Non-root. The base image ships an unprivileged "app" user (UID 1654) and
# exposes its id as $APP_UID. If the API is compromised, the process cannot
# rewrite its own binaries or the OS inside the container.
USER $APP_UID

# .NET 8+ images listen on 8080, not 80 - binding below 1024 needs root.
EXPOSE 8080
ENTRYPOINT ["dotnet", "Vms.Api.dll"]
