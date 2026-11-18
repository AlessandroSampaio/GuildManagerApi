#!/usr/bin/env bash
# Backup do banco da GuildManagerApi (pg_dump em formato custom, lido pelo pg_restore). Chamado pelo guild.sh atualizar e,
# se quiser, por cron:  0 3 * * * root bash /opt/guildmanager/backup.sh
# Guarda os 14 mais recentes em /var/backups/guildmanager. Copie-os para FORA da VPS.
set -euo pipefail
BANCO=guild_manager_v2
DESTINO=/var/backups/guildmanager
ARQUIVO="$DESTINO/guild-$(date +%Y%m%d-%H%M%S).dump"

mkdir -p "$DESTINO"
chmod 700 "$DESTINO"
runuser -u postgres -- pg_dump -Fc "$BANCO" > "$ARQUIVO"
chmod 600 "$ARQUIVO"
echo "Backup: $ARQUIVO ($(du -h "$ARQUIVO" | cut -f1))"

ls -1t "$DESTINO"/guild-*.dump | tail -n +15 | xargs -r rm -f
