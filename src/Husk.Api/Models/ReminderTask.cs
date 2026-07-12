namespace Husk.Api.Models;

public sealed record ReminderTask(
    Guid Id,
    string Title,
    string? Notes,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
