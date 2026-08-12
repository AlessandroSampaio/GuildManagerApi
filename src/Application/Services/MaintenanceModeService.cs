using GuildManagerApi.Domain.Interfaces;

namespace GuildManagerApi.Application.Services;

/// <summary>
/// Contador em memória: um restore ativo bloqueia requisições mutáveis via
/// <c>MaintenanceModeMiddleware</c>. Não sobrevive a um restart do processo —
/// aceitável, pois um pg_restore interrompido já deixa o banco inconsistente
/// independente do que este flag faça.
/// </summary>
public sealed class MaintenanceModeService : IMaintenanceModeService
{
    private int _activeCount;

    public bool IsActive => Volatile.Read(ref _activeCount) > 0;

    public void Enter(Guid restoreId) => Interlocked.Increment(ref _activeCount);

    public void Exit(Guid restoreId) => Interlocked.Decrement(ref _activeCount);
}
