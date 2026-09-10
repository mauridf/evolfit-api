# EvolFit — Backend

> API REST de fitness e saúde para academias, personal trainers e usuários individuais.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)](https://www.postgresql.org)
[![License](https://img.shields.io/badge/license-Proprietary-red)](#licença)

---

## 📖 Sobre

O **EvolFit** permite:

- 📊 Registrar métricas de saúde (peso, altura, IMC, BMR, TDEE) via **TinyFn Health API**
- 🏋️ Gerar rotinas de treino personalizadas via **wger API**
- ✅ Acompanhar exercícios diários com marcação de conclusão
- 📈 Visualizar evolução no dashboard

Documentação técnica completa: ver `docs/`.

---

## 🛠️ Stack

| Camada | Tecnologia |
|---|---|
| Backend | .NET 10 + C# 13 + ASP.NET Core |
| Arquitetura | Clean Architecture + DDD + Monólito Modular |
| Banco | PostgreSQL 16 + EF Core 10 + DbUp |
| Auth | JWT + BCrypt.Net |
| Validação | FluentValidation |
| Logging | Serilog + Seq (dev) |
| Docs API | Scalar |
| Testes | xUnit + FluentAssertions + NSubstitute + Testcontainers |
| Deploy | Docker + Render |

---

## 🚀 Como rodar local

```bash
# 1. Subir dependências (PostgreSQL + Seq)
docker compose up -d

# 2. Aplicar migrations
dotnet run --project src/EvolFit.Migrations

# 3. Rodar a API
dotnet run --project src/EvolFit.Api

# 4. Acessar documentação interativa
# http://localhost:5000/scalar
```

---

## 📁 Estrutura

```
EvolFit/
├── src/
│   ├── EvolFit.Api/              # ASP.NET Core Web API
│   ├── EvolFit.Core/             # Domínio (entidades, enums)
│   ├── EvolFit.Application/      # Features + serviços + DTOs
│   ├── EvolFit.Infrastructure/   # EF Core, repos, HTTP clients
│   └── EvolFit.Migrations/       # DbUp (console app)
├── tests/
│   ├── EvolFit.UnitTests/
│   └── EvolFit.IntegrationTests/
├── db/migrations/                # Scripts SQL versionados
├── docs/                         # Documentação técnica
├── deploy/                       # Dockerfiles + render.yaml
└── docker-compose.yml
```

---

## 📄 Licença

Proprietário. Todos os direitos reservados.