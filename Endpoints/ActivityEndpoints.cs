using Microsoft.AspNetCore.Mvc;

public static class ActivityEndpoints
{
    public static void MapActivityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/activities").RequireAuthorization();

        // GET /api/activities
        group.MapGet("/", async ([FromServices] ActivityCache activities) =>
        await GetAll(activities));

        // GET /api/activities/{id}
        //

        // POST /api/activities
        group.MapPost("/", async (
            [FromServices] ActivityFetcher fetcher, 
            [FromServices] ActivityQueries queries,
            [FromServices] ActivityCache activities, 
            long garminId,
            DateTime? date = null
            ) => await PostActivity(fetcher, queries, activities, garminId, date, app.Logger));

        // DELETE /api/activities/{id}
        group.MapDelete("/{id:long}", async (
            [FromServices] ActivityQueries queries,
            [FromServices] ActivityCache activities,
            long id
            ) => await DeleteActivity(queries, activities, id, app.Logger));
    }

    private static async Task<IResult> GetAll(ActivityCache activities)
    {
        var result = await activities.Get();
        var ordered = result.OrderByDescending(activity => activity.Date);
        return Results.Ok(ordered);
    }

    private static async Task<IResult> PostActivity(ActivityFetcher fetcher, ActivityQueries queries, ActivityCache activities, long garminId, DateTime? date, ILogger logger)
    {
        try
        {
            var activity = fetcher.Fetch(garminId, date);
            queries.InsertAsync(activity).Wait();
            activities.InvalidateCache();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Added Garmin activity with ID {activity.Id}", activity.Id);
            }

            return Results.Created($"/api/activities/{activity.Id}", activity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching activity with Garmin ID {garminId}", garminId);
            return Results.Problem($"Error fetching activity with Garmin ID {garminId}: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteActivity(ActivityQueries queries, ActivityCache activities, long id, ILogger logger)
    {
        try
        {
            var deletedCount = await queries.DeleteAsync(id);
            activities.InvalidateCache();

            if (deletedCount == 0)
            {
                return Results.NotFound();
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Deleted Garmin activity with ID {Id}", id);
            }

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting activity with ID {Id}", id);
            return Results.Problem($"Error deleting activity with ID {id}: {ex.Message}");
        }
    }
}
