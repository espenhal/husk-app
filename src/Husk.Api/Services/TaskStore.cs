using System.Text.Json;
using Husk.Api.Models;

namespace Husk.Api.Services;

public sealed class TaskStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public TaskStore(IConfiguration configuration, IHostEnvironment environment)
    {
        _filePath = configuration["HUSK_DATA_PATH"]
            ?? Path.Combine(environment.ContentRootPath, "App_Data", "tasks.json");
    }

    public async Task<IReadOnlyList<ReminderTask>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var tasks = await LoadCoreAsync(cancellationToken);

            return tasks
                .OrderBy(task => task.IsCompleted)
                .ThenByDescending(task => task.CreatedAt)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ReminderTask> AddAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var tasks = await LoadCoreAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var task = new ReminderTask(
                Guid.NewGuid(),
                request.Title.Trim(),
                Normalize(request.Notes),
                IsCompleted: false,
                CreatedAt: now,
                CompletedAt: null);

            tasks.Add(task);
            await SaveCoreAsync(tasks, cancellationToken);
            return task;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ReminderTask?> UpdateAsync(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var tasks = await LoadCoreAsync(cancellationToken);
            var index = tasks.FindIndex(task => task.Id == id);

            if (index < 0)
            {
                return null;
            }

            var existing = tasks[index];
            var isCompleted = request.IsCompleted ?? existing.IsCompleted;
            var completedAt = ResolveCompletedAt(existing, isCompleted);
            var updated = existing with
            {
                Title = request.Title is null ? existing.Title : request.Title.Trim(),
                Notes = request.Notes is null ? existing.Notes : Normalize(request.Notes),
                IsCompleted = isCompleted,
                CompletedAt = completedAt
            };

            tasks[index] = updated;
            await SaveCoreAsync(tasks, cancellationToken);
            return updated;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var tasks = await LoadCoreAsync(cancellationToken);
            var removed = tasks.RemoveAll(task => task.Id == id) > 0;

            if (removed)
            {
                await SaveCoreAsync(tasks, cancellationToken);
            }

            return removed;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<ReminderTask>> LoadCoreAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_filePath);
        var tasks = await JsonSerializer.DeserializeAsync<List<ReminderTask>>(
            stream,
            _jsonOptions,
            cancellationToken);

        return tasks ?? [];
    }

    private async Task SaveCoreAsync(List<ReminderTask> tasks, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{_filePath}.tmp";

        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, tasks, _jsonOptions, cancellationToken);
        }

        File.Move(temporaryPath, _filePath, overwrite: true);
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static DateTimeOffset? ResolveCompletedAt(ReminderTask existing, bool isCompleted)
    {
        if (!isCompleted)
        {
            return null;
        }

        return existing.IsCompleted ? existing.CompletedAt : DateTimeOffset.UtcNow;
    }
}
