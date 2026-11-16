namespace TaskManager.Api.Models;

public record TaskModel(
    Guid Id,
    string Title,
    string Description,
    bool Completed
);
