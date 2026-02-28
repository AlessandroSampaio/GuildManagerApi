# WarcraftLogs Integration API

> **Design Specification v1.0** · C# · .NET 10.0 · GraphQL · REST · EF Core

API RESTful em **C# .NET 10.0** que atua como middleware entre o cliente e a API GraphQL do WarcraftLogs. Ela recebe um `reportId`, consulta o WarcraftLogs, processa os dados e os persiste localmente — expondo endpoints para consulta de reports, personagens e guildas armazenados.

---

## Sumário

1. [Visão Geral](#1-visão-geral)
2. [Stack Tecnológico](#2-stack-tecnológico)
3. [Pré-requisitos](#3-pré-requisitos)
4. [Configuração e Instalação](#4-configuração-e-instalação)
5. [Autenticação Local (JWT)](#5-autenticação-local-jwt)
6. [Autenticação WarcraftLogs (OAuth 2.0)](#6-autenticação-warcraftlogs-oauth-20)
7. [Endpoints da API](#7-endpoints-da-api)
8. [Schema do Banco de Dados](#8-schema-do-banco-de-dados)
9. [Estrutura do Projeto](#9-estrutura-do-projeto)
10. [Checklist de Implementação](#10-checklist-de-implementação)
11. [Observações Importantes](#11-observações-importantes)
12. [Próximos Passos](#12-próximos-passos)

---

## 1. Visão Geral

```
Cliente  →  Nossa API (.NET 10 / REST)  →  OAuth 2.0 Token Service  →  WarcraftLogs (GraphQL v2)
                        ↓
                  Banco Local (PostgreSQL / EF Core)
```

O fluxo principal consiste em:

1. O cliente envia um `reportCode` para nossa API
2. A API autentica-se no WarcraftLogs via OAuth 2.0 (Client Credentials)
3. A API consulta a API GraphQL do WarcraftLogs buscando fights, personagens, guilda e rankings
4. Os dados são processados e persistidos localmente via EF Core
5. A API expõe os dados armazenados através de endpoints REST

---

## 2. Stack Tecnológico

| Tecnologia | Uso |
|---|---|
| **.NET 10.0** | Runtime / SDK |
| **ASP.NET Core** | Web API + Minimal APIs |
| **EF Core 10** | ORM + Migrations |
| **PostgreSQL** | Banco de dados local |
| **GraphQL.Client** | Comunicação com WarcraftLogs (StrawberryShake ou POCO) |
| **OAuth 2.0** | Client Credentials Flow para autenticação no WCL |
| **JWT Bearer** | Autenticação local da API |
| **BCrypt.Net** | Hash de senhas |
| **Swagger / OpenAPI** | Documentação automática dos endpoints |
| **Clean Architecture** | Domain · Application · Infrastructure · API |

---

## 3. Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 15+
- Client WarcraftLogs: criar em [https://www.warcraftlogs.com/api/clients/](https://www.warcraftlogs.com/api/clients/)

---

## 4. Configuração e Instalação

### 4.1 Configurar credenciais

Edite `src/API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=warcraftlogs;Username=postgres;Password=SUA_SENHA"
  },
  "WarcraftLogs": {
    "ClientId": "SEU_WCL_CLIENT_ID",
    "ClientSecret": "SEU_WCL_CLIENT_SECRET",
    "TokenEndpoint": "https://www.warcraftlogs.com/oauth/token",
    "GraphQlEndpoint": "https://www.warcraftlogs.com/api/v2/client"
  },
  "Jwt": {
    "SecretKey": "CHAVE_SECRETA_DE_PELO_MENOS_32_CARACTERES",
    "Issuer": "WarcraftLogsApi",
    "Audience": "WarcraftLogsApi",
    "AccessTokenExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 30
  }
}
```

Ou via variáveis de ambiente:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=warcraftlogs;Username=postgres;Password=..."
export WarcraftLogs__ClientId="seu_client_id"
export WarcraftLogs__ClientSecret="seu_client_secret"
export Jwt__SecretKey="sua_chave_secreta"
```

> **Dica:** Gere uma `SecretKey` forte com `openssl rand -base64 48`

### 4.2 Executar migrations

```bash
cd src/API
dotnet ef database update
```

### 4.3 Iniciar a API

```bash
dotnet run --project src/API
```

A API estará disponível em `https://localhost:5001`.  
Swagger disponível em `/swagger`.

> As migrations são executadas automaticamente no startup da aplicação.

---

## 5. Autenticação Local (JWT)

Todos os endpoints de dados exigem um **Bearer Token JWT** válido.

### Fluxo de uso

```
POST /api/auth/register   →  cria conta e retorna accessToken + refreshToken
POST /api/auth/login      →  autentica e retorna accessToken + refreshToken
GET  /api/reports         →  Authorization: Bearer <accessToken>
POST /api/auth/refresh    →  renova o accessToken (token rotation automático)
POST /api/auth/logout     →  revoga a sessão atual
```

### Endpoints de autenticação

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `POST` | `/api/auth/register` | ❌ | Registra novo usuário |
| `POST` | `/api/auth/login` | ❌ | Login e retorno de tokens |
| `POST` | `/api/auth/refresh` | ❌ | Renova o access token |
| `POST` | `/api/auth/logout` | ✅ | Revoga sessão atual |
| `POST` | `/api/auth/logout-all` | ✅ | Revoga todas as sessões |
| `PATCH` | `/api/auth/change-password` | ✅ | Altera senha e revoga sessões |
| `GET` | `/api/auth/me` | ✅ | Dados do usuário autenticado |

### Exemplo: registro e uso

```bash
# 1. Registrar
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"meunome","email":"eu@email.com","password":"minhasenha123"}'

# Resposta:
# { "accessToken": "eyJ...", "refreshToken": "abc...", "expiresAt": "...", "user": {...} }

# 2. Usar o token nas chamadas
curl https://localhost:5001/api/reports \
  -H "Authorization: Bearer eyJ..."

# 3. Renovar token
curl -X POST https://localhost:5001/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"abc..."}'
```

### Segurança implementada

- Senhas com hash via **BCrypt**
- Refresh tokens com **rotação automática** — ao renovar, o token antigo é revogado
- Detecção de reuso de token revogado revoga **todas** as sessões do usuário
- Troca de senha revoga automaticamente **todas** as sessões ativas

---

## 6. Autenticação WarcraftLogs (OAuth 2.0)

A API autentica-se no WarcraftLogs via **Client Credentials Flow**:

| Passo | Ação |
|---|---|
| **01 — Criar Client** | Registrar app em warcraftlogs.com → obter `client_id` e `client_secret` |
| **02 — Token Request** | `POST` para `/oauth/token` com `grant_type=client_credentials` |
| **03 — Cache Token** | Armazenar Bearer token em memória. Renovar automaticamente ao expirar. |
| **04 — GraphQL Call** | Header `Authorization: Bearer <token>` para `/api/v2/client` |

```http
# Token endpoint
POST https://www.warcraftlogs.com/oauth/token
Content-Type: application/x-www-form-urlencoded
Authorization: Basic {Base64(clientId:clientSecret)}

grant_type=client_credentials

# GraphQL endpoint (reports públicos)
POST https://www.warcraftlogs.com/api/v2/client
Authorization: Bearer {access_token}
Content-Type: application/json
```

---

## 7. Endpoints da API

Todos os endpoints abaixo exigem `Authorization: Bearer <token>`.

### Reports

#### `POST /api/reports/import/{reportCode}`
Importa e persiste um report do WarcraftLogs localmente. **Idempotente** — re-importar atualiza os dados existentes.

```graphql
# Query GraphQL enviada ao WarcraftLogs:
{
  reportData {
    report(code: "REPORT_CODE") {
      title
      startTime
      endTime
      guild { name  server { name region { name } } }
      fights { id name startTime endTime kill difficulty }
      masterData {
        actors { id name type subType server }
      }
    }
  }
}
```

**Resposta:** `201 Created`
```json
{
  "reportCode": "aAbBcCdDeE",
  "title": "Nome do Report",
  "fightsImported": 12,
  "killsImported": 5,
  "playersImported": 20,
  "performanceEntriesSaved": 100,
  "guildName": "Nome da Guilda"
}
```

---

#### `GET /api/reports`
Lista todos os reports importados com paginação.

| Query param | Padrão | Descrição |
|---|---|---|
| `page` | `1` | Página |
| `pageSize` | `20` | Itens por página (máx. 100) |

---

#### `GET /api/reports/{reportCode}`
Retorna detalhes completos de um report, incluindo guilda e lista de fights.

---

#### `GET /api/reports/{reportCode}/performance`
Retorna dados de performance (DPS / HPS / Tank) agrupados por fight.

```graphql
# Query adicional ao WarcraftLogs para rankings:
rankings(fightIDs: [...], playerMetric: dps) {
  data {
    name  class  spec  role
    amount  rankPercent  totalParses  bestPercent
  }
}
```

> Rankings são buscados apenas para **kills** (não wipes).

---

### Characters

#### `GET /api/characters/{id}`
Retorna dados de um personagem com histórico de performance nos últimos 50 fights registrados.

---

### Guilds

#### `GET /api/guilds/{id}`
Retorna dados de uma guilda.

#### `GET /api/guilds/{id}/reports`
Lista os reports importados de uma guilda (paginado).

| Query param | Padrão | Descrição |
|---|---|---|
| `page` | `1` | Página |
| `pageSize` | `20` | Itens por página (máx. 100) |

#### `GET /api/guilds/{id}/characters`
Lista os personagens associados a uma guilda.

---

### Tabela resumo

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `POST` | `/api/reports/import/{reportCode}` | ✅ | Importa report do WCL |
| `GET` | `/api/reports` | ✅ | Lista reports (paginado) |
| `GET` | `/api/reports/{code}` | ✅ | Detalhes de um report |
| `GET` | `/api/reports/{code}/performance` | ✅ | Performance por fight |
| `GET` | `/api/characters/{id}` | ✅ | Personagem com histórico |
| `GET` | `/api/guilds/{id}` | ✅ | Dados de uma guilda |
| `GET` | `/api/guilds/{id}/reports` | ✅ | Reports de uma guilda |
| `GET` | `/api/guilds/{id}/characters` | ✅ | Personagens de uma guilda |

---

## 8. Schema do Banco de Dados

### `Reports` — Tabela principal

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `string(16)` | PK | Código do report (ex: `"aAbBcC1234"`) |
| `Title` | `string` | | Título do report |
| `StartTime` | `DateTime` | | Início da sessão |
| `EndTime` | `DateTime` | | Fim da sessão |
| `GuildId` | `int?` | FK | Guilda associada |
| `ImportedAt` | `DateTime` | | Data da importação local |
| `LastSyncedAt` | `DateTime?` | | Última sincronização |

### `Fights` — Por report

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `int` | PK | Auto-increment |
| `FightIndex` | `int` | | ID do fight no report original |
| `ReportId` | `string` | FK | Report pai |
| `Name` | `string` | | Nome do boss / encounter |
| `Kill` | `bool?` | | Kill ou wipe |
| `StartTimeMs` | `long` | | Início em milissegundos |
| `EndTimeMs` | `long` | | Fim em milissegundos |
| `Difficulty` | `int` | | Dificuldade do encontro |

### `Characters` — Personagens únicos

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `int` | PK | Auto-increment |
| `WclActorId` | `int` | | ID original do WarcraftLogs |
| `Name` | `string` | | Nome do personagem |
| `Server` | `string` | | Realm |
| `Class` | `string` | | Classe (ex: Mage, Druid) |
| `GuildId` | `int?` | FK | Guilda (se houver) |

### `PerformanceEntries` — DPS / HPS / Tank por fight

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `int` | PK | Auto-increment |
| `FightId` | `int` | FK | Fight relacionado |
| `CharacterId` | `int` | FK | Personagem |
| `Spec` | `string` | | Spec usada no fight |
| `Role` | `string` | | `dps` / `healer` / `tank` |
| `Amount` | `float` | | DPS / HPS médio |
| `RankPercent` | `float?` | | % de ranking global WCL |
| `TotalParses` | `int?` | | Total de parses registrados |
| `BestPercent` | `float?` | | Melhor parse histórico |

### `Guilds` — Guildas

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `int` | PK | Auto-increment |
| `Name` | `string` | | Nome da guilda |
| `Server` | `string` | | Realm |
| `Region` | `string` | | `US` / `EU` / etc. |

### `AppUsers` — Usuários da API

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `int` | PK | Auto-increment |
| `Username` | `string(32)` | UNIQUE | Nome de usuário |
| `Email` | `string(128)` | UNIQUE | E-mail |
| `PasswordHash` | `string` | | Hash BCrypt |
| `Role` | `string` | | `User` / `Admin` |
| `CreatedAt` | `DateTime` | | Data de criação |
| `LastLoginAt` | `DateTime?` | | Último login |
| `IsActive` | `bool` | | Conta ativa |

### `RefreshTokens` — Sessões ativas

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `int` | PK | Auto-increment |
| `UserId` | `int` | FK | Usuário dono do token |
| `Token` | `string(128)` | UNIQUE | Token aleatório (Base64) |
| `ExpiresAt` | `DateTime` | | Expiração |
| `CreatedAt` | `DateTime` | | Criação |
| `IsRevoked` | `bool` | | Token revogado |

---

## 9. Estrutura do Projeto

```
WarcraftLogsApi/
├── src/
│   ├── WarcraftLogsApi.Domain/
│   │   ├── Entities/          # Report, Fight, Character, Guild, PerformanceEntry, AppUser
│   │   └── Interfaces/        # IReportRepository, IGuildRepository, IUserRepository...
│   │
│   ├── WarcraftLogsApi.Application/
│   │   ├── Services/          # ImportReportService
│   │   ├── Auth/              # JwtService, AuthService
│   │   ├── DTOs/              # ReportDto, CharacterDto, PerformanceDto, AuthDtos
│   │   └── GraphQL/           # WclGraphQLClient, WclTokenService
│   │
│   ├── WarcraftLogsApi.Infrastructure/
│   │   ├── Data/              # AppDbContext, EF Migrations
│   │   ├── Repositories/      # Implementações EF Core
│   │   └── Auth/              # WclTokenService (OAuth cache)
│   │
│   └── WarcraftLogsApi.API/
│       ├── Controllers/       # AuthController, ReportsController, CharactersController, GuildsController
│       ├── Middleware/        # GlobalExceptionMiddleware
│       ├── appsettings.json   # ClientId, ClientSecret, ConnectionString, JwtOptions
│       └── Program.cs
│
└── tests/
    └── WarcraftLogsApi.Tests/
```

---

## 10. Checklist de Implementação

### Fase 1 — Fundação
- [ ] Criar solução .NET 10 com 4 projetos (Domain, Application, Infrastructure, API)
- [ ] Configurar EF Core + banco de dados + migrations iniciais
- [ ] Registrar client no WarcraftLogs e obter `client_id` / `secret`

### Fase 2 — Integração WCL
- [ ] Implementar `WclTokenService` (OAuth client credentials + cache)
- [ ] Implementar `WclGraphQLClient` (HttpClient + query builder)
- [ ] Criar queries GraphQL para report, fights, masterData e rankings

### Fase 3 — Negócio
- [ ] Implementar `ImportReportService` (orquestração completa)
- [ ] Implementar repositories (EF Core) para todas as entidades

### Fase 4 — API
- [ ] Criar `ReportsController` com endpoints POST import e GET
- [ ] Criar `CharactersController` e `GuildsController`
- [ ] Implementar `AuthController` + `JwtService` + `AuthService`
- [ ] Configurar Swagger / OpenAPI com suporte a Bearer token
- [ ] Tratamento de erros global (middleware + ProblemDetails)

### Fase 5 — Qualidade
- [ ] Testes unitários para `ImportReportService`
- [ ] Documentar `appsettings` e variáveis de ambiente

---

## 11. Observações Importantes

- O `reportCode` no WarcraftLogs é **alfanumérico** (ex: `"aAbBcCdDeE"`), não um inteiro — o endpoint GraphQL usa `report(code: "...")`
- Rankings são buscados apenas para **kills** (fights com `kill: true`) — wipes não possuem dados de ranking no WCL
- O token OAuth do WarcraftLogs é **cacheado em memória** com renovação automática 2 minutos antes da expiração
- A importação é **idempotente** — re-importar o mesmo report atualiza os dados existentes sem duplicar
- O matching de personagens nos rankings é feito por **nome** (o WCL não retorna `actorId` nos rankings)
- O `GlobalExceptionMiddleware` retorna respostas no formato **ProblemDetails** (RFC 7807)

---

## 12. Próximos Passos

- [ ] Background job para re-sincronizar reports periodicamente (Hangfire ou Worker Service)
- [ ] Cache Redis para responses frequentes
- [ ] Rate limiting para evitar abusos da API WCL
- [ ] Endpoint de ranking comparativo entre membros de uma guilda
- [ ] Suporte a múltiplas métricas de performance (HPS, DTPS, além de DPS)
- [ ] Webhook / notificação quando um novo report for importado
- [ ] Painel administrativo para gestão de usuários (`Role: Admin`)

---

*WarcraftLogs Integration API · Design Spec v1.0 · .NET 10 · PostgreSQL*
