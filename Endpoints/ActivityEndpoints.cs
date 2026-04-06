using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json.Nodes;

public static class ActivityEndpoints
{
    public static void MapActivityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/activities").RequireAuthorization();

        // GET /api/activities
        group.MapGet("/", async ([FromServices] ActivityCache activities) =>
        await GetAll(activities));

        // POST /api/activities
        group.MapPost("/", async (
            [FromServices] ActivityFetcher fetcher, 
            [FromServices] ActivityQueries queries,
            [FromServices] ActivityCache activities, 
            long garminId,
            DateTime? date = null
            ) => await PostActivity(fetcher, queries, activities, garminId, date, app.Logger));

        // PUT /api/activities/{id}
        group.MapPut("/{id:long}", async (
            [FromServices] ActivityQueries queries,
            [FromServices] ActivityCache activities,
            long id,
            [FromBody] ActivityUpdateRequest request
            ) => await PutActivity(queries, activities, id, request, app.Logger));

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

    private static async Task<IResult> PutActivity(ActivityQueries queries, ActivityCache activities, long id, ActivityUpdateRequest request, ILogger logger)
    {
        try
        {
            var existing = await queries.GetByIdAsync(id);
            if (existing is null)
            {
                return Results.NotFound();
            }

            if ((ActivityDataType)existing.DataType != ActivityDataType.Garmin)
            {
                return Results.BadRequest("Only Garmin activities can be edited.");
            }

            if (!TryParsePaceToSpeed(request.Pace, out var speedMetersPerSecond))
            {
                return Results.BadRequest("Pace must be in m:ss format.");
            }

            var updatedSerializedData = UpdateGarminSerializedData(
                existing.SerializedData,
                request.Date,
                request.Distance,
                request.Climb,
                request.Duration,
                speedMetersPerSecond);

            if (updatedSerializedData is null)
            {
                return Results.BadRequest("Garmin serialized data is missing required summary fields.");
            }

            existing.Id = id;
            existing.IsRace = request.IsRace;
            existing.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            existing.Gpx = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            existing.SerializedData = updatedSerializedData;

            var updatedCount = await queries.UpdateAsync(existing);
            if (updatedCount == 0)
            {
                return Results.NotFound();
            }

            activities.InvalidateCache();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Updated Garmin activity with ID {Id}", id);
            }

            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating activity with ID {Id}", id);
            return Results.Problem($"Error updating activity with ID {id}: {ex.Message}");
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

    private static string? UpdateGarminSerializedData(
        string? serializedData,
        DateTime date,
        double distance,
        double climb,
        double duration,
        double averageSpeed)
    {
        if (string.IsNullOrWhiteSpace(serializedData))
        {
            return null;
        }

        var root = JsonNode.Parse(serializedData) as JsonObject;
        if (root is null)
        {
            return null;
        }

        var summary = root["summaryDTO"] as JsonObject;
        if (summary is null)
        {
            return null;
        }

        var dateOnly = DateOnly.FromDateTime(date);
        UpdateSummaryDate(summary, dateOnly);

        summary["distance"] = distance;
        summary["elevationGain"] = climb;
        summary["duration"] = duration;
        summary["averageSpeed"] = averageSpeed;
        summary["averageMovingSpeed"] = averageSpeed;

        return root.ToJsonString();
    }

    private static void UpdateSummaryDate(JsonObject summary, DateOnly date)
    {
        var fallbackLocal = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var localDateTime = TryParseDateTimeOffset(summary["startTimeLocal"]?.GetValue<string>()) ?? fallbackLocal;
        var updatedLocal = new DateTimeOffset(date.ToDateTime(TimeOnly.FromTimeSpan(localDateTime.TimeOfDay)), localDateTime.Offset);

        summary["startTimeLocal"] = updatedLocal.ToString("O", CultureInfo.InvariantCulture);
        summary["startTimeGMT"] = updatedLocal.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset? TryParseDateTimeOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;
    }

    private static bool TryParsePaceToSpeed(string? pace, out double speedMetersPerSecond)
    {
        speedMetersPerSecond = 0;

        if (string.IsNullOrWhiteSpace(pace))
        {
            return false;
        }

        var normalized = pace.Trim();
        if (normalized.EndsWith("m/km", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^4].Trim();
        }

        var parts = normalized.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
            minutes < 0 ||
            seconds < 0 ||
            seconds > 59)
        {
            return false;
        }

        var totalSeconds = (minutes * 60) + seconds;
        if (totalSeconds <= 0)
        {
            return false;
        }

        speedMetersPerSecond = 1000d / totalSeconds;
        return true;
    }

    private sealed record ActivityUpdateRequest(
        DateTime Date,
        bool IsRace,
        double Distance,
        double Climb,
        double Duration,
        string Pace,
        string? Description,
        string? Notes);
}
