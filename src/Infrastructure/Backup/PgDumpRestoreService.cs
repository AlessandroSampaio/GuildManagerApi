using System.Diagnostics;
using System.Text;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GuildManagerApi.Infrastructure.Backup;

public interface IDatabaseBackupService
{
    Task<long> DumpAsync(string outputFilePath, Guid backupId, IProgress<BackupProgressEvent>? progress, CancellationToken ct);
    Task RestoreAsync(string inputFilePath, Guid restoreId, IProgress<RestoreProgressEvent>? progress, CancellationToken ct);
    Task<bool> IsValidCustomFormatDumpAsync(string filePath, CancellationToken ct);
}

/// <summary>
/// Invoca pg_dump/pg_restore como processos externos, usando as credenciais de
/// ConnectionStrings:DefaultConnection. A senha nunca é passada por linha de comando —
/// vai via variável de ambiente PGPASSWORD do processo filho.
/// </summary>
public class PgDumpRestoreService(
        IConfiguration configuration,
        IOptions<BackupSettings> settings,
        ILogger<PgDumpRestoreService> logger) : IDatabaseBackupService
{
    private readonly BackupSettings _settings = settings.Value;
    private readonly ILogger<PgDumpRestoreService> _logger = logger;
    private static readonly byte[] MagicBytes = "PGDMP"u8.ToArray();

    public async Task<long> DumpAsync(
        string outputFilePath, Guid backupId, IProgress<BackupProgressEvent>? progress, CancellationToken ct)
    {
        var csb = BuildConnectionStringBuilder();
        progress?.Report(new BackupProgressEvent(backupId, BackupPhase.Dumping, "Executando pg_dump."));

        var psi = BuildProcessStartInfo(
            _settings.PgDumpPath, csb,
            $"-Fc -f \"{outputFilePath}\" -d \"{csb.Database}\"");

        await RunProcessAsync(psi, "pg_dump", ct);

        progress?.Report(new BackupProgressEvent(backupId, BackupPhase.Finalizing, "Finalizando arquivo de backup."));
        return new FileInfo(outputFilePath).Length;
    }

    public async Task RestoreAsync(
        string inputFilePath, Guid restoreId, IProgress<RestoreProgressEvent>? progress, CancellationToken ct)
    {
        var csb = BuildConnectionStringBuilder();
        progress?.Report(new RestoreProgressEvent(restoreId, RestorePhase.Restoring, "Executando pg_restore."));

        var psi = BuildProcessStartInfo(
            _settings.PgRestorePath, csb,
            $"--clean --if-exists -d \"{csb.Database}\" \"{inputFilePath}\"");

        await RunProcessAsync(psi, "pg_restore", ct);

        progress?.Report(new RestoreProgressEvent(restoreId, RestorePhase.Finalizing, "Restore finalizado."));
    }

    public async Task<bool> IsValidCustomFormatDumpAsync(string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath)) return false;

        var buffer = new byte[MagicBytes.Length];
        await using var fs = File.OpenRead(filePath);
        var read = await fs.ReadAsync(buffer, ct);
        return read == MagicBytes.Length && buffer.AsSpan().SequenceEqual(MagicBytes);
    }

    private NpgsqlConnectionStringBuilder BuildConnectionStringBuilder()
        => new(configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured."));

    private static ProcessStartInfo BuildProcessStartInfo(string exe, NpgsqlConnectionStringBuilder csb, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = $"-h {csb.Host} -p {csb.Port} -U {csb.Username} {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.EnvironmentVariables["PGPASSWORD"] = csb.Password;
        return psi;
    }

    private async Task RunProcessAsync(ProcessStartInfo psi, string toolName, CancellationToken ct)
    {
        using var process = new Process { StartInfo = psi };
        var stderr = new StringBuilder();
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(_settings.ProcessTimeoutMinutes));

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to start {toolName} ('{psi.FileName}'). Verify Backup:PgDumpPath/PgRestorePath in configuration.", ex);
        }

        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }

            if (!ct.IsCancellationRequested)
                throw new InvalidOperationException($"{toolName} timed out after {_settings.ProcessTimeoutMinutes} minute(s).");

            throw;
        }

        if (process.ExitCode != 0)
        {
            _logger.LogError("{Tool} exited with code {Code}: {Stderr}", toolName, process.ExitCode, stderr);
            throw new InvalidOperationException($"{toolName} exited with code {process.ExitCode}: {stderr}");
        }
    }
}
