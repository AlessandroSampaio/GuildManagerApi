# GuildManagerApi

ASP.NET Core REST API para gerenciamento de guilds de World of Warcraft, com integração WarcraftLogs, Battle.net e Raider.IO.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 15+
- Redis

## Configuração

Copie o arquivo de exemplo e preencha as variáveis:

```bash
cp src/Api/appsettings.example.json src/Api/appsettings.json
```

### Seções obrigatórias

#### `ConnectionStrings`
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=guildmanager;Username=postgres;Password=SUA_SENHA",
  "Redis": "localhost:6379,password=SUA_SENHA_REDIS,abortConnect=false"
}
```

#### `Encryption`
Chave AES-256 para criptografia de credenciais OAuth no banco. Gere com:
```csharp
Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
```
```json
"Encryption": {
  "MasterKey": "CHAVE_BASE64_DE_32_BYTES"
}
```

#### `Jwt`
```json
"Jwt": {
  "SecretKey": "CHAVE_SECRETA_DE_PELO_MENOS_32_CARACTERES",
  "Issuer": "GuildManagerApi",
  "Audience": "GuildManagerApi",
  "AccessTokenExpiryMinutes": 60,
  "RefreshTokenExpiryDays": 30
}
```

### Seções opcionais

#### `WarcraftLogs`
Necessário para importar logs e sincronizar personagens.
```json
"WarcraftLogs": {
  "TokenEndpoint": "https://www.warcraftlogs.com/oauth/token",
  "AuthorizeEndpoint": "https://www.warcraftlogs.com/oauth/authorize",
  "PublicGraphQlEndpoint": "https://www.warcraftlogs.com/api/v2/client",
  "PrivateGraphQlEndpoint": "https://www.warcraftlogs.com/api/v2/user",
  "RedirectUri": "https://localhost:5001/api/wcl-auth/callback"
}
```

#### `BattleNet`
Necessário para integração com perfis Battle.net.
```json
"BattleNet": {
  "AuthorizeEndpoint": "https://oauth.battle.net/authorize",
  "TokenEndpoint": "https://oauth.battle.net/token",
  "UserInfoEndpoint": "https://oauth.battle.net/userinfo",
  "Scope": "wow.profile openid",
  "RedirectUri": "https://localhost:5173/api/profile/bnet/callback",
  "FrontendCallbackUrl": "http://localhost:1420/api/profile/bnet/callback"
}
```

#### `RaiderIoSync`
```json
"RaiderIoSync": {
  "SyncIntervalHours": 6,
  "ThrottleDelayMs": 300
}
```

## Comandos

```bash
# Build
dotnet build

# Rodar a API (HTTP: localhost:5173 / HTTPS: localhost:7283)
dotnet run --project src/Api

# Rodar todos os testes
dotnet test

# Rodar um teste específico
dotnet test tests/GuildManagerApi.Tests/GuildManagerApi.Tests.csproj --filter "FullyQualifiedName~TestName"

# Adicionar uma migration
dotnet ef migrations add NomeDaMigration --project src/Api --startup-project src/Api

# Aplicar migrations manualmente
dotnet ef database update --project src/Api --startup-project src/Api
```

> As migrations são aplicadas automaticamente na inicialização da API.

## Swagger

Disponível em `/swagger` apenas no ambiente de desenvolvimento.

## Autenticação

### Fluxo local (JWT)

| Endpoint | Descrição |
|---|---|
| `POST /api/auth/register` | Cadastro de novo usuário |
| `POST /api/auth/login` | Login — retorna `accessToken` + `refreshToken` |
| `POST /api/auth/refresh` | Renova o access token via refresh token (rotation) |
| `POST /api/auth/logout` | Revoga a sessão atual |
| `POST /api/auth/logout-all` | Revoga todas as sessões do usuário |
| `PATCH /api/auth/change-password` | Altera senha (requer senha atual) |
| `GET /api/auth/me` | Retorna informações do usuário autenticado |

### Redefinição de senha (self-service)

Validação interna — sem envio de e-mail. O usuário confirma username e e-mail cadastrados.

| Endpoint | Descrição |
|---|---|
| `POST /api/auth/reset-password` | Confirma username + e-mail e define a nova senha |

**Body:** `{ "username": "...", "email": "...", "newPassword": "..." }`

Se username e e-mail corresponderem a um usuário ativo, a senha é redefinida e todas as sessões ativas são revogadas. Caso contrário, retorna 422.

### Redefinição de senha (admin)

| Endpoint | Descrição |
|---|---|
| `POST /api/admin/users/reset-password` | Admin redefine a senha de qualquer usuário |

**Body:** `{ "userId": "...", "newPassword": "..." }` — se `newPassword` for omitido, uma senha temporária de 16 caracteres é gerada e retornada na resposta.

## Variáveis de ambiente (produção)

Prefira sobrescrever segredos via variáveis de ambiente em vez de editar `appsettings.json`:

```bash
ConnectionStrings__DefaultConnection="..."
ConnectionStrings__Redis="..."
Jwt__SecretKey="..."
Encryption__MasterKey="..."
```
