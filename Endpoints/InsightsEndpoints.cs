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

        // GET /api/insights/lastxworkouts
        group.MapGet("/lastxworkouts", async (
            [FromServices] WorkoutCache cache,
            BucketType bucket,
            int count
            ) => await GetLastXWorkouts(cache, bucket, count));

    }

    private static async Task<IResult> GetLastXRuns(ActivityCache cache, BucketType bucket, int count)
    {
        if (count <= 0)
        {
            return Results.BadRequest("Count must be greater than 0.");
        }

        var all = await cache.Get();
        var today = DateTime.UtcNow.Date;
        IEnumerable<(DateTime Start, DateTime EndExclusive, string Key)> buckets;

        try
        {
            buckets = GetBuckets(bucket, count, today);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Results.BadRequest("Invalid bucket type.");
        }

        var results = buckets.Select(bucketWindow =>
        {
            var bucketActivities = all.Where(activity =>
                activity.Date.Date >= bucketWindow.Start &&
                activity.Date.Date < bucketWindow.EndExclusive);

            return new
            {
                BucketStartUtc = bucketWindow.Start,
                BucketEndUtcExclusive = bucketWindow.EndExclusive,
                BucketKey = bucketWindow.Key,
                Distance = bucketActivities.Sum(activity => activity.Distance),
                Climb = bucketActivities.Sum(activity => activity.Climb),
                Speed = bucketActivities.Any() ? bucketActivities.Average(activity => activity.Speed) : 0
            };
        });

        return Results.Ok(results);
    }

    private static async Task<IResult> GetLastXWorkouts(WorkoutCache cache, BucketType bucket, int count)
    {
        if (count <= 0)
        {
            return Results.BadRequest("Count must be greater than 0.");
        }

        var all = await cache.Get();
        var today = DateTime.UtcNow.Date;
        IEnumerable<(DateTime Start, DateTime EndExclusive, string Key)> buckets;

        try
        {
            buckets = GetBuckets(bucket, count, today);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Results.BadRequest("Invalid bucket type.");
        }

        var results = buckets.Select(bucketWindow =>
        {
            var bucketWorkouts = all.Where(workout =>
                workout.Date.Date >= bucketWindow.Start &&
                workout.Date.Date < bucketWindow.EndExclusive);

            return new
            {
                BucketStartUtc = bucketWindow.Start,
                BucketEndUtcExclusive = bucketWindow.EndExclusive,
                BucketKey = bucketWindow.Key,
                Minutes = bucketWorkouts.Sum(workout => workout.Minutes)
            };
        });

        return Results.Ok(results);
    }

    private static IEnumerable<(DateTime Start, DateTime EndExclusive, string Key)> GetBuckets(BucketType bucket, int count, DateTime today)
    {
        return bucket switch
        {
            BucketType.Daily => Enumerable.Range(0, count)
                .Select(offset => today.AddDays(-offset))
                .Select(dayStart => (
                    Start: dayStart,
                    EndExclusive: dayStart.AddDays(1),
                    Key: dayStart.ToString("yyyy-MM-dd"))),

            BucketType.Weekly => Enumerable.Range(0, count)
                .Select(offset => today.AddDays(-(((int)today.DayOfWeek + 6) % 7)).AddDays(-(offset * 7)))
                .Select(weekStart => (
                    Start: weekStart,
                    EndExclusive: weekStart.AddDays(7),
                    Key: $"{ISOWeek.GetYear(weekStart)}-W{ISOWeek.GetWeekOfYear(weekStart):00}")),

            BucketType.Monthly => Enumerable.Range(0, count)
                .Select(offset => new DateTime(today.Year, today.Month, 1).AddMonths(-offset))
                .Select(monthStart => (
                    Start: monthStart,
                    EndExclusive: monthStart.AddMonths(1),
                    Key: monthStart.ToString("yyyy-MM"))),

            BucketType.Yearly => Enumerable.Range(0, count)
                .Select(offset => new DateTime(today.Year, 1, 1).AddYears(-offset))
                .Select(yearStart => (
                    Start: yearStart,
                    EndExclusive: yearStart.AddYears(1),
                    Key: yearStart.ToString("yyyy"))),

            _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, "Invalid bucket type.")
        };
    }
}
