namespace GuildManagerApi.Domain.Interfaces;

public interface IImportProgressHub
{
    Task BroadcastAsync(ImportProgressEvent evt, CancellationToken ct = default);
}
