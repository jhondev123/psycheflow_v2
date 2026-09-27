# syntax=docker/dockerfile:1

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os arquivos de projeto (melhor cache de camadas)
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Psycheflow.Api/Psycheflow.Api.csproj src/Psycheflow.Api/
RUN dotnet restore src/Psycheflow.Api/Psycheflow.Api.csproj

COPY src/ src/
RUN dotnet publish src/Psycheflow.Api/Psycheflow.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Fusos IANA (America/Sao_Paulo) para o horário local da clínica
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Anexos de prontuário (montado como volume no compose)
RUN mkdir -p /app/storage && chown -R $APP_UID /app/storage
USER $APP_UID

HEALTHCHECK --interval=15s --timeout=3s --retries=5 CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Psycheflow.Api.dll"]
