# WarcraftLogs Integration API

> **Design Specification v2.0** · C# · .NET 10.0 · GraphQL · REST · EF Core

API RESTful em **C# .NET 10.0** que atua como middleware entre o cliente e a API GraphQL do WarcraftLogs. Ela recebe um `reportCode`, consulta o WarcraftLogs, processa os dados e os persiste localmente — expondo endpoints para consulta de reports, personagens, guildas, players, semanas de raid e pontuação de performance.

---

## Sumário

1. [Visão Geral](#1-visão-geral)
2. [Stack Tecnológico](#2-stack-tecnológico)
3. [Pré-requisitos](#3-pré-requisitos)
4. [Configuração e Instalação](#4-configuração-e-instalação)
5. [Autenticação Local (JWT)](#5-autenticação-local-jwt)
6. [Autenticação WarcraftLogs (OAuth 2.0)](#6-autenticação-warcraftlogs-oauth-20)
7. [Workflow Completo](#7-workflow-completo)
8. [Conceitos de Domínio](#8-conceitos-de-domínio)
9. [Endpoints da API](#9-endpoints-da-api)
10. [Schema do Banco de Dados](#10-schema-do-banco-de-dados)
11. [Estrutura do Projeto](#11-estrutura-do-projeto)
12. [Checklist de Implementação](#12-checklist-de-implementação)
13. [Observações Importantes](#13-observações-importantes)
14. [Próximos Passos](#14-próximos-passos)

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
| **Redis** | Store distribuído para contadores de rate limiting |
| **AspNetCoreRateLimit** | Rate limiting por IP e por cliente |
| **WebSockets** | Acompanhamento em tempo real do status de importação |
| **Swagger / OpenAPI** | Documentação automática |
| **Clean Architecture** | Domain · Application · Infrastructure · API |

---

## 3. Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 15+
- Redis 7+ (usado como store distribuído para rate limiting)
- Client WarcraftLogs registrado em [https://www.warcraftlogs.com/api/clients/](https://www.warcraftlogs.com/api/clients/)
  - Tipo: **Authorization Code** (necessário para a rota privada)
  - Redirect URI configurada: `https://localhost:5001/api/wcl-auth/callback`

---

## 4. Configuração e Instalação

### 4.1 Configurar credenciais

Copie `src/Api/appsettings.example.json` para `src/Api/appsettings.json` e preencha os valores:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=warcraftlogs;Username=postgres;Password=SUA_SENHA",
    "Redis": "localhost:6379,password=SUA_SENHA_REDIS,abortConnect=false"
  },
  "Encryption": {
    "MasterKey": "GERE_COM: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))"
  },
  "WarcraftLogs": {
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
  },
  "IpRateLimiting": { ... },
  "ClientRateLimiting": { ... }
}
```

> As credenciais WCL (clientId/clientSecret) **não ficam no appsettings** — são configuradas via `PUT /api/admin/wcl-credentials` após o primeiro login com conta Admin.

> **Dica:** Gere uma `SecretKey` forte com `openssl rand -base64 48`

Ou via variáveis de ambiente:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=warcraftlogs;Username=postgres;Password=..."
export ConnectionStrings__Redis="localhost:6379,password=...,abortConnect=false"
export Jwt__SecretKey="sua_chave_secreta"
export Encryption__MasterKey="sua_master_key_base64"
```

### 4.2 Rate Limiting

A API usa **dois níveis** de rate limiting via `AspNetCoreRateLimit`, com contadores armazenados no **Redis** (necessário para ambientes multi-instância):

#### Por IP (`IpRateLimiting`)

Limita requisições com base no IP do cliente (detectado via `X-Real-IP` ou `X-Forwarded-For` em proxies reversos).

| Endpoint | Janela | Limite |
|----------|--------|--------|
| `POST /api/auth/login` | 1 min | 5 req |
| `*` (global) | 1 min | 60 req |

#### Por Cliente (`ClientRateLimiting`)

Limita por `X-ClientId` (header injetado pelo middleware `ClientIdInjectionMiddleware`).

| Endpoint | Janela | Limite |
|----------|--------|--------|
| `*` (global) | 1 min | 120 req |
| `*` (global) | 1 hora | 3.000 req |
| `POST /api/*` | 1 min | 30 req |

Os limites podem ser ajustados em `appsettings.json` nas seções `IpRateLimiting.GeneralRules` e `ClientRateLimiting.GeneralRules` sem necessidade de recompilação. Requisições bloqueadas retornam `429 Too Many Requests`.

### 4.3 Executar migrations

```bash
dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/API
dotnet ef database update --project src/Infrastructure --startup-project src/API
```

### 4.4 Iniciar a API

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
2.  POST /api/Reports/import/{code}  → importa via rota pública /api/v2/client (202 Accepted)
    ↳ conecte no wsUrl retornado para acompanhar o progresso em tempo real
3.  GET  /api/Reports/{code}         → consulta dados armazenados
```

### 7.2 Primeiro uso (report privado)

```
1.  POST /api/auth/register                 → cria conta, obtém JWT
2.  GET  /api/wcl-auth/authorize            → obtém authorizeUrl
3.  [browser] abre authorizeUrl             → usuário clica "Authorize" no WCL
4.  [automático] GET /api/wcl-auth/callback → WCL redireciona, token é persistido
5.  POST /api/Reports/import/{code}         → importa via rota privada /api/v2/user
6.  GET  /api/Reports/{code}/performance    → consulta performance armazenada
```

### 7.3 Uso recorrente (token WCL já autorizado)

```
1.  POST /api/auth/login             → obtém JWT
2.  POST /api/Reports/import/{code}  → usa rota privada automaticamente
    ↳ se token WCL expirou           → renovação automática via refresh token (transparente)
    ↳ se refresh token inválido      → 401 com mensagem de re-autorização
```

### 7.4 Calcular pontuação de uma semana de raid

```
1.  POST /api/raid-weeks                              → cria a semana (label + startsAt)
2.  POST /api/raid-weeks/{id}/reports/{reportCode}    → associa reports à semana
3.  POST /api/raid-weeks/{id}/penalties               → aplica penalidades a players (opcional)
4.  POST /api/player-scoring/by-week/{raidWeekId}     → calcula pontuação da semana
```

### 7.5 Verificar status da autorização WCL

```bash
GET /api/wcl-auth/status
Authorization: Bearer <seu_jwt>

# Resposta (autorizado):
# { "userId": "...", "isAuthorized": true, "message": "WarcraftLogs access is active." }

# Resposta (não autorizado):
# { "userId": "...", "isAuthorized": false, "message": "Not authorized. Call GET /api/wcl-auth/authorize." }
```

### 7.6 Revogar acesso WCL

```bash
DELETE /api/wcl-auth/revoke
Authorization: Bearer <seu_jwt>
# 204 No Content — token WCL removido, próximas importações usarão rota pública
```

---

## 8. Conceitos de Domínio

### Player

Representa um **jogador real** (pessoa), independente dos personagens que ele joga. Um player pode ter múltiplos characters vinculados. Isso permite rastrear a performance de um jogador mesmo quando ele troca de personagem.

### Character

Personagem do jogo importado do WarcraftLogs. Pertence a uma guilda e possui histórico de performance por fight.

### RaidWeek

Agrupa um conjunto de reports de uma **semana de raid** para facilitar o cálculo de pontuação semanal. Cada semana tem uma data de início (`startsAt`) e fim calculado automaticamente.

### PenaltyEvent

Evento de penalidade **reutilizável** com uma descrição e um valor fixo de pontos negativos. Exemplos: "Ausência não justificada (-20 pts)", "Morte evitável (-10 pts)". Cada evento pode ser aplicado a múltiplos players em diferentes semanas.

### PlayerScoring

Sistema de pontuação que converte o `rankPercent` WarcraftLogs em pontos inteiros via **tiers configuráveis** (ex: 95–100% = 100 pts, 75–94% = 75 pts). As configurações de tiers são gerenciadas pelo endpoint Admin. Penalidades da semana são descontadas do total de performance.

---

## 9. Endpoints da API

Todos os endpoints abaixo (exceto auth e callback) exigem `Authorization: Bearer <jwt>`.

### Auth local

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `POST` | `/api/auth/register` | ❌ | Registra novo usuário |
| `POST` | `/api/auth/login` | ❌ | Login — retorna accessToken + refreshToken |
| `POST` | `/api/auth/refresh` | ❌ | Renova access token via refresh token (rotation) |
| `POST` | `/api/auth/logout` | ✅ | Revoga o refresh token da sessão atual |
| `POST` | `/api/auth/logout-all` | ✅ | Revoga todos os refresh tokens do usuário |
| `PATCH` | `/api/auth/change-password` | ✅ | Altera a senha e revoga todas as sessões ativas |
| `GET` | `/api/auth/me` | ✅ | Retorna informações do usuário autenticado |

### WCL OAuth

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/wcl-auth/authorize` | ✅ JWT | Inicia o fluxo OAuth — retorna a URL de autorização do WCL |
| `GET` | `/api/wcl-auth/callback` | ❌ | Callback do WCL — valida state e persiste o token do usuário |
| `GET` | `/api/wcl-auth/status` | ✅ JWT | Verifica se o usuário possui token WCL ativo |
| `DELETE` | `/api/wcl-auth/revoke` | ✅ JWT | Revoga e remove o token WCL do usuário |

### Admin

> Requer role `Admin`.

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `PUT` | `/api/admin/wcl-credentials` | ✅ Admin | Configura as credenciais WCL da aplicação (clientId + clientSecret) |
| `GET` | `/api/admin/wcl-credentials/status` | ✅ Admin | Retorna o status das credenciais WCL configuradas |
| `GET` | `/api/admin/scoring-settings` | ✅ Admin | Retorna as configurações de pontuação por tiers |
| `PUT` | `/api/admin/scoring-settings` | ✅ Admin | Atualiza as configurações de tiers de pontuação |
| `DELETE` | `/api/admin/scoring-settings` | ✅ Admin | Remove as configurações de pontuação |
| `GET` | `/api/admin/scoring-settings/calculate` | ✅ Admin | Simula o cálculo de pontos para um `rankPercent` informado (query param) |

#### Exemplo — configurar tiers de pontuação

```json
// PUT /api/admin/scoring-settings
{
  "tiers": [
    { "minPercent": 95, "maxPercent": 100, "points": 100, "label": "Mythic" },
    { "minPercent": 75, "maxPercent": 94,  "points": 75,  "label": "Heroic" },
    { "minPercent": 50, "maxPercent": 74,  "points": 50,  "label": "Normal" },
    { "minPercent": 0,  "maxPercent": 49,  "points": 25,  "label": "Below Average" }
  ]
}
```

### Reports

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `POST` | `/api/Reports/import/{reportCode}` | ✅ | Inicia importação assíncrona do report — retorna `202 Accepted` com wsUrl |
| `GET` | `/api/Reports/{reportCode}/ws` | ✅ (query param) | WebSocket para acompanhar o progresso da importação em tempo real |
| `GET` | `/api/Reports` | ✅ | Lista reports paginados |
| `GET` | `/api/Reports/{reportCode}` | ✅ | Retorna dados completos de um report específico |
| `GET` | `/api/Reports/{reportCode}/performance` | ✅ | Retorna dados de performance por fight (DPS/HPS/Tank) |

#### Resposta de importação (`POST /api/Reports/import/{reportCode}`)

```json
{
  "reportCode": "aAbBcCdDeE",
  "status": "Queued",
  "wsUrl": "wss://localhost:5001/api/Reports/aAbBcCdDeE/ws",
  "message": "Import started. Connect to wsUrl to follow progress."
}
```

> O token JWT deve ser passado via query param `?access_token=<jwt>` ao conectar no WebSocket.

### Characters

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/characters/{id}` | ✅ | Retorna detalhes de um personagem com histórico de performance |
| `GET` | `/api/characters/search` | ✅ | Busca characters por nome (substring) e/ou classe, paginado. Retorna o player vinculado se houver |

#### Parâmetros de `/api/characters/search`

| Param | Tipo | Descrição |
|-------|------|-----------|
| `q` | string | Substring do nome do personagem |
| `className` | string | Classe (ex: `Mage`, `Druid`) |
| `page` | int | Página (default: 1) |
| `pageSize` | int | Itens por página (default: 20) |

### Guilds

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/guilds` | ✅ | Lista todas as guildas paginado |
| `GET` | `/api/guilds/{id}` | ✅ | Retorna dados de uma guilda específica |
| `GET` | `/api/guilds/{id}/reports` | ✅ | Lista reports de uma guilda (paginado) |
| `GET` | `/api/guilds/{id}/characters` | ✅ | Lista personagens de uma guilda |
| `GET` | `/api/guilds/{id}/roster` | ✅ | Retorna o roster da guilda com vínculo ao player (se houver) |
| `POST` | `/api/guilds/{id}/sync-characters` | ✅ | Sincroniza todos os membros atuais da guilda via API do WarcraftLogs |

#### Sync de personagens (`POST /api/guilds/{id}/sync-characters`)

Busca todos os membros da guilda diretamente na API do WarcraftLogs (`guildData`) e faz upsert na base local. Útil para popular o roster sem depender de importações de reports.

```json
// Resposta 200
{
  "guildId": 1,
  "guildName": "MinhaGuilda",
  "charactersSynced": 42
}
```

> A operação é **idempotente** — rodar múltiplas vezes não duplica personagens.

### Players

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/players` | ✅ | Lista players paginado |
| `POST` | `/api/players` | ✅ | Cria um novo player |
| `GET` | `/api/players/{id}` | ✅ | Retorna detalhes de um player com seus characters vinculados |
| `PUT` | `/api/players/{id}` | ✅ | Atualiza o nome de um player |
| `DELETE` | `/api/players/{id}` | ✅ | Remove um player |
| `POST` | `/api/players/{id}/characters/{characterId}` | ✅ | Vincula um character a um player |
| `DELETE` | `/api/players/{id}/characters/{characterId}` | ✅ | Desvincula um character de um player |

### PenaltyEvents

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/penalty-events` | ✅ | Lista todos os eventos de penalidade disponíveis |
| `POST` | `/api/penalty-events` | ✅ | Cria um novo evento de penalidade (descrição + pontos) |
| `GET` | `/api/penalty-events/{id}` | ✅ | Retorna detalhes de um evento de penalidade |
| `PUT` | `/api/penalty-events/{id}` | ✅ | Atualiza descrição e/ou pontos de um evento de penalidade |
| `DELETE` | `/api/penalty-events/{id}` | ✅ | Remove um evento de penalidade |

### RaidWeeks

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `GET` | `/api/raid-weeks` | ✅ | Lista semanas de raid paginado |
| `POST` | `/api/raid-weeks` | ✅ | Cria uma nova semana de raid (label, startsAt, reportCodes opcionais) |
| `GET` | `/api/raid-weeks/{id}` | ✅ | Retorna detalhes de uma semana de raid com seus report codes |
| `PUT` | `/api/raid-weeks/{id}` | ✅ | Atualiza label e/ou data de início de uma semana |
| `DELETE` | `/api/raid-weeks/{id}` | ✅ | Remove uma semana de raid |
| `GET` | `/api/raid-weeks/by-date` | ✅ | Busca a semana de raid que contém uma data específica (query param `date`) |
| `POST` | `/api/raid-weeks/{id}/reports/{reportCode}` | ✅ | Associa um report a uma semana de raid |
| `DELETE` | `/api/raid-weeks/{id}/reports/{reportCode}` | ✅ | Remove a associação de um report com uma semana |
| `GET` | `/api/raid-weeks/{id}/penalties` | ✅ | Lista todas as penalidades aplicadas a players nessa semana |
| `POST` | `/api/raid-weeks/{id}/penalties` | ✅ | Aplica uma penalidade a um player na semana informada |
| `DELETE` | `/api/raid-weeks/{id}/penalties/{penaltyId}` | ✅ | Remove uma penalidade aplicada a um player nessa semana |

### PlayerScoring

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| `POST` | `/api/player-scoring` | ✅ | Calcula a pontuação de performance dos players para uma lista de report codes informada |
| `POST` | `/api/player-scoring/by-week/{raidWeekId}` | ✅ | Calcula a pontuação de todos os players para todos os reports de uma RaidWeek registrada. Inclui penalidades da semana no resultado |

#### Exemplo — calcular por lista de reports

```json
// POST /api/player-scoring
{
  "reportCodes": ["aAbBcC1234", "xXyYzZ5678"]
}
```

#### Exemplo — resposta de pontuação

```json
{
  "players": [
    {
      "playerId": 1,
      "playerName": "Fulano",
      "performancePoints": 175,
      "penaltyPoints": -20,
      "totalPoints": 155,
      "averageRankPercent": 87.3,
      "scoredEntries": 8,
      "unscoredEntries": 1,
      "characters": [...],
      "penalties": [...]
    }
  ],
  "reports": [...],
  "scoringSettings": { "tiers": [...] },
  "raidWeek": { "id": 5, "label": "Semana 1", "startsAt": "...", "endsAt": "..." },
  "totalEntriesEvaluated": 40,
  "entriesWithoutRankPercent": 3
}
```

---

## 10. Schema do Banco de Dados

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

### `WclUserTokens` — Tokens OAuth WCL por usuário

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `int` | PK | Auto-increment |
| `UserId` | `int` | FK UNIQUE | Um token por usuário (1:1 com AppUsers) |
| `AccessToken` | `string` | | Bearer token WCL atual |
| `WclRefreshToken` | `string(512)` | | Refresh token WCL para renovação |
| `ExpiresAt` | `DateTime` | | Expiração do access token |
| `CreatedAt` | `DateTime` | | Primeira autorização |
| `LastRefreshedAt` | `DateTime?` | | Última renovação automática |

### `Players` — Jogadores reais

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `int` | PK | Auto-increment |
| `Name` | `string` | | Nome do jogador |
| `CreatedAt` | `DateTime` | | Data de criação |
| `UpdatedAt` | `DateTime` | | Última atualização |

### `RaidWeeks` — Semanas de raid

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `int` | PK | Auto-increment |
| `Label` | `string` | | Identificador amigável (ex: "Semana 1") |
| `StartsAt` | `DateTime` | | Início da semana |
| `EndsAt` | `DateTime` | | Fim da semana (calculado) |
| `CreatedAt` | `DateTime` | | Data de criação |
| `UpdatedAt` | `DateTime` | | Última atualização |

### `PenaltyEvents` — Tipos de penalidade

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `int` | PK | Auto-increment |
| `Description` | `string` | | Descrição do evento |
| `Points` | `int` | | Pontos negativos aplicados |
| `CreatedAt` | `DateTime` | | Data de criação |

### `PlayerWeekPenalties` — Penalidades aplicadas por semana

| Coluna | Tipo | Flag | Descrição |
|--------|------|------|-----------|
| `Id` | `int` | PK | Auto-increment |
| `PlayerId` | `int` | FK | Player penalizado |
| `RaidWeekId` | `int` | FK | Semana de raid |
| `PenaltyEventId` | `int` | FK | Tipo de penalidade |
| `Note` | `string?` | | Observação opcional |
| `CreatedAt` | `DateTime` | | Data de aplicação |

### `ScoringSettings` / `ScoringTiers` — Configurações de pontuação

Tabelas que definem os tiers de conversão de `rankPercent` → pontos, gerenciadas via `/api/admin/scoring-settings`.

---

## 11. Estrutura do Projeto

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
│   │   │   ├── Player.cs
│   │   │   ├── RaidWeek.cs
│   │   │   ├── PenaltyEvent.cs
│   │   │   ├── PlayerWeekPenalty.cs
│   │   │   ├── ScoringSettings.cs
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
│   │   │   └── WclAuthDtos.cs
│   │   ├── GraphQL/
│   │   │   └── WclGraphQLClient.cs # Resolução automática público/privado
│   │   └── Services/
│   │       ├── ImportReportService.cs
│   │       ├── PlayerScoringService.cs
│   │       └── RaidWeekService.cs
│   │
│   ├── WarcraftLogsApi.Infrastructure/
│   │   ├── Auth/
│   │   │   └── WclTokenService.cs  # Client Credentials + Authorization Code
│   │   ├── Data/
│   │   │   └── AppDbContext.cs
│   │   └── Repositories/
│   │       ├── Repositories.cs
│   │       └── UserRepository.cs
│   │
│   └── WarcraftLogsApi.API/
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── WclAuthController.cs
│       │   ├── AdminController.cs
│       │   ├── ReportsController.cs
│       │   ├── CharactersController.cs
│       │   ├── GuildsController.cs
│       │   ├── PlayersController.cs
│       │   ├── PenaltyEventsController.cs
│       │   ├── RaidWeeksController.cs
│       │   └── PlayerScoringController.cs
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

## 12. Checklist de Implementação

### Fase 1 — Fundação
- [x] Criar solução .NET 10 com 4 projetos (Domain, Application, Infrastructure, API)
- [x] Configurar EF Core + PostgreSQL + migrations iniciais
- [x] Registrar client no WarcraftLogs (tipo: Authorization Code) e configurar Redirect URI

### Fase 2 — Integração WCL
- [x] Implementar `WclTokenService` — Client Credentials (cache em memória)
- [x] Implementar `WclTokenService` — Authorization Code Flow (persiste no banco por usuário)
- [x] Implementar renovação automática via refresh token
- [x] Implementar `WclGraphQLClient` com resolução automática público/privado

### Fase 3 — Negócio core
- [x] Implementar `ImportReportService` (assíncrono com WebSocket)
- [x] Implementar repositories EF Core para todas as entidades

### Fase 4 — API Auth & Reports
- [x] Implementar `AuthController` (JWT local)
- [x] Implementar `WclAuthController` (authorize / callback / status / revoke)
- [x] Implementar `ReportsController` com import assíncrono e WebSocket
- [x] Implementar `CharactersController` e `GuildsController`
- [x] Configurar Swagger com suporte a Bearer token
- [x] Registrar `IMemoryCache` para estado anti-CSRF OAuth
- [x] Tratamento de erros global (ProblemDetails)

### Fase 5 — Novos domínios
- [x] Implementar `PlayersController` (CRUD + vincular/desvincular characters)
- [x] Implementar `PenaltyEventsController` (CRUD)
- [x] Implementar `RaidWeeksController` (CRUD + reports + penalties)
- [x] Implementar `PlayerScoringController` (por report codes e por raid week)
- [x] Implementar `AdminController` (credenciais WCL + scoring settings)
- [x] Implementar `PlayerScoringService` com conversão rankPercent → pontos por tiers

### Fase 6 — Qualidade
- [ ] Testes unitários para `PlayerScoringService`
- [ ] Testes unitários para `ImportReportService`
- [ ] Testes de integração para o fluxo OAuth WCL
- [ ] Documentar variáveis de ambiente

---

## 13. Observações Importantes

- O `reportCode` no WarcraftLogs é **alfanumérico** (ex: `"aAbBcCdDeE"`), nunca numérico
- A resolução entre rota pública e privada é **automática e transparente** — baseada na existência de token WCL vinculado ao usuário autenticado via JWT
- O `state` OAuth é armazenado em `IMemoryCache` com TTL de 10 minutos para prevenir ataques CSRF. Após o callback, é removido imediatamente
- Rankings são buscados apenas para **kills** — wipes não possuem dados de ranking no WCL
- O token de aplicação (Client Credentials) é cacheado em **memória compartilhada**; os tokens de usuário (Authorization Code) são persistidos por usuário no **banco de dados**
- A renovação via refresh token WCL é automática. Se o refresh token for inválido, o token é removido do banco e a API retorna `401` com mensagem indicando a necessidade de re-autorização
- A importação é **idempotente** — re-importar o mesmo report atualiza os dados existentes
- O `RedirectUri` configurado no `appsettings.json` deve ser idêntico ao registrado no painel do WarcraftLogs
- O `GlobalExceptionMiddleware` retorna respostas no formato **ProblemDetails** (RFC 7807)
- O token JWT deve ser passado via query param `?access_token=<jwt>` ao conectar no endpoint WebSocket `/api/Reports/{code}/ws`

---

## 14. Próximos Passos

- [ ] Background job para re-sincronizar reports periodicamente (Hangfire ou Worker Service)
- [ ] Cache Redis para responses frequentes de consulta
- [x] Rate limiting por IP e por cliente (AspNetCoreRateLimit + Redis)
- [ ] Persistência do `state` OAuth em banco/Redis para ambientes com múltiplas instâncias
- [ ] Testes de carga no endpoint de scoring com grandes volumes de reports

---

*GuildManager API · Design Spec v2.0 · .NET 10 · PostgreSQL*
