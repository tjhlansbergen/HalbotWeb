using System.Globalization;
using System.Text.Json;

public static class ActivityTranslators
{
    public static List<HalbotActivity> Parse(IEnumerable<ActivityRecord> records)
    {
        var result = new List<HalbotActivity>();

        result.AddRange(ParseClassicJson(records.Where(r => r.DataType == ActivityDataType.Classic)));
        result.AddRange(ParseTomTomJson(records.Where(r => r.DataType == ActivityDataType.TomTom)));
        result.AddRange(ParseGarminJson(records.Where(r => r.DataType == ActivityDataType.Garmin)));

        return result;
    }

    private static List<HalbotActivity> ParseGarminJson(IEnumerable<ActivityRecord> records)
    {
        var result = new List<HalbotActivity>();

        foreach (var record in records)
        {
            // invalid data type, return empty object
            if (record.DataType != ActivityDataType.Garmin)
            {
                result.Add(new HalbotActivity() { Id = record.Id });
                continue;
            }

            var garminActivity = JsonSerializer.Deserialize<GarminJson>(record.SerializedData);

            if (garminActivity == null)
            {
                result.Add(new HalbotActivity() { Id = record.Id });
                continue;
            }

            var halbotActivity = new HalbotActivity
            {
                Id = record.Id,
                Description = record.Description,
                IsRace = record.IsRace,
                DataType = record.DataType,
                Journal = record.Gpx,    // todo yes, this is a hack

                Climb = garminActivity.SummaryDto.ElevationGain,
                Descent = garminActivity.SummaryDto.ElevationLoss,
                MaxElevation = garminActivity.SummaryDto.MaxElevation,
                MinElevation = garminActivity.SummaryDto.MinElevation,
                Date = garminActivity.SummaryDto.StartTimeLocal.DateTime,
                Distance = garminActivity.SummaryDto.Distance,
                Duration = garminActivity.SummaryDto.Duration,
                Heartrate = garminActivity.SummaryDto.AverageHr,
                Lat = (garminActivity.SummaryDto.StartLatitude + garminActivity.SummaryDto.EndLatitude) / 2,
                Lng = (garminActivity.SummaryDto.StartLongitude + garminActivity.SummaryDto.EndLongitude) / 2,
                Speed = garminActivity.SummaryDto.AverageSpeed,
                Cadence = garminActivity.SummaryDto.AverageRunCadence,
                TrainingEffect = garminActivity.SummaryDto.TrainingEffect,
                AnaerobicTrainingEffect = garminActivity.SummaryDto.AnaerobicTrainingEffect,
                Url = new Uri($"https://connect.garmin.com/modern/activity/{garminActivity.ActivityId}")
            };

            result.Add(halbotActivity);
        }

        return result;
    }

    private static List<HalbotActivity> ParseTomTomJson(IEnumerable<ActivityRecord> records)
    {
        var result = new List<HalbotActivity>();

        foreach (var record in records)
        {
            // invalid data type, return empty object
            if (record.DataType != ActivityDataType.TomTom)
            {
                result.Add(new HalbotActivity() { Id = record.Id });
                continue;
            }

            var tomTomActivity = JsonSerializer.Deserialize<TomTomJson>(record.SerializedData);

            if (tomTomActivity == null)
            {
                result.Add(new HalbotActivity() { Id = record.Id });
                continue;
            }

            var halbotActivity = new HalbotActivity
            {
                Id = record.Id,
                Description = record.Description,
                IsRace = record.IsRace,
                DataType = record.DataType,
                Journal = record.Gpx,    // yes, this is a hack

                Climb = tomTomActivity.Aggregates.ClimbTotal,
                Descent = tomTomActivity.Aggregates.DescentTotal,
                Date = tomTomActivity.StartDatetimeUser.DateTime,
                Distance = tomTomActivity.Aggregates.DistanceTotal,
                Duration = tomTomActivity.Aggregates.ActiveTimeTotal,
                Heartrate = tomTomActivity.Aggregates.HeartrateAvg,
                Lat = (tomTomActivity.BoundingBox.NorthEast.Lat + tomTomActivity.BoundingBox.SouthWest.Lat) / 2,
                Lng = (tomTomActivity.BoundingBox.NorthEast.Lng + tomTomActivity.BoundingBox.SouthWest.Lng) / 2,
                Speed = tomTomActivity.Aggregates.SpeedAvg,
                Url = tomTomActivity.Links.Self
            };

            result.Add(halbotActivity);
        }

        return result;
    }

    private static List<HalbotActivity> ParseClassicJson(IEnumerable<ActivityRecord> records)
    {
        var result = new List<HalbotActivity>();

        foreach (var record in records)
        {
            // invalid data type, return empty object
            if (record.DataType != ActivityDataType.Classic)
            {
                result.Add(new HalbotActivity() { Id = record.Id });
                continue;
            }

            // valid data type, start parsing
            var classicActivity = JsonSerializer.Deserialize<ClassicJson>(record.SerializedData);

            if (classicActivity == null)
            {
                result.Add(new HalbotActivity() { Id = record.Id });
                continue;
            }

            var halbotActivity = new HalbotActivity
            {
                Id = record.Id,
                Description = record.Description,
                IsRace = record.IsRace,
                DataType = record.DataType,
                Journal = record.Gpx,    // yes, this is a hack

                Date = classicActivity.StartDatetime.DateTime,
                Distance = double.TryParse(classicActivity.DistanceTotal, NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo, out double distanceValue) ? distanceValue : 0,
                Speed = double.TryParse(classicActivity.SpeedAvg, NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo, out double speedValue) ? speedValue : 0
            };

            result.Add(halbotActivity);
        }

        return result;
    }
}