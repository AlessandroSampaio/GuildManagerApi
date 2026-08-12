namespace GuildManagerApi.Infrastructure.Backup;

public class BackupSettings
{
    public const string Section = "Backup";

    public string PgDumpPath { get; set; } = "pg_dump";
    public string PgRestorePath { get; set; } = "pg_restore";
    public string StorageDirectory { get; set; } = "backups";
    public long MaxUploadSizeBytes { get; set; } = 2L * 1024 * 1024 * 1024;
    public int ProcessTimeoutMinutes { get; set; } = 60;

    public string ResolvedStorageDirectory =>
        Path.IsPathRooted(StorageDirectory)
            ? StorageDirectory
            : Path.Combine(AppContext.BaseDirectory, StorageDirectory);
}
