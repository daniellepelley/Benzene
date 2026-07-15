# Build context is the repository root - this project references sibling `src/` packages by
# relative path, so the whole repo needs to be in the build context:
#   docker build -f examples/Google/Benzene.Examples.Google/Dockerfile -t benzene-google-example .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish examples/Google/Benzene.Examples.Google/Benzene.Examples.Google.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Cloud Run injects PORT; Program.cs reads it and binds Kestrel to it.
ENTRYPOINT ["dotnet", "Benzene.Examples.Google.dll"]
