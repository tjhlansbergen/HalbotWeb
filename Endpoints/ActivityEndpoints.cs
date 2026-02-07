using Microsoft.AspNetCore.Mvc;

public static class ActivityEndpoints
{
    public static void MapActivityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/activities");

        // GET /api/activities
        group.MapGet("/", async ([FromServices] ActivityQueries queries) => await GetAll(queries));

        // GET /api/activities/{id}
        //

        // POST /api/activities
        //

        // DELETE /api/activities/{id}
        //
    }

    private static async Task<IResult> GetAll(ActivityQueries queries)
    {
        var result = await queries.GetAllAsync();
        return Results.Ok(result);
    }
}
