# Onboarding — API de Contas

API de onboarding de contas bancárias construída em **.NET 8** com **PostgreSQL**, seguindo os princípios de **DDD**, **SOLID**, **MVC** e **Clean Code**. A task exige CRUD de contas com validação de CPF, cache diário e publicação de eventos de domínio via padrão **Outbox**.

## Stack

- .NET 8 (ASP.NET Core Web API)
- Entity Framework Core 8 + Npgsql (PostgreSQL)
- Redis 7 (cache distribuído via `IDistributedCache`)
- xUnit + Moq + FluentAssertions (testes unitários)
- Docker Compose (PostgreSQL 16 + Redis 7)

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

1. Suba o PostgreSQL e o Redis:

```bash
docker compose up -d
```

2. Em **Development**, as connection strings já vêm prontas no `appsettings.Development.json` (Postgres em `localhost:5432`, Redis em `localhost:6379`), então basta rodar:

```bash
dotnet run --project src/Onboarding.WebApi
```

A API sobe em `http://localhost:5000` e o Swagger em `http://localhost:5000/swagger`.

### Configuração por variáveis de ambiente

Para outros ambientes (ou para sobrescrever o Development), use variáveis de ambiente:

```bash
# Banco (PostgreSQL)
$env:DB_HOST="localhost"
$env:DB_PORT="5432"
$env:DB_NAME="onboarding"
$env:DB_USER="postgres"
$env:DB_PASSWORD="postgres"

# Cache (Redis)
$env:REDIS_HOST="localhost"
$env:REDIS_PORT="6379"
$env:REDIS_PASSWORD=""   # opcional, se o Redis exigir senha
```

> Alternativa: defina as connection strings completas em `ConnectionStrings__Onboarding` e `ConnectionStrings__Redis`.

## Endpoints

| Método | Rota                 | Descrição                        |
| ------ | -------------------- | -------------------------------- |
| POST   | `/api/accounts`      | Cria uma conta (201)             |
| GET    | `/api/accounts`      | Lista todas as contas (200)      |
| GET    | `/api/accounts/{id}` | Busca uma conta por id (200/404) |
| PUT    | `/api/accounts/{id}` | Atualiza nome/status (200/404)   |
| DELETE | `/api/accounts/{id}` | Remove uma conta (204/404)       |

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
- **Cache diário com Redis**: `CachedAccountRepository` (decorator sobre `IAccountRepository`) cacheia contas por id, por CPF e a listagem completa até o fim do dia, invalidando o cache em create/update/delete. Usa `IDistributedCache` com Redis (`StackExchange.Redis`), serializando as entidades em JSON. As chaves são prefixadas com `onboarding:`.

## Cache em memória local vs Redis

| Critério                       | Cache em memória (`IMemoryCache`) | Redis (`IDistributedCache`) |
| ------------------------------ | --------------------------------- | --------------------------- |
| Onde fica                      | RAM do processo da API            | Servidor Redis externo      |
| Compartilhado entre instâncias | Não                               | Sim                         |
| Sobrevive a restart da API     | Não                               | Sim                         |
| Latência                       | Menor (zero rede)                 | Baixa (uma chamada de rede) |
| Dependência externa            | Nenhuma                           | Servidor Redis              |
| Custo de infra                 | Nenhum                            | Um container/serviço a mais |

**Quando usar cache em memória local:** ideal para um **servidor único** (uma única instância da API). Como o cache vive dentro do processo, é o mais rápido possível e não exige infraestrutura extra. A desvantagem é que, se a aplicação for escalada para várias instâncias (ex.: atrás de um load balancer), cada instância teria o **seu próprio cache**, causando dados desatualizados e inconsistência entre instâncias.

**Quando usar Redis:** ideal para **sistemas distribuídos** com **várias instâncias/servidores** atrás de um load balancer. O Redis é um cache **único e centralizado** compartilhado por todas as instâncias, garantindo que todas leiam o mesmo dado e que a invalidação (create/update/delete) valha para o sistema inteiro. Também sobrevive a restart de uma instância individual. A contrapartida é a dependência de um servidor Redis e uma pequena latência de rede.

- **EF Core + UnitOfWork**: repositórios sobre `AppDbContext` com `IUnitOfWork` para commit transacional.
- **Validação de CPF**: `CpfValidator` aceita CPF com ou sem máscara e valida os dígitos verificadores.
