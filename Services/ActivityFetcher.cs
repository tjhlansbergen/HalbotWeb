using System.Globalization;
using System.Text.Json;

public class ActivityFetcher
{
    public ActivityRecord Fetch(long garminId)
    {
        var content = new HttpClient().GetStringAsync($"https://connect.garmin.com/modern/activity/{garminId}").Result;
        var scraped = scrapeGarminActivity(content);

        var garminActivity = new GarminJson
        {
            ActivityId = garminId,
            ActivityName = scraped.Title,
            SummaryDto = new SummaryDto
            {
                ElevationGain = scraped.Climb,
                StartTimeLocal = scraped.Date,
                Distance = scraped.DistanceMeters,
                Duration = scraped.DurationSeconds,
                StartLatitude = scraped.Latitude,
                EndLatitude = scraped.Latitude,
                StartLongitude = scraped.Longitude,
                EndLongitude = scraped.Longitude,
                AverageSpeed = scraped.SpeedMetersPerSecond
            }
        };


        return new ActivityRecord{
            Id = garminId,
            DataType = ActivityDataType.Garmin,
            SerializedData = JsonSerializer.Serialize(garminActivity),
            Description = scraped.Title,
            IsRace = false,
            Gpx = string.Empty
        };
    }

    private static ScrapedGarminActivity scrapeGarminActivity(string content)
    {
        var result = new ScrapedGarminActivity();
        var splits = content.Split("meta");

        result.Title = splits.Single(s => s.Contains("og:title")).Split('"')[3];
        result.Latitude = double.Parse(splits.Single(s => s.Contains("og:latitude")).Split('"')[3], CultureInfo.InvariantCulture);
        result.Longitude = double.Parse(splits.Single(s => s.Contains("og:longitude\"")).Split('"')[3], CultureInfo.InvariantCulture);

        var ogDesciption = splits.Single(s => s.Contains("og:description")).Split('"')[3];

        result.DistanceMeters = double.Parse(ogDesciption.Split('|').Single(s => s.Contains("Distance")).Split(' ')[1], CultureInfo.InvariantCulture) * 1000;
        result.Climb = double.Parse(ogDesciption.Split('|').Single(s => s.Contains("Elevation")).Split(' ')[2], CultureInfo.InvariantCulture);

        var duration = ogDesciption.Split('|').Single(s => s.Contains("Time")).Split(' ')[2].Trim();
        if (duration.Split(':').Length == 2)
        {
            duration = "00:" + duration;
        }
        result.DurationSeconds = TimeSpan.Parse(duration, CultureInfo.InvariantCulture).TotalSeconds;

        result.SpeedMetersPerSecond = result.DistanceMeters / result.DurationSeconds;

        return result;
    }

    public class ScrapedGarminActivity
    {
        public string Title { get; set; } = string.Empty;
        public double DistanceMeters { get; set; }
        public double DurationSeconds { get; set; }
        public double Climb { get; set; }
        public double SpeedMetersPerSecond { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow.Date.AddHours(1);  // hacky
    }
}