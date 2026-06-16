# syntax=docker/dockerfile:1

ARG BUILD_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG BASE_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra

# --- build stage: runs natively on the builder arch, cross-publishes for target ---
FROM --platform=$BUILDPLATFORM ${BUILD_IMAGE} AS build
ARG TARGETARCH
ARG VERSION=0.1.0-dev
ARG BUILDKIT_SBOM_SCAN_STAGE=true
WORKDIR /src

# Copy build infrastructure + project files first for restore-layer caching.
COPY global.json Directory.Build.props Directory.Build.targets Directory.Packages.props ./
COPY eng/ eng/
COPY src/Directory.Build.props src/Directory.Build.targets ./src/
COPY src/Server.Api/Server.Api.csproj               src/Server.Api/
COPY src/Application/Application.csproj              src/Application/
COPY src/Domain/Domain.csproj                        src/Domain/
COPY src/Infrastructure/Infrastructure.csproj        src/Infrastructure/
COPY src/Server.Mcp.Shared/Server.Mcp.Shared.csproj  src/Server.Mcp.Shared/

RUN dotnet restore src/Server.Api/Server.Api.csproj -a $TARGETARCH

# Copy the rest of the source and publish (framework-dependent).
COPY src/ src/
RUN dotnet publish src/Server.Api/Server.Api.csproj \
    -a $TARGETARCH -c Release --no-restore --self-contained false -o /app \
    -p:Version=$VERSION \
    -p:OpenApiGenerateDocumentsOnBuild=false

# --- runtime stage: minimal, non-root (UID 1654), no shell ---
FROM ${BASE_IMAGE} AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "ssmsmcp.Server.Api.dll"]
