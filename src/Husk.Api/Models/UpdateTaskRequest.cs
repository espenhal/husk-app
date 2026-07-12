namespace Husk.Api.Models;

public sealed record UpdateTaskRequest
{
    public string? Title { get; init; }

    public string? Notes { get; init; }

    public bool? IsCompleted { get; init; }
}
