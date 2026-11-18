# Gera guild-publish.tar.gz (self-contained linux-x64) para o guild.sh. Rode na raiz do repositório:
#   pwsh deploy\publicar.ps1
# O appsettings.json (Encryption:MasterKey, WarcraftLogs, BattleNet, RaiderIo...) vai junto, copiado de src\Api: confira-o antes.
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$saida = Join-Path $raiz 'deploy\saida'
$pub = Join-Path $saida 'publish'
if (Test-Path $saida) { Remove-Item $saida -Recurse -Force }
New-Item -ItemType Directory -Path $saida | Out-Null

if (-not (Test-Path (Join-Path $raiz 'src\Api\appsettings.json'))) { throw 'Falta src\Api\appsettings.json (copie do appsettings.example.json).' }

dotnet publish (Join-Path $raiz 'src\Api') -c Release -r linux-x64 --self-contained true -o $pub
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish falhou' }

# O guild.sh remove a pasta 'publish/' com --strip-components=1.
tar -czf (Join-Path $saida 'guild-publish.tar.gz') -C $saida publish
if ($LASTEXITCODE -ne 0) { throw 'tar falhou' }
Remove-Item $pub -Recurse -Force
Write-Host "Pronto: $saida\guild-publish.tar.gz"
Write-Host 'Envie para a VPS junto com guild.sh, backup.sh e nginx-guild.conf (pasta deploy/vps).'
