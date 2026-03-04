namespace GuildManagerApi.Application.DTOs;

public record ImportAcceptedDto(
    string ReportCode,
    string Status,
    string WsUrl,
    string Message
);

public record ImportResultDto(
    string ReportCode,
    string Title,
    int FightsImported,
    int KillsImported,
    int PlayersImported,
    int PerformanceEntriesSaved,
    string? GuildName
);
