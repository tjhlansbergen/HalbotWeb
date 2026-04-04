using Microsoft.AspNetCore.Mvc;

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
        var all = await cache.Get();
        
        switch (bucket)
        {
            case BucketType.Daily:

                var daily = Enumerable.Range(0, count)
                    .Select(offset => DateTime.UtcNow.Date.AddDays(-offset))
                    .Select(date => new { Date = date, Activities = all.Where(activity => activity.Date.Date == date) })
                    .Select(day => new { 
                        Date = day.Date, 
                        Distance = day.Activities.Sum(activity => activity.Distance),
                        Climb = day.Activities.Sum(activity => activity.Climb),
                        Speed = day.Activities.Any() ? day.Activities.Average(activity => activity.Speed) : 0
                    });
                        
                return Results.Ok(daily);

            case BucketType.Weekly:
                var weekly = Enumerable.Range(0, count)
                    .Select(offset => HalbotActivity.WeekOfYear(DateTime.UtcNow.Date.AddDays(-offset)))
                    .Select(week => new { Week = week, Activities = all.Where(activity => activity.Week() == week) })
                    .Select(week => new { 
                        Week = week.Week, 
                        Distance = week.Activities.Sum(activity => activity.Distance),
                        Climb = week.Activities.Sum(activity => activity.Climb),
                        Speed = week.Activities.Any() ? week.Activities.Average(activity => activity.Speed) : 0
                    });

                return Results.Ok(weekly);

            case BucketType.Monthly:
                var monthly = Enumerable.Range(0, count)
                    .Select(offset => DateTime.UtcNow.Date.AddMonths(-offset))
                    .Select(date => new { Month = new DateTime(date.Year, date.Month, 1), Activities = all.Where(activity => activity.Date.Year == date.Year && activity.Date.Month == date.Month) })
                    .Select(month => new { 
                        Month = month.Month, 
                        Distance = month.Activities.Sum(activity => activity.Distance),
                        Climb = month.Activities.Sum(activity => activity.Climb),
                        Speed = month.Activities.Any() ? month.Activities.Average(activity => activity.Speed) : 0
                    });

                return Results.Ok(monthly);

            case BucketType.Yearly:
                var yearly = Enumerable.Range(0, count)
                    .Select(offset => DateTime.UtcNow.Date.AddYears(-offset))
                    .Select(date => new { Year = new DateTime(date.Year, 1, 1), Activities = all.Where(activity => activity.Date.Year == date.Year) })
                    .Select(year => new { 
                        Year = year.Year, 
                        Distance = year.Activities.Sum(activity => activity.Distance),
                        Climb = year.Activities.Sum(activity => activity.Climb),
                        Speed = year.Activities.Any() ? year.Activities.Average(activity => activity.Speed) : 0
                    });     

                return Results.Ok(yearly);

            default:
                return Results.BadRequest("Invalid bucket type.");
        }
    }

    
}
