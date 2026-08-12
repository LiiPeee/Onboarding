# Onboarding — API de Contas

API de onboarding de contas bancárias construída em **.NET 8** com **PostgreSQL**, seguindo os princípios de **DDD**, **SOLID**, **MVC** e **Clean Code**. A task exige CRUD de contas com validação de CPF, cache diário e publicação de eventos de domínio via padrão **Outbox**.

## Stack

- .NET 8 (ASP.NET Core Web API)
- Entity Framework Core 8 + Npgsql (PostgreSQL)
- xUnit + Moq + FluentAssertions (testes unitários)
- Docker Compose (PostgreSQL 16)

## Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│  Onboarding.WebApi  (Controllers, DTOs, Mappers, Middleware) │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────┐
│  Onboarding.Application  (AccountService, CpfValidator, DTOs)│
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────┐
│  Onboarding.Domain  (Account, OutboxEvent, contratos)        │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────┐
│  Onboarding.Data  (AppDbContext, mapeamentos EF)             │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────┐
│  Onboarding.Infrastructure  (Repos EF, UoW, Cache, Outbox)   │
└─────────────────────────────────────────────────────────────┘
```

## Como rodar

1. Suba o PostgreSQL:

```bash
docker compose up -d
```

2. Rode a API (a migration é aplicada automaticamente em Development):

```bash
dotnet run --project src/Onboarding.WebApi
```

A API sobe em `http://localhost:5000` e o Swagger em `http://localhost:5000/swagger`.

## Endpoints

| Método | Rota                | Descrição                          |
|--------|---------------------|------------------------------------|
| POST   | `/api/accounts`     | Cria uma conta (201)               |
| GET    | `/api/accounts`     | Lista todas as contas (200)        |
| GET    | `/api/accounts/{id}`| Busca uma conta por id (200/404)   |
| PUT    | `/api/accounts/{id}`| Atualiza nome/status (200/404)     |
| DELETE | `/api/accounts/{id}`| Remove uma conta (204/404)         |

Exemplos:

```bash
# Criar
curl -X POST http://localhost:5000/api/accounts \
  -H "Content-Type: application/json" \
  -d '{"name":"Felipe","cpf":"529.982.247-25"}'

# Listar
curl http://localhost:5000/api/accounts

# Buscar por id
curl http://localhost:5000/api/accounts/1

# Atualizar
curl -X PUT http://localhost:5000/api/accounts/1 \
  -H "Content-Type: application/json" \
  -d '{"name":"Felipe N.","status":"Inativa"}'

# Deletar
curl -X DELETE http://localhost:5000/api/accounts/1
```

## Testes

```bash
dotnet test tests/Onboarding.Tests
```

## Decisões de design

- **Outbox pattern**: cada mutação (create/update/delete) grava um evento em `outbox_events` na mesma transação da conta. Um `BackgroundService` (`OutboxProcessorService`) publica os eventos pendentes a cada 5s para consumidores (fraud-prevention, cards).
- **Cache diário**: `CachedAccountRepository` (decorator sobre `IAccountRepository`) cacheia contas por id até o fim do dia, invalidando o cache em update/delete.
- **EF Core + UnitOfWork**: repositórios sobre `AppDbContext` com `IUnitOfWork` para commit transacional.
- **Validação de CPF**: `CpfValidator` aceita CPF com ou sem máscara e valida os dígitos verificadores.
