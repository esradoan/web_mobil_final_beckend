# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first (for better layer caching)
COPY ["SmartCampus.API/SmartCampus.API.csproj", "SmartCampus.API/"]
COPY ["SmartCampus.Business/SmartCampus.Business.csproj", "SmartCampus.Business/"]
COPY ["SmartCampus.DataAccess/SmartCampus.DataAccess.csproj", "SmartCampus.DataAccess/"]
COPY ["SmartCampus.Entities/SmartCampus.Entities.csproj", "SmartCampus.Entities/"]

# Restore dependencies
RUN dotnet restore "SmartCampus.API/SmartCampus.API.csproj"

# Copy source code (excluding test projects via .dockerignore)
COPY SmartCampus.API/ ./SmartCampus.API/
COPY SmartCampus.Business/ ./SmartCampus.Business/
COPY SmartCampus.DataAccess/ ./SmartCampus.DataAccess/
COPY SmartCampus.Entities/ ./SmartCampus.Entities/

# Build and publish only the API project (not the solution)
WORKDIR "/src/SmartCampus.API"
RUN dotnet build "SmartCampus.API.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "SmartCampus.API.csproj" -c Release -o /app/publish

# Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartCampus.API.dll"]
