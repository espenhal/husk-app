using Husk.Api.Models;
using Husk.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddSingleton<TaskStore>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var tasks = app.MapGroup("/api/tasks");

tasks.MapGet("", async (TaskStore store, CancellationToken cancellationToken) =>
{
    var allTasks = await store.GetAllAsync(cancellationToken);
    return Results.Ok(allTasks);
});

tasks.MapPost("", async (CreateTaskRequest request, TaskStore store, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { error = "Title is required." });
    }

    var task = await store.AddAsync(request, cancellationToken);
    return Results.Created($"/api/tasks/{task.Id}", task);
});

tasks.MapPut("/{id:guid}", async (
    Guid id,
    UpdateTaskRequest request,
    TaskStore store,
    CancellationToken cancellationToken) =>
{
    if (request.Title is not null && string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { error = "Title cannot be empty." });
    }

    var task = await store.UpdateAsync(id, request, cancellationToken);
    return task is null ? Results.NotFound() : Results.Ok(task);
});

tasks.MapDelete("/{id:guid}", async (Guid id, TaskStore store, CancellationToken cancellationToken) =>
{
    var deleted = await store.DeleteAsync(id, cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();
