# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only the main project
COPY AuthApi/*.csproj AuthApi/

# Restore only the API project (ignore test projects)
RUN dotnet restore AuthApi/AuthApi.csproj

# Copy the full API project
COPY AuthApi/ AuthApi/

WORKDIR /src/AuthApi

# Publish the API project
RUN dotnet publish AuthApi.csproj -c Release -o /app/publish

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

EXPOSE 8080

# Tell ASP.NET to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Start the API
ENTRYPOINT ["dotnet", "AuthApi.dll"]