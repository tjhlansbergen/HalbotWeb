using Microsoft.AspNetCore.Mvc;

public static class LogEndpoints
{
    public static void MapLogEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/logs").RequireAuthorization();

        // GET /api/logs
        group.MapGet("/", async ([FromServices] LogQueries logs) =>
        await GetAll(logs));
    }

    private static async Task<IResult> GetAll(LogQueries logs)
    {
        var result = await logs.ReadAllOrderedAsync();
        return Results.Ok(result);
    }
}
