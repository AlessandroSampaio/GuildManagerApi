namespace GuildManagerApi.Application.DTOs;

public record PenaltyEventDto(
    int Id,
    string Description,
    int Points,
    DateTime CreatedAt
);

public record CreatePenaltyEventRequest(string Description, int Points);

public record UpdatePenaltyEventRequest(string Description, int Points);

public record PlayerWeekPenaltyDto(
    int Id,
    int PlayerId,
    string PlayerName,
    int RaidWeekId,
    int PenaltyEventId,
    string PenaltyDescription,
    int Points,
    string? Note,
    DateTime CreatedAt
);

public record AddPlayerPenaltyRequest(int PlayerId, int PenaltyEventId, string? Note);
