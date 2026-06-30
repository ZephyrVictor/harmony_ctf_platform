# Multi-stage build for GZCTF with Flag Scoring Module
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Install Node.js for frontend build
RUN apk add --no-cache nodejs npm && npm install -g pnpm@10

# Restore backend
COPY src/Directory.Packages.props src/
COPY src/global.json src/
COPY src/GZCTF/*.csproj src/GZCTF/
RUN dotnet restore src/GZCTF/GZCTF.csproj

# Copy source
COPY . .

# Optional sub-path base for the SPA (e.g. /harmonyctf/).
# Must match the PathBase configured on the backend at runtime.
ARG VITE_BASE_PATH=/
ENV VITE_BASE_PATH=$VITE_BASE_PATH

# Build and publish
RUN dotnet publish src/GZCTF/GZCTF.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    ASPNETCORE_URLS=http://+:8080

WORKDIR /app

RUN apk add --no-cache wget libpcap icu-data-full icu-libs \
    ca-certificates libgdiplus tzdata krb5-libs && \
    update-ca-certificates

COPY --from=build --chown=$APP_UID:$APP_UID /app .

EXPOSE 8080

HEALTHCHECK --interval=5m --timeout=3s --start-period=10s --retries=1 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "GZCTF.dll"]
