# Multi-stage Dockerfile for .NET Core 8 Backend API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["Clausio.Legal.API/Clausio.Legal.API.csproj", "Clausio.Legal.API/"]
COPY ["Clausio.Legal.Service/Clausio.Legal.Service.csproj", "Clausio.Legal.Service/"]
COPY ["Clausio.Legal.Infrastructure/Clausio.Legal.Infrastructure.csproj", "Clausio.Legal.Infrastructure/"]
COPY ["Clausio.Legal.Core/Clausio.Legal.Core.csproj", "Clausio.Legal.Core/"]
COPY ["Clausio.MCP/Clausio.MCP.csproj", "Clausio.MCP/"]
COPY ["Clausio.Legal.Cache/Clausio.Legal.Cache.csproj", "Clausio.Legal.Cache/"]

RUN dotnet restore "Clausio.Legal.API/Clausio.Legal.API.csproj"

# Copy full source and build release binary
COPY . .
WORKDIR "/src/Clausio.Legal.API"
RUN dotnet build "Clausio.Legal.API.csproj" -c Release -o /app/build
RUN dotnet publish "Clausio.Legal.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5123
ENTRYPOINT ["dotnet", "Clausio.Legal.API.dll"]
