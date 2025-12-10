#!/bin/bash

# Build and publish the project
echo "Building SmartCampus API..."
dotnet restore
dotnet publish SmartCampus.API/SmartCampus.API.csproj -c Release -o /app/publish

# Run the application
echo "Starting SmartCampus API..."
cd /app/publish
dotnet SmartCampus.API.dll

