namespace Husk.Api.Models;

public sealed record CreateTaskRequest
{
    public string Title { get; init; } = string.Empty;

    public string? Notes { get; init; }
}
