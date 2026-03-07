namespace GuildManagerApi.Application.DTOs;

public record RaidWeekDto(
    int Id,
    string Label,
    DateTime StartsAt,
    DateTime EndsAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<string> ReportCodes
);

public record RaidWeekSummaryDto(
    int Id,
    string Label,
    DateTime StartsAt,
    DateTime EndsAt,
    int ReportCount
);

public record CreateRaidWeekRequest(
    /// <summary>Rótulo da semana (ex: "S1W3 — Aberrus"). Máx 128 caracteres.</summary>
    string Label,
    /// <summary>
    /// Data de início — deve ser uma Terça-Feira (qualquer hora, normalizada para 00:00 UTC).
    /// </summary>
    DateTime StartsAt,
    /// <summary>Report codes a associar imediatamente (opcional).</summary>
    List<string>? ReportCodes = null
);

public record UpdateRaidWeekRequest(
    string Label,
    DateTime StartsAt
);
