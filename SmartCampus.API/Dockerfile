# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SmartCampus.API/SmartCampus.API.csproj", "SmartCampus.API/"]
COPY ["SmartCampus.Business/SmartCampus.Business.csproj", "SmartCampus.Business/"]
COPY ["SmartCampus.DataAccess/SmartCampus.DataAccess.csproj", "SmartCampus.DataAccess/"]
COPY ["SmartCampus.Entities/SmartCampus.Entities.csproj", "SmartCampus.Entities/"]
RUN dotnet restore "SmartCampus.API/SmartCampus.API.csproj"
COPY . .
WORKDIR "/src/SmartCampus.API"
RUN dotnet build "SmartCampus.API.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "SmartCampus.API.csproj" -c Release -o /app/publish

# Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartCampus.API.dll"]
