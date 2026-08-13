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
│  Onboarding.Services  (AccountService, CpfValidator, DTOs)   │
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

| Método | Rota                      | Descrição                        |
| ------ | ------------------------- | -------------------------------- |
| POST   | `/api/accounts`           | Cria uma conta (201)             |
| GET    | `/api/accounts`           | Lista todas as contas (200)      |
| GET    | `/api/accounts/cpf/{cpf}` | Busca uma conta por cpf (200)    |
| GET    | `/api/accounts/{id}`      | Busca uma conta por id (200/404) |
| PUT    | `/api/accounts/{id}`      | Atualiza nome/status (200/404)   |
| DELETE | `/api/accounts/{id}`      | Remove uma conta (204/404)       |

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
dotnet test src/Onboarding.Tests
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

## Paginação

O endpoint `GET /api/accounts` é paginado para evitar carregar todas as contas de uma vez na memória — essencial quando a base cresce (milhares/milhões de registros). Sem paginação, cada chamada faria um `SELECT` completo, consumindo memória, CPU e rede desnecessariamente e degradando a API.

**Parâmetros de query:**

| Parâmetro | Padrão | Limites | Descrição                          |
| --------- | ------ | ------- | ---------------------------------- |
| `page`    | `1`    | `>= 1`  | Número da página (1-based)         |
| `pageSize`| `10`   | `1..100`| Quantidade de itens por página     |

**Como funciona** (`AccountRepository.GetAllAsync`):

- `page` e `pageSize` são normalizados (`page = Math.Max(page, 1)` e `pageSize = Math.Clamp(pageSize, 1, 100)`), evitando valores inválidos/negativos.
- Conta o total de registros (`CountAsync`) para calcular `TotalItems` e `TotalPages`.
- Aplica `Skip((page - 1) * pageSize).Take(pageSize)` para buscar apenas a fatia da página.
- Retorna um `PaginatedResult<T>` com `Items`, `Page`, `PageSize`, `TotalItems` e `TotalPages`.

**Exemplo de resposta:**

```json
{
  "items": [ { "id": 1, "name": "Felipe", "cpf": "***.***.247-25", "status": "Ativa" } ],
  "page": 1,
  "pageSize": 10,
  "totalItems": 2,
  "totalPages": 1
}
```

**Cache por página:** o `CachedAccountRepository` cacheia cada página com uma chave própria (`accounts:all:{version}:{page}:{pageSize}`). Isso evita dois problemas: (1) todas as páginas compartilharem a mesma chave e retornarem dados errados, e (2) a invalidação em create/update/delete. Para invalidar todas as páginas de uma vez, usa-se um **contador de versão** (`accounts:version`): cada mutação incrementa a versão, o que torna obsoletas todas as chaves de página anteriores.

## Máscara e normalizador de CPF

O CPF é tratado em três etapas no `CpfValidator`:

1. **Normalização** (`NormalizeCpf`): remove tudo que não for dígito (`char.IsDigit`), aceitando CPF com ou sem máscara (`529.982.247-25` → `52998224725`). Isso garante que o dado seja armazenado e comparado de forma consistente no banco, independente de como o usuário digitou.
2. **Validação** (`IsValid`): verifica se tem exatamente 11 dígitos, rejeita sequências repetidas (ex.: `111.111.111-11`) e confere os dois dígitos verificadores pelo algoritmo oficial do CPF.
3. **Máscara** (`Mask`): na resposta da API, o CPF é **parcialmente mascarado** (`***.***.247-25`), expondo apenas os últimos 5 dígitos.

**Por que isso é importante:**

- **Privacidade / LGPD:** o CPF é um dado pessoal sensível. Retorná-lo por completo na API expõe informações desnecessárias a quem consome o endpoint. Mascarar a maior parte do número reduz a superfície de exposição, mantendo apenas o suficiente para identificação parcial.
- **Consistência:** normalizar antes de gravar evita duplicidades (o mesmo CPF digitado com e sem máscara viraria dois registros diferentes).
- **Robustez:** a validação dos dígitos verificadores impede CPFs inválidos ou fabricados de entrarem no sistema.

## Autenticação (decisão de design)

**Por que não há autenticação agora:** este é um **projeto de teste técnico**, cujo foco é demonstrar arquitetura (DDD, SOLID, Clean Code), CRUD, validação de CPF, cache e o padrão Outbox. Adicionar autenticação completa (login, refresh token, revogação, etc.) aumentaria o escopo sem agregar valor à avaliação do que está sendo testado.

**O que seria ideal em produção:** em um sistema real, todos os endpoints deveriam exigir autenticação via **JWT (JSON Web Token)** — um token assinado que o cliente envia no header `Authorization: Bearer <token>`, permitindo que a API valide a identidade do usuário sem estado de sessão no servidor. Isso protege os dados pessoais (como o CPF) e garante que apenas usuários autenticados e autorizados acessem os recursos.

**Demonstração no código:** para evidenciar que a autenticação foi considerada, o atributo `[Authorize]` está **comentado** no controller:

```csharp
[ApiController]
[Route("api/accounts")]
//[Authorize]
public class AccountsController(IAccountService accountService) : ControllerBase
```

Basta descomentar `[Authorize]` e configurar o JWT (via `AddAuthentication().AddJwtBearer(...)` no `Program.cs`) para que todos os endpoints passem a exigir um token válido. O `using Microsoft.AspNetCore.Authorization;` já está presente no arquivo.
