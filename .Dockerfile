# ============================================================
# EvolFit — Dockerfile de produção (multi-stage)
# ============================================================
# Estágio 1: build + publish
# Estágio 2: runtime enxuto, non-root, porta 10000 (Render)
# ============================================================

# ---------- Stage 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia apenas csproj/sln para aproveitar cache de restore
COPY EvolFit.slnx ./
COPY src/EvolFit.Api/EvolFit.Api.csproj                        src/EvolFit.Api/
COPY src/EvolFit.Core/EvolFit.Core.csproj                      src/EvolFit.Core/
COPY src/EvolFit.Application/EvolFit.Application.csproj        src/EvolFit.Application/
COPY src/EvolFit.Infrastructure/EvolFit.Infrastructure.csproj  src/EvolFit.Infrastructure/
COPY src/EvolFit.Migrations/EvolFit.Migrations.csproj          src/EvolFit.Migrations/

RUN dotnet restore src/EvolFit.Api/EvolFit.Api.csproj
RUN dotnet restore src/EvolFit.Migrations/EvolFit.Migrations.csproj

# Copia o resto do código
COPY . .

# Publica a API
RUN dotnet publish src/EvolFit.Api/EvolFit.Api.csproj \
    -c Release -o /app/api /p:UseAppHost=false

# Publica o runner de migrations
RUN dotnet publish src/EvolFit.Migrations/EvolFit.Migrations.csproj \
    -c Release -o /app/migrations /p:UseAppHost=false

# ---------- Stage 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Usuário não-root (SEC)
RUN addgroup --system --gid 1001 evolfit \
 && adduser --system --uid 1001 --ingroup evolfit evolfit

# Copia artefatos com ownership do usuário
COPY --from=build --chown=evolfit:evolfit /app/api /app/api
COPY --from=build --chown=evolfit:evolfit /app/migrations /app/migrations

# Configurações de produção
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:10000
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

USER evolfit

EXPOSE 10000

ENTRYPOINT ["dotnet", "/app/api/EvolFit.Api.dll"]