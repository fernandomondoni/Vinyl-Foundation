FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.Application/Vinyl.Identity.Application.csproj src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.Application/
COPY src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.Adapters/Vinyl.Identity.Adapters.csproj src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.Adapters/
COPY src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.API/Vinyl.Identity.API.csproj src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.API/

RUN dotnet restore src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.API/Vinyl.Identity.API.csproj

COPY src/backend/Vinyl/Vinyl.Identity/ src/backend/Vinyl/Vinyl.Identity/
RUN dotnet publish src/backend/Vinyl/Vinyl.Identity/Vinyl.Identity.API/Vinyl.Identity.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Vinyl.Identity.API.dll"]
