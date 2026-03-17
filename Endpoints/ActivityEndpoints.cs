using Microsoft.AspNetCore.Mvc;

public static class ActivityEndpoints
{
    public static void MapActivityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/activities").RequireAuthorization();

        // GET /api/activities
        group.MapGet("/", async ([FromServices] ActivityCache activities) => await GetAll(activities));

        // GET /api/activities/{id}
        //

        // POST /api/activities
        //

        // DELETE /api/activities/{id}
        // carefull here!
    }

    private static async Task<IResult> GetAll(ActivityCache activities)
    {
        var result = await activities.Get();
        var ordered = result.OrderByDescending(activity => activity.Date);
        return Results.Ok(ordered);
    }
}
