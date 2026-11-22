using TaskManager.Api.Models;
using Npgsql;

public static class Program
{
    public static List<TaskModel> Tasks { get; } = new List<TaskModel>
    {
        new TaskModel(Guid.NewGuid(), "Learn Docker", "Learn how to use Docker for containerization.", false),
        new TaskModel(Guid.NewGuid(), "Build Task Manager", "Create a simple task manager application.", false)
    };


    public static async Task Main(string[] args)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = "taskmanager-db", // Use the container name as the host
            Port = 5432,
            Database = "taskmanager",
            Username = "taskmanager",
            Password = "devpassword"
        };

        var connectionString = connectionStringBuilder.ConnectionString;
        await using var dataSource = NpgsqlDataSource.Create(connectionString);

        await using var command = dataSource.CreateCommand("SELECT current_database();");
        var result = await command.ExecuteScalarAsync();
        Console.WriteLine($"Connected to database: {result}");

        await using var createTableCommand = dataSource.CreateCommand(@"
            CREATE TABLE IF NOT EXISTS tasks (
    id UUID PRIMARY KEY,
    title TEXT NOT NULL,
    description TEXT,
    completed BOOLEAN NOT NULL DEFAULT FALSE
);");

        await createTableCommand.ExecuteNonQueryAsync();

        await using (var readTableCommand = dataSource.CreateCommand("SELECT * FROM tasks;"))
        await using (var readTableReader = await readTableCommand.ExecuteReaderAsync())
        {
            while (await readTableReader.ReadAsync())
            {
                var id = readTableReader.GetGuid(0);
                var title = readTableReader.GetString(1);
                var description = readTableReader.IsDBNull(2)
                    ? null
                    : readTableReader.GetString(2);
                var completed = readTableReader.GetBoolean(3);
                Console.WriteLine($"Row: Id={id}, Title={title}, Description={description}, Completed={completed}");
            }
        }

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