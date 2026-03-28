using Microsoft.AspNetCore.Mvc;

public static class WorkoutEndpoints
{
    public static void MapWorkoutEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/workouts").RequireAuthorization();

        // GET /api/workouts
        group.MapGet("/", async ([FromServices] WorkoutCache workouts) =>
        await GetAll(workouts));

        // POST /api/workouts
        group.MapPost("/", async (
            [FromServices] WorkoutQueries queries,
            [FromServices] WorkoutCache workouts, 
            [FromBody] WorkoutRecord record
            ) => await PostWorkout(queries, workouts, record, app.Logger));
    }

    private static async Task<IResult> GetAll(WorkoutCache workouts)
    {
        var result = await workouts.Get();
        var ordered = result.OrderByDescending(workout => workout.Date);
        return Results.Ok(ordered);
    }

    private static async Task<IResult> PostWorkout(WorkoutQueries queries, WorkoutCache workouts, WorkoutRecord record, ILogger logger)
    {
        try
        {
            await queries.InsertAsync(record);
            workouts.InvalidateCache();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Added workout record for {date} with {minutes} minutes", record.Date, record.Minutes);
            }

            return Results.Created($"/api/workouts", record);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding workout record");
            return Results.Problem($"Error adding workout record: {ex.Message}");
        }
    }
}
