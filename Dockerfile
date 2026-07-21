# ── Stage 1: Build ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copier les fichiers de projet pour restaurer les packages (couche cache)
COPY ["src/Formations.Domain/Formations.Domain.csproj", "src/Formations.Domain/"]
COPY ["src/Formations.Application/Formations.Application.csproj", "src/Formations.Application/"]
COPY ["src/Formations.Infrastructure/Formations.Infrastructure.csproj", "src/Formations.Infrastructure/"]
COPY ["src/Formations.Api/Formations.Api.csproj", "src/Formations.Api/"]
RUN dotnet restore "src/Formations.Api/Formations.Api.csproj"

# Copier tout le code source
COPY . .

# Build Release
WORKDIR "/src/src/Formations.Api"
RUN dotnet build "Formations.Api.csproj" -c Release -o /app/build

# ── Stage 2: Publish ────────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "Formations.Api.csproj" -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ── Stage 3: Runtime ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Créer un utilisateur non-root pour la sécurité
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=publish --chown=appuser:appuser /app/publish .

# Créer le répertoire des logs
RUN mkdir -p /app/logs

EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Formations.Api.dll"]
