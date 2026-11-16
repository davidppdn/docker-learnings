using TaskManager.Api.Models;

public static class Program
{
    public static List<TaskModel> Tasks { get; } = new List<TaskModel>
    {
        new TaskModel(Guid.NewGuid(), "Learn Docker", "Learn how to use Docker for containerization.", false),
        new TaskModel(Guid.NewGuid(), "Build Task Manager", "Create a simple task manager application.", false)
    };


    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenApi();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        // Basic API for in-memory task management
        app.MapGet("/tasks", () =>
        {
            return Tasks;
        });

        app.MapGet("/tasks/{id}", (Guid id) =>
        {
            var task = Tasks.FirstOrDefault(t => t.Id == id);
            return task is not null ? Results.Ok(task) : Results.NotFound();
        });

        app.MapPost("/tasks", (TaskModel task) =>
        {   
            if (string.IsNullOrWhiteSpace(task.Title))
            {
                return Results.BadRequest("Task title cannot be empty.");
            }

            var newTask = task with { Id = Guid.NewGuid() };
            Tasks.Add(newTask);
            return Results.Created($"/tasks/{newTask.Id}", newTask);
        });

        app.MapPut("/tasks/{id}", (Guid id, TaskModel updatedTask) =>
        {
            var index = Tasks.FindIndex(t => t.Id == id);
            if (index == -1)
            {
                return Results.NotFound();
            }

            if (string.IsNullOrWhiteSpace(updatedTask.Title))
            {
                return Results.BadRequest("Task title cannot be empty.");
            }

            var taskToUpdate = Tasks[index];
            var newTask = taskToUpdate with
            {
                Title = updatedTask.Title,
                Description = updatedTask.Description,
                Completed = updatedTask.Completed
            };
            Tasks[index] = newTask;
            return Results.Ok(newTask);
        });

        app.MapDelete("/tasks/{id}", (Guid id) =>
        {
            var task = Tasks.FirstOrDefault(t => t.Id == id);
            if (task is null)
            {
                return Results.NotFound();
            }
            Tasks.Remove(task);
            return Results.NoContent();
        });

        app.Run();
    }
}