# Multi-stage build for SmartUnderwrite API
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["SmartUnderwrite.Api/SmartUnderwrite.Api.csproj", "SmartUnderwrite.Api/"]
COPY ["SmartUnderwrite.Core/SmartUnderwrite.Core.csproj", "SmartUnderwrite.Core/"]
COPY ["SmartUnderwrite.Infrastructure/SmartUnderwrite.Infrastructure.csproj", "SmartUnderwrite.Infrastructure/"]
RUN dotnet restore "SmartUnderwrite.Api/SmartUnderwrite.Api.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/SmartUnderwrite.Api"
RUN dotnet build "SmartUnderwrite.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SmartUnderwrite.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "SmartUnderwrite.Api.dll"]