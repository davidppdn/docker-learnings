public static class Program
{
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

        app.MapGet("/tasks", () =>
        {
            // Temporary hardcoded tasks for quick setup of the API.
            return new[]
            {
                new { Id = 1, Title = "Learn Docker", Completed = false },
                new { Id = 2, Title = "Build Task Manager", Completed = false }
            };
        });

        app.Run();
    }
}