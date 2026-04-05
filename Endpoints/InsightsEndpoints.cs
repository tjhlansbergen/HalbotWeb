using Microsoft.AspNetCore.Mvc;
using System.Globalization;

public static class InsightsEndpoints
{
    public static void MapInsightsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/insights").RequireAuthorization();

        // GET /api/insights/lastxruns
        group.MapGet("/lastxruns", async (
            [FromServices] ActivityCache cache,
            BucketType bucket,
            int count
            ) => await GetLastXRuns(cache, bucket, count));

    }

    private static async Task<IResult> GetLastXRuns(ActivityCache cache, BucketType bucket, int count)
    {
        if (count <= 0)
        {
            return Results.BadRequest("Count must be greater than 0.");
        }

        var all = await cache.Get();
        var today = DateTime.UtcNow.Date;

        object BuildBucket(DateTime bucketStartUtc, DateTime bucketEndUtcExclusive, string bucketKey)
        {
            var bucketActivities = all.Where(activity =>
                activity.Date.Date >= bucketStartUtc &&
                activity.Date.Date < bucketEndUtcExclusive);

            return new
            {
                BucketStartUtc = bucketStartUtc,
                BucketEndUtcExclusive = bucketEndUtcExclusive,
                BucketKey = bucketKey,
                Distance = bucketActivities.Sum(activity => activity.Distance),
                Climb = bucketActivities.Sum(activity => activity.Climb),
                Speed = bucketActivities.Any() ? bucketActivities.Average(activity => activity.Speed) : 0
            };
        }
        
        switch (bucket)
        {
            case BucketType.Daily:
                var daily = Enumerable.Range(0, count)
                    .Select(offset => today.AddDays(-offset))
                    .Select(dayStart => BuildBucket(
                        dayStart,
                        dayStart.AddDays(1),
                        dayStart.ToString("yyyy-MM-dd")));
                        
                return Results.Ok(daily);

            case BucketType.Weekly:
                var currentWeekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                var weekly = Enumerable.Range(0, count)
                    .Select(offset => currentWeekStart.AddDays(-(offset * 7)))
                    .Select(weekStart => BuildBucket(
                        weekStart,
                        weekStart.AddDays(7),
                        $"{ISOWeek.GetYear(weekStart)}-W{ISOWeek.GetWeekOfYear(weekStart):00}"));

                return Results.Ok(weekly);

            case BucketType.Monthly:
                var currentMonthStart = new DateTime(today.Year, today.Month, 1);
                var monthly = Enumerable.Range(0, count)
                    .Select(offset => currentMonthStart.AddMonths(-offset))
                    .Select(monthStart => BuildBucket(
                        monthStart,
                        monthStart.AddMonths(1),
                        monthStart.ToString("yyyy-MM")));

                return Results.Ok(monthly);

            case BucketType.Yearly:
                var currentYearStart = new DateTime(today.Year, 1, 1);
                var yearly = Enumerable.Range(0, count)
                    .Select(offset => currentYearStart.AddYears(-offset))
                    .Select(yearStart => BuildBucket(
                        yearStart,
                        yearStart.AddYears(1),
                        yearStart.ToString("yyyy")));

                return Results.Ok(yearly);

            default:
                return Results.BadRequest("Invalid bucket type.");
        }
    }

    
}
