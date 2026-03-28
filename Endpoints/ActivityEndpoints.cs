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
        group.MapPost("/", async ([FromServices] ActivityCache activities, long garminId) => 
        await PostActivity(garminId, app.Logger));

        // DELETE /api/activities/{id}
        // carefull here!
    }

    private static async Task<IResult> GetAll(ActivityCache activities)
    {
        var result = await activities.Get();
        var ordered = result.OrderByDescending(activity => activity.Date);
        return Results.Ok(ordered);
    }

    private static async Task<IResult> PostActivity(long garminId, ILogger logger)
    {
        //var activity = await activities.Add(garminId);
        //return Results.Created($"/api/activities/{activity.Id}", activity);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Adding activity with Garmin ID {GarminId}", garminId);
        }
        return Results.Created();
    }
}
