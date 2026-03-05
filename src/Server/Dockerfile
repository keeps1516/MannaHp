# Build context should be the repo root (../../ from this file)
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files first for layer caching
COPY src/Shared/MannaHp.Shared.csproj src/Shared/
COPY src/Server/MannaHp.Server.csproj src/Server/
RUN dotnet restore src/Server/MannaHp.Server.csproj -p:Configuration=Docker

# Copy source code
COPY src/Shared/ src/Shared/
COPY src/Server/ src/Server/

# Publish with Docker configuration (excludes Blazor)
RUN dotnet publish src/Server/MannaHp.Server.csproj -c Docker -o /app

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "MannaHp.Server.dll"]
