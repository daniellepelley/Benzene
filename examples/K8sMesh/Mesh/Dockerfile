# Build context is the repo root (the project references src/ directly).
#   docker build -f examples/K8sMesh/Mesh/Dockerfile -t benzene-k8smesh-mesh:local .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish examples/K8sMesh/Mesh/Benzene.Examples.K8sMesh.Mesh.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Benzene.Examples.K8sMesh.Mesh.dll"]
