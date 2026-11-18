#!/usr/bin/env bash
# GuildManagerApi na VPS, em duas etapas. Rode como root.
#
#   sudo bash guild.sh preparar
#       Cria o usuário do sistema, o papel 'guildapi' no PostgreSQL (senha nova), o Redis com senha (só no loopback) e o arquivo de ambiente
#       /etc/guildmanager/guild.env com segredos NOVOS (senha do banco, do Redis e chave do JWT).
#   (aqui: sudo bash restaurar-bancos.sh <exportacao.tar.gz> --dono=guildapi guild_manager_v2)
#   sudo bash guild.sh instalar <guild-publish.tar.gz> <dominio> <email>
#       Primeira instalação: publish (self-contained linux-x64), serviço systemd, site no nginx e HTTPS.
#   sudo bash guild.sh atualizar <guild-publish.tar.gz>
#       Versão nova: backup do banco, troca do link 'atual', reinício e teste de saúde (volta sozinho se falhar).
#       As migrações do EF rodam na subida da API (MigrateAsync em Program.cs).
#   sudo bash guild.sh reverter
#       Volta para a versão instalada antes da atual.
#
# O pacote vem de  deploy\publicar.ps1  (gera guild-publish.tar.gz).
#
# A chave Encryption:MasterKey NÃO é gerada aqui: ela cifra campos que já estão no banco restaurado (tokens OAuth e credenciais) e vem
# do appsettings.json que acompanha o publish. Trocá-la exige recifrar esses campos.
set -euo pipefail
export PATH=/usr/sbin:/usr/bin:/sbin:/bin

ACAO="${1:?Uso: guild.sh preparar | instalar <publish.tar.gz> <dominio> <email> | atualizar <publish.tar.gz> | reverter}"
AQUI="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV=/etc/guildmanager/guild.env
BANCO=guild_manager_v2
[ "$(id -u)" -eq 0 ] || { echo "Rode como root (sudo)." >&2; exit 1; }
export DEBIAN_FRONTEND=noninteractive

preparar() {
    echo "==> Pacotes"
    apt-get update -y
    apt-get install -y redis-server libicu-dev tzdata fontconfig ca-certificates curl openssl postgresql-client
    command -v nginx > /dev/null || { echo "nginx não encontrado." >&2; exit 1; }

    echo "==> Usuário e pastas"
    id guildapi > /dev/null 2>&1 || useradd --system --home-dir /opt/guildmanager --shell /usr/sbin/nologin guildapi
    mkdir -p /opt/guildmanager/releases /etc/guildmanager
    chmod 755 /opt/guildmanager

    if [ -f "$ENV" ]; then
        echo "    $ENV já existe: segredos mantidos."
    else
        SENHA_DB="$(openssl rand -hex 24)"
        SENHA_REDIS="$(openssl rand -hex 24)"
        JWT="$(openssl rand -hex 48)"

        echo "==> Papel 'guildapi' no PostgreSQL"
        runuser -u postgres -- psql -X -v ON_ERROR_STOP=1 -v senha="$SENHA_DB" <<'SQL'
DO $$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'guildapi') THEN CREATE ROLE guildapi LOGIN; END IF;
END $$;
ALTER ROLE guildapi PASSWORD :'senha';
SQL

        echo "==> Redis (só loopback, com senha)"
        sed -i -E "s/^#?\s*bind .*/bind 127.0.0.1 -::1/; s/^#?\s*protected-mode .*/protected-mode yes/" /etc/redis/redis.conf
        sed -i -E '/^#?\s*requirepass /d' /etc/redis/redis.conf
        printf 'requirepass %s\n' "$SENHA_REDIS" >> /etc/redis/redis.conf
        systemctl enable redis-server > /dev/null
        systemctl restart redis-server

        echo "==> $ENV"
        install -m 0640 -o root -g guildapi /dev/null "$ENV"
        cat > "$ENV" <<FIM
ASPNETCORE_URLS=http://127.0.0.1:5173
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=$BANCO;Username=guildapi;Password=$SENHA_DB
ConnectionStrings__Redis=localhost:6379,password=$SENHA_REDIS,abortConnect=false
Jwt__SecretKey=$JWT
Backup__StorageDirectory=/var/lib/guildmanager/backups
FIM
    fi
    echo
    echo "Pronto. Agora restaure o banco:"
    echo "  sudo bash restaurar-bancos.sh ~/exportacao-*.tar.gz --dono=guildapi $BANCO"
    echo "Depois:  sudo bash guild.sh instalar ~/guild-publish.tar.gz woodenflipflop.myvnc.com SEU_EMAIL"
}

publicar_release() {
    PACOTE="$1"
    [ -f "$PACOTE" ] || { echo "Não achei $PACOTE." >&2; exit 1; }
    [ -f "$ENV" ] || { echo "Falta $ENV: rode antes  guild.sh preparar." >&2; exit 1; }
    runuser -u postgres -- psql -X -qAt -c "select 1 from pg_database where datname='$BANCO'" | grep -q 1 \
        || { echo "O banco $BANCO não existe: restaure-o antes (restaurar-bancos.sh --dono=guildapi $BANCO)." >&2; exit 1; }

    TS="$(date +%Y%m%d-%H%M%S)"
    REL="/opt/guildmanager/releases/$TS"
    echo "==> Instalando $TS"
    mkdir -p "$REL"
    # O tar do Windows traz a pasta 'publish/': --strip-components=1 a remove.
    tar -xzf "$PACOTE" -C "$REL" --strip-components=1 --no-same-owner
    [ -f "$REL/GuildManagerApi.Api" ] || { echo "GuildManagerApi.Api não está no pacote (esperado: pasta publish/ compactada)." >&2; exit 1; }
    chmod +x "$REL/GuildManagerApi.Api"
    chown -R root:root "$REL"
    chmod -R a+rX "$REL"
    ANTERIOR="$(readlink -f /opt/guildmanager/atual 2> /dev/null || true)"
    ln -sfn "$REL" /opt/guildmanager/atual

    cat > /etc/systemd/system/guildmanager-api.service <<'FIM'
[Unit]
Description=GuildManagerApi
After=network-online.target postgresql.service redis-server.service
Wants=network-online.target

[Service]
Type=simple
User=guildapi
Group=guildapi
WorkingDirectory=/opt/guildmanager/atual
ExecStart=/opt/guildmanager/atual/GuildManagerApi.Api
EnvironmentFile=/etc/guildmanager/guild.env
Environment=HOME=/var/lib/guildmanager
Environment=DOTNET_CLI_TELEMETRY_OPTOUT=1
Restart=on-failure
RestartSec=5
KillSignal=SIGINT
TimeoutStopSec=30
StateDirectory=guildmanager
LogsDirectory=GuildManagerApi
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
PrivateTmp=true
PrivateDevices=true
ProtectKernelTunables=true
ProtectControlGroups=true
RestrictSUIDSGID=true

[Install]
WantedBy=multi-user.target
FIM
    systemctl daemon-reload
    systemctl enable guildmanager-api > /dev/null
    systemctl restart guildmanager-api

    echo "==> Aguardando a API (até 3 min)"
    if ! curl -s -o /dev/null --retry 60 --retry-delay 3 --retry-connrefused --max-time 5 http://127.0.0.1:5173/; then
        echo "!! A API não respondeu. Log:" >&2
        journalctl -u guildmanager-api -n 40 --no-pager >&2 || true
        if [ -n "$ANTERIOR" ] && [ -d "$ANTERIOR" ]; then ln -sfn "$ANTERIOR" /opt/guildmanager/atual; systemctl restart guildmanager-api; fi
        exit 1
    fi
    echo "    respondeu"
    ls -1dt /opt/guildmanager/releases/*/ | tail -n +6 | xargs -r rm -rf
}

# Primeira instalação: release + nginx + HTTPS.
instalar() {
    DOMINIO="${3:?Uso: guild.sh instalar <publish.tar.gz> <dominio> <email>}"
    EMAIL="${4:?Falta o e-mail (certbot)}"
    publicar_release "${2:?Uso: guild.sh instalar <publish.tar.gz> <dominio> <email>}"

    echo "==> nginx ($DOMINIO)"
    sed "s|__DOMINIO__|$DOMINIO|" "$AQUI/nginx-guild.conf" > /etc/nginx/conf.d/guild.conf
    nginx -t
    systemctl reload nginx
    if ! certbot --nginx -d "$DOMINIO" --non-interactive --agree-tos --redirect -m "$EMAIL"; then
        echo "    certbot falhou (o DNS de $DOMINIO aponta para esta VPS?). O site segue em HTTP." >&2
    fi
    echo "Versão $TS no ar:  https://$DOMINIO"
}

# Versão nova: não mexe em nginx/certbot.
atualizar() {
    PACOTE="${2:?Uso: guild.sh atualizar <publish.tar.gz>}"
    [ -f "$PACOTE" ] || { echo "Não achei $PACOTE." >&2; exit 1; }
    echo "==> Backup do banco"
    bash "$AQUI/backup.sh"
    publicar_release "$PACOTE"
    echo "Versão $TS no ar."
}

reverter() {
    ATUAL="$(readlink -f /opt/guildmanager/atual)"
    ANTERIOR="$(ls -1dt /opt/guildmanager/releases/*/ | sed 's:/$::' | grep -vx "$ATUAL" | head -n 1 || true)"
    [ -n "$ANTERIOR" ] || { echo "Não há versão anterior guardada." >&2; exit 1; }
    echo "Voltando de $(basename "$ATUAL") para $(basename "$ANTERIOR")"
    ln -sfn "$ANTERIOR" /opt/guildmanager/atual
    systemctl restart guildmanager-api
    curl -s -o /dev/null --retry 60 --retry-delay 3 --retry-connrefused --max-time 5 http://127.0.0.1:5173/ && echo "API respondeu."
    echo "Obs.: migrações do banco não são desfeitas; se a versão antiga não roda com o schema novo, restaure um backup."
}

case "$ACAO" in
    preparar) preparar ;;
    instalar) instalar "$@" ;;
    atualizar) atualizar "$@" ;;
    reverter) reverter ;;
    *) echo "Uso: guild.sh preparar | instalar <publish.tar.gz> <dominio> <email> | atualizar <publish.tar.gz> | reverter" >&2; exit 1 ;;
esac
