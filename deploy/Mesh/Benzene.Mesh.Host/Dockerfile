# Build from the repo root: docker build -f deploy/Mesh/Benzene.Mesh.Host/Dockerfile .
# (this project's ProjectReferences reach up into src/, so the build context must be the repo
# root, not this folder - same reason examples/Cloudflare/Dockerfile does the same thing.)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish deploy/Mesh/Benzene.Mesh.Host/Benzene.Mesh.Host.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# curl isn't in the base aspnet image; only pulled in for the HEALTHCHECK below.
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
VOLUME /data/mesh-artifacts
HEALTHCHECK --interval=30s --timeout=5s CMD curl -f http://localhost:8080/mesh-ui || exit 1
ENTRYPOINT ["dotnet", "Benzene.Mesh.Host.dll"]
