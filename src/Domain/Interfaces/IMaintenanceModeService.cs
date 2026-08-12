namespace GuildManagerApi.Domain.Interfaces;

public interface IMaintenanceModeService
{
    bool IsActive { get; }
    void Enter(Guid restoreId);
    void Exit(Guid restoreId);
}
