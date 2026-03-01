# WarcraftLogs Integration API

> **Design Specification v1.1** · C# · .NET 10.0 · GraphQL · REST · EF Core

API RESTful em **C# .NET 10.0** que atua como middleware entre o cliente e a API GraphQL do WarcraftLogs. Ela recebe um `reportCode`, consulta o WarcraftLogs, processa os dados e os persiste localmente — expondo endpoints para consulta de reports, personagens e guildas armazenados.

---

## Sumário

1. [Visão Geral](#1-visão-geral)
2. [Stack Tecnológico](#2-stack-tecnológico)
3. [Pré-requisitos](#3-pré-requisitos)
4. [Configuração e Instalação](#4-configuração-e-instalação)
5. [Autenticação Local (JWT)](#5-autenticação-local-jwt)
6. [Autenticação WarcraftLogs (OAuth 2.0)](#6-autenticação-warcraftlogs-oauth-20)
7. [Workflow Completo](#7-workflow-completo)
8. [Endpoints da API](#8-endpoints-da-api)
9. [Schema do Banco de Dados](#9-schema-do-banco-de-dados)
10. [Estrutura do Projeto](#10-estrutura-do-projeto)
11. [Checklist de Implementação](#11-checklist-de-implementação)
12. [Observações Importantes](#12-observações-importantes)
13. [Próximos Passos](#13-próximos-passos)

---

## 1. Visão Geral

A API suporta dois modos de operação dependendo do tipo de report a ser consultado:

```
╔═════════════════════════════════════════════════════════════════════════════╗
║  MODO PÚBLICO — reports públicos                                            ║
║                                                                             ║
║  Cliente → Nossa API → Client Credentials → /api/v2/client (WCL)            ║
║                  ↓                                                          ║
║            Banco Local (PostgreSQL)                                         ║
╚═════════════════════════════════════════════════════════════════════════════╝

╔═════════════════════════════════════════════════════════════════════════════╗
║  MODO PRIVADO — reports privados (requer autorização do usuário)            ║
║                                                                             ║
║  Cliente → GuildManager API → Authorization Code Flow → /api/v2/user (WCL)  ║
║                  ↓                                                          ║
║            Banco Local (PostgreSQL)                                         ║
╚═════════════════════════════════════════════════════════════════════════════╝
```

O modo é resolvido **automaticamente**: se o usuário autenticado possui um token WCL ativo (obtido via Authorization Code Flow), a rota privada `/api/v2/user` é usada. Caso contrário, a API cai para a rota pública `/api/v2/client` com Client Credentials.

---

## 2. Stack Tecnológico

| Tecnologia | Uso |
|---|---|
| **.NET 10.0** | Runtime / SDK |
| **ASP.NET Core** | Web API |
| **EF Core 10** | ORM + Migrations |
| **PostgreSQL** | Banco de dados local |
| **OAuth 2.0 — Client Credentials** | Token de aplicação para rota pública WCL |
| **OAuth 2.0 — Authorization Code** | Token de usuário para rota privada WCL |
| **JWT Bearer** | Autenticação local da API |
| **BCrypt.Net** | Hash de senhas |
| **IMemoryCache** | Cache de nonces anti-CSRF para o fluxo OAuth |
| **Swagger / OpenAPI** | Documentação automática |
| **Clean Architecture** | Domain · Application · Infrastructure · API |

---

## 3. Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 15+
- Client WarcraftLogs registrado em [https://www.warcraftlogs.com/api/clients/](https://www.warcraftlogs.com/api/clients/)
  - Tipo: **Authorization Code** (necessário para a rota privada)
  - Redirect URI configurada: `https://localhost:5001/api/wcl-auth/callback`

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
    "AuthorizeEndpoint": "https://www.warcraftlogs.com/oauth/authorize",
    "PublicGraphQlEndpoint": "https://www.warcraftlogs.com/api/v2/client",
    "PrivateGraphQlEndpoint": "https://www.warcraftlogs.com/api/v2/user",
    "RedirectUri": "https://localhost:5001/api/wcl-auth/callback"
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
export WarcraftLogs__RedirectUri="https://localhost:5001/api/wcl-auth/callback"
export Jwt__SecretKey="sua_chave_secreta"
```

> **Dica:** Gere uma `SecretKey` forte com `openssl rand -base64 48`

### 4.2 Executar migrations

```bash
dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/API
dotnet ef database update --project src/Infrastructure --startup-project src/API
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

Todos os endpoints de dados exigem um **Bearer Token JWT** válido obtido via registro ou login.

### Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `POST` | `/api/auth/register` | ❌ | Registra novo usuário |
| `POST` | `/api/auth/login` | ❌ | Login e retorno de tokens |
| `POST` | `/api/auth/refresh` | ❌ | Renova o access token (rotation) |
| `POST` | `/api/auth/logout` | ✅ | Revoga sessão atual |
| `POST` | `/api/auth/logout-all` | ✅ | Revoga todas as sessões |
| `PATCH` | `/api/auth/change-password` | ✅ | Altera senha e revoga sessões |
| `GET` | `/api/auth/me` | ✅ | Dados do usuário autenticado |

### Exemplo

```bash
# 1. Criar conta
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"meunome","email":"eu@email.com","password":"minhasenha123"}'

# Resposta: { "accessToken": "eyJ...", "refreshToken": "abc...", "expiresAt": "...", "user": {...} }

# 2. Usar o token
curl https://localhost:5001/api/reports \
  -H "Authorization: Bearer eyJ..."

# 3. Renovar token
curl -X POST https://localhost:5001/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"abc..."}'
```

### Segurança implementada

- Senhas com hash via **BCrypt**
- Refresh tokens com **rotação automática** — ao renovar, o token antigo é revogado imediatamente
- Detecção de reuso de token revogado revoga **todas** as sessões do usuário (proteção contra roubo)
- Troca de senha revoga automaticamente **todas** as sessões ativas

---

## 6. Autenticação WarcraftLogs (OAuth 2.0)

A API implementa **dois fluxos OAuth** do WarcraftLogs com seleção automática por endpoint:

### 6.1 Client Credentials (rota pública)

Usado automaticamente quando o usuário não possui token WCL. Acessa apenas reports **públicos**.

```
POST https://www.warcraftlogs.com/oauth/token
Authorization: Basic {Base64(clientId:clientSecret)}

grant_type=client_credentials
```

O token é cacheado em memória e renovado automaticamente 2 minutos antes da expiração.

---

### 6.2 Authorization Code Flow (rota privada)

Necessário para acessar reports **privados** via `/api/v2/user`. Exige que o usuário autorize explicitamente o acesso pelo browser.

#### Passo 1 — Obter URL de autorização

```bash
GET /api/wcl-auth/authorize
Authorization: Bearer <seu_jwt>
```

Resposta:
```json
{
  "authorizeUrl": "https://www.warcraftlogs.com/oauth/authorize?client_id=...&state=abc123",
  "state": "abc123",
  "instructions": "Abra a URL no browser para autorizar o acesso ao WarcraftLogs."
}
```

#### Passo 2 — Usuário autoriza no browser

O usuário abre `authorizeUrl` no browser e clica em **Authorize** no site do WarcraftLogs.

#### Passo 3 — WCL redireciona para o callback

O WarcraftLogs redireciona automaticamente para:

```
GET /api/wcl-auth/callback?code=AUTHORIZATION_CODE&state=abc123
```

A API valida o `state` (anti-CSRF), troca o `code` pelo access + refresh token do usuário e persiste no banco.

#### Troca do código

```
POST https://www.warcraftlogs.com/oauth/token
Authorization: Basic {Base64(clientId:clientSecret)}

grant_type=authorization_code
code=AUTHORIZATION_CODE
redirect_uri=https://localhost:5001/api/wcl-auth/callback
```

#### Renovação automática

Quando o access token do usuário expira, o `WclTokenService` renova automaticamente usando o refresh token armazenado — transparente para o chamador. Se o refresh token também expirar ou for inválido, o usuário precisa re-autorizar.

### Endpoints de gerenciamento do token WCL

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/wcl-auth/authorize` | ✅ JWT | Retorna URL de autorização WCL |
| `GET` | `/api/wcl-auth/callback` | ❌ | Callback OAuth — chamado pelo WCL |
| `GET` | `/api/wcl-auth/status` | ✅ JWT | Verifica se o usuário tem token WCL ativo |
| `DELETE` | `/api/wcl-auth/revoke` | ✅ JWT | Revoga e remove o token WCL do usuário |

---

## 7. Workflow Completo

### 7.1 Primeiro uso (report público)

```
1.  POST /api/auth/register          → cria conta, obtém JWT
2.  POST /api/reports/import/{code}  → importa via rota pública /api/v2/client
3.  GET  /api/reports/{code}         → consulta dados armazenados
```

### 7.2 Primeiro uso (report privado)

```
1.  POST /api/auth/register                 → cria conta, obtém JWT
2.  GET  /api/wcl-auth/authorize            → obtém authorizeUrl
3.  [browser] abre authorizeUrl             → usuário clica "Authorize" no WCL
4.  [automático] GET /api/wcl-auth/callback → WCL redireciona, token é persistido
5.  POST /api/reports/import/{code}         → importa via rota privada /api/v2/user
6.  GET  /api/reports/{code}/performance    → consulta performance armazenada
```

### 7.3 Uso recorrente (token WCL já autorizado)

```
1.  POST /api/auth/login             → obtém JWT
2.  POST /api/reports/import/{code}  → usa rota privada automaticamente
    ↳ se token WCL expirou           → renovação automática via refresh token (transparente)
    ↳ se refresh token inválido      → 401 com mensagem de re-autorização
```

### 7.4 Verificar status da autorização WCL

```bash
# Checar se o usuário atual tem token WCL ativo
GET /api/wcl-auth/status
Authorization: Bearer <seu_jwt>

# Resposta (autorizado):
# { "userId": 1, "isAuthorized": true, "message": "WarcraftLogs access is active." }

# Resposta (não autorizado):
# { "userId": 1, "isAuthorized": false, "message": "Not authorized. Call GET /api/wcl-auth/authorize." }
```

### 7.5 Revogar acesso WCL

```bash
DELETE /api/wcl-auth/revoke
Authorization: Bearer <seu_jwt>
# 204 No Content — token WCL removido, próximas importações usarão rota pública
```

---

## 8. Endpoints da API

Todos os endpoints abaixo (exceto auth) exigem `Authorization: Bearer <jwt>`.

### Auth local

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `POST` | `/api/auth/register` | ❌ | Registra novo usuário |
| `POST` | `/api/auth/login` | ❌ | Login |
| `POST` | `/api/auth/refresh` | ❌ | Renova access token |
| `POST` | `/api/auth/logout` | ✅ | Revoga sessão atual |
| `POST` | `/api/auth/logout-all` | ✅ | Revoga todas as sessões |
| `PATCH` | `/api/auth/change-password` | ✅ | Altera senha |
| `GET` | `/api/auth/me` | ✅ | Dados do usuário autenticado |

### WCL OAuth

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/wcl-auth/authorize` | ✅ JWT | Inicia fluxo OAuth — retorna authorizeUrl |
| `GET` | `/api/wcl-auth/callback` | ❌ | Callback do WCL — persiste token do usuário |
| `GET` | `/api/wcl-auth/status` | ✅ JWT | Status da autorização WCL |
| `DELETE` | `/api/wcl-auth/revoke` | ✅ JWT | Revoga token WCL do usuário |

### Reports

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `POST` | `/api/reports/import/{code}` | ✅ | Importa report (público ou privado automaticamente) |
| `GET` | `/api/reports` | ✅ | Lista reports paginados |
| `GET` | `/api/reports/{code}` | ✅ | Detalhes de um report |
| `GET` | `/api/reports/{code}/performance` | ✅ | Performance por fight (DPS/HPS/Tank) |

### Characters

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/characters/{id}` | ✅ | Personagem com histórico de performance |

### Guilds

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/guilds/{id}` | ✅ | Dados de uma guilda |
| `GET` | `/api/guilds/{id}/reports` | ✅ | Reports de uma guilda (paginado) |
| `GET` | `/api/guilds/{id}/characters` | ✅ | Personagens de uma guilda |

#### Resposta de importação (`POST /api/reports/import/{code}`)

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

## 9. Schema do Banco de Dados

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

### `Guilds`

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

### `RefreshTokens` — Sessões JWT ativas

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `int` | PK | Auto-increment |
| `UserId` | `int` | FK | Usuário dono do token |
| `Token` | `string(128)` | UNIQUE | Token aleatório (Base64) |
| `ExpiresAt` | `DateTime` | | Expiração |
| `CreatedAt` | `DateTime` | | Criação |
| `IsRevoked` | `bool` | | Token revogado |

### `WclUserTokens` — Tokens OAuth WCL por usuário ⭐ novo

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `int` | PK | Auto-increment |
| `UserId` | `int` | FK UNIQUE | Um token por usuário (1:1 com AppUsers) |
| `AccessToken` | `string` | | Bearer token WCL atual |
| `WclRefreshToken` | `string(512)` | | Refresh token WCL para renovação |
| `ExpiresAt` | `DateTime` | | Expiração do access token |
| `CreatedAt` | `DateTime` | | Primeira autorização |
| `LastRefreshedAt` | `DateTime?` | | Última renovação automática |

---

## 10. Estrutura do Projeto

```
WarcraftLogsApi/
├── global.json
├── WarcraftLogsApi.sln
├── src/
│   ├── WarcraftLogsApi.Domain/
│   │   ├── Entities/
│   │   │   ├── Report.cs
│   │   │   ├── Fight.cs
│   │   │   ├── Character.cs
│   │   │   ├── Guild.cs
│   │   │   ├── PerformanceEntry.cs
│   │   │   └── AppUser.cs          # AppUser + RefreshToken + WclUserToken
│   │   └── Interfaces/
│   │       ├── IRepositories.cs
│   │       └── IUserRepository.cs
│   │
│   ├── WarcraftLogsApi.Application/
│   │   ├── Auth/
│   │   │   ├── JwtService.cs
│   │   │   └── AuthService.cs
│   │   ├── DTOs/
│   │   │   ├── Dtos.cs
│   │   │   ├── AuthDtos.cs
│   │   │   └── WclAuthDtos.cs      # DTOs do fluxo OAuth WCL
│   │   ├── GraphQL/
│   │   │   └── WclGraphQLClient.cs # Resolução automática público/privado
│   │   └── Services/
│   │       └── ImportReportService.cs
│   │
│   ├── WarcraftLogsApi.Infrastructure/
│   │   ├── Auth/
│   │   │   └── WclTokenService.cs  # Client Credentials + Authorization Code
│   │   ├── Data/
│   │   │   └── AppDbContext.cs     # Inclui WclUserTokens
│   │   └── Repositories/
│   │       ├── Repositories.cs
│   │       └── UserRepository.cs
│   │
│   └── WarcraftLogsApi.API/
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── WclAuthController.cs  # Fluxo OAuth WCL (authorize/callback/status/revoke)
│       │   ├── ReportsController.cs
│       │   ├── CharactersGuildsControllers.cs
│       ├── Middleware/
│       │   └── GlobalExceptionMiddleware.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Program.cs
│
└── tests/
    └── WarcraftLogsApi.Tests.csproj
```

---

## 11. Checklist de Implementação

### Fase 1 — Fundação
- [x] Criar solução .NET 10 com 4 projetos (Domain, Application, Infrastructure, API)
- [x] Configurar EF Core + PostgreSQL + migrations iniciais
- [x] Registrar client no WarcraftLogs (tipo: Authorization Code) e configurar Redirect URI

### Fase 2 — Integração WCL
- [x] Implementar `WclTokenService` — Client Credentials (cache em memória)
- [x] Implementar `WclTokenService` — Authorization Code Flow (persiste no banco por usuário)
- [x] Implementar renovação automática via refresh token
- [x] Implementar `WclGraphQLClient` com resolução automática público/privado

### Fase 3 — Negócio
- [x] Implementar `ImportReportService` passando `userId` para o client
- [x] Implementar repositories EF Core para todas as entidades

### Fase 4 — API
- [x] Implementar `AuthController` (JWT local)
- [x] Implementar `WclAuthController` (authorize / callback / status / revoke)
- [x] Implementar `ReportsController` extraindo `userId` do JWT
- [x] Implementar `CharactersController` e `GuildsController`
- [x] Configurar Swagger com suporte a Bearer token
- [x] Registrar `IMemoryCache` para estado anti-CSRF OAuth
- [x] Tratamento de erros global (ProblemDetails)

### Fase 5 — Qualidade
- [ ] Testes unitários para `ImportReportService`
- [ ] Testes de integração para o fluxo OAuth WCL
- [ ] Documentar variáveis de ambiente

---

## 12. Observações Importantes

- O `reportCode` no WarcraftLogs é **alfanumérico** (ex: `"aAbBcCdDeE"`), nunca numérico
- A resolução entre rota pública e privada é **automática e transparente** — baseada na existência de token WCL vinculado ao usuário autenticado via JWT
- O `state` OAuth é armazenado em `IMemoryCache` com TTL de 10 minutos para prevenir ataques CSRF. Após o callback, é removido imediatamente
- Rankings são buscados apenas para **kills** — wipes não possuem dados de ranking no WCL
- O token de aplicação (Client Credentials) é cacheado em **memória compartilhada**; os tokens de usuário (Authorization Code) são persistidos por usuário no **banco de dados**
- A renovação via refresh token WCL é automática. Se o refresh token for inválido, o token é removido do banco e a API retorna `401` com mensagem indicando a necessidade de re-autorização
- A importação é **idempotente** — re-importar o mesmo report atualiza os dados existentes
- O `RedirectUri` configurado no `appsettings.json` deve ser idêntico ao registrado no painel do WarcraftLogs
- O `GlobalExceptionMiddleware` retorna respostas no formato **ProblemDetails** (RFC 7807)

---

## 13. Próximos Passos

- [ ] Background job para re-sincronizar reports periodicamente (Hangfire ou Worker Service)
- [ ] Cache Redis para responses frequentes de consulta
- [ ] Rate limiting para evitar abusos da cota da API WCL
- [ ] Endpoint de ranking comparativo entre membros de uma guilda
- [ ] Suporte a múltiplas métricas de performance (HPS, DTPS, além de DPS)
- [ ] Webhook / notificação ao importar novo report
- [ ] Painel administrativo para gestão de usuários (`Role: Admin`)
- [ ] Persistência do `state` OAuth em banco/Redis para ambientes com múltiplas instâncias

---

*GuildManager API · Design Spec v1.2 · .NET 10 · PostgreSQL*
