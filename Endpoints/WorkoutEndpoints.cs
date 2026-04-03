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

        // PUT /api/workouts/{id}
        group.MapPut("/{id:long}", async (
            [FromServices] WorkoutQueries queries,
            [FromServices] WorkoutCache workouts,
            long id,
            [FromBody] WorkoutRecord record
            ) => await PutWorkout(queries, workouts, id, record, app.Logger));

        // DELETE /api/workouts/{id}
        group.MapDelete("/{id:long}", async (
            [FromServices] WorkoutQueries queries,
            [FromServices] WorkoutCache workouts,
            long id
            ) => await DeleteWorkout(queries, workouts, id, app.Logger));
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

    private static async Task<IResult> PutWorkout(WorkoutQueries queries, WorkoutCache workouts, long id, WorkoutRecord record, ILogger logger)
    {
        try
        {
            record.Id = id;
            var updatedCount = await queries.UpdateAsync(record);

            if (updatedCount == 0)
            {
                return Results.NotFound();
            }

            workouts.InvalidateCache();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Updated workout record with ID {Id}", id);
            }

            return Results.Ok(record);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating workout record with ID {Id}", id);
            return Results.Problem($"Error updating workout record with ID {id}: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteWorkout(WorkoutQueries queries, WorkoutCache workouts, long id, ILogger logger)
    {
        try
        {
            var deletedCount = await queries.DeleteAsync(id);
            workouts.InvalidateCache();

            if (deletedCount == 0)
            {
                return Results.NotFound();
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Deleted workout record with ID {Id}", id);
            }

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting workout record with ID {Id}", id);
            return Results.Problem($"Error deleting workout record with ID {id}: {ex.Message}");
        }
    }
}
