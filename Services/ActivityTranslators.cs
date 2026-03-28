using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class ActivityTranslators
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static List<HalbotActivity> Parse(IEnumerable<ActivityRecord> records)
    {
        var result = new List<HalbotActivity>();

        result.AddRange(ParseClassicJson(records.Where(r => (ActivityDataType)r.DataType == ActivityDataType.Classic)));
        result.AddRange(ParseTomTomJson(records.Where(r => (ActivityDataType)r.DataType == ActivityDataType.TomTom)));
        result.AddRange(ParseGarminJson(records.Where(r => (ActivityDataType)r.DataType == ActivityDataType.Garmin)));

        return result;
    }

    private static List<HalbotActivity> ParseGarminJson(IEnumerable<ActivityRecord> records)
    {
        var result = new List<HalbotActivity>();

        foreach (var record in records)
        {
            // invalid data type, return empty object
            if ((ActivityDataType)record.DataType != ActivityDataType.Garmin || string.IsNullOrEmpty(record.SerializedData))
            {
                result.Add(new HalbotActivity() { Id = record.Id });
                continue;
            }

            var garminActivity = JsonSerializer.Deserialize<GarminJson>(record.SerializedData, DeserializeOptions);

            if (garminActivity == null)
            {
                result.Add(new HalbotActivity() { Id = record.Id });
                continue;
            }

            var halbotActivity = new HalbotActivity
            {
                Id = record.Id,
                Description = record.Description,
                IsRace = Convert.ToBoolean(record.IsRace),
                DataType = (ActivityDataType)record.DataType,
                Journal = record.Gpx,    // todo yes, this is a hack
            };

            if (garminActivity.SummaryDto != null)
            {
                halbotActivity.Climb = garminActivity.SummaryDto.ElevationGain;
                halbotActivity.Descent = garminActivity.SummaryDto.ElevationLoss;
                halbotActivity.MaxElevation = garminActivity.SummaryDto.MaxElevation;
                halbotActivity.MinElevation = garminActivity.SummaryDto.MinElevation;
                halbotActivity.Date = garminActivity.SummaryDto.StartTimeLocal.DateTime;
                halbotActivity.Distance = garminActivity.SummaryDto.Distance;
                halbotActivity.Duration = garminActivity.SummaryDto.Duration;
                halbotActivity.Heartrate = garminActivity.SummaryDto.AverageHr;
                halbotActivity.Lat = (garminActivity.SummaryDto.StartLatitude + garminActivity.SummaryDto.EndLatitude) / 2;
                halbotActivity.Lng = (garminActivity.SummaryDto.StartLongitude + garminActivity.SummaryDto.EndLongitude) / 2;
                halbotActivity.Speed = garminActivity.SummaryDto.AverageSpeed;
                halbotActivity.Cadence = garminActivity.SummaryDto.AverageRunCadence;
                halbotActivity.TrainingEffect = garminActivity.SummaryDto.TrainingEffect;
                halbotActivity.AnaerobicTrainingEffect = garminActivity.SummaryDto.AnaerobicTrainingEffect;
                halbotActivity.Url = new Uri($"https://connect.garmin.com/modern/activity/{garminActivity.ActivityId}");
            }

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
            if ((ActivityDataType)record.DataType != ActivityDataType.TomTom || string.IsNullOrEmpty(record.SerializedData))
            {
                result.Add(new HalbotActivity() { Id = record.Id });
                continue;
            }

            var tomTomActivity = JsonSerializer.Deserialize<TomTomJson>(record.SerializedData, DeserializeOptions);

            if (tomTomActivity == null)
            {
                result.Add(new HalbotActivity() { Id = record.Id });
                continue;
            }

            var halbotActivity = new HalbotActivity
            {
                Id = record.Id,
                Description = record.Description,
                IsRace = Convert.ToBoolean(record.IsRace),
                DataType = (ActivityDataType)record.DataType,
                Journal = record.Gpx,    // yes, this is a hack
            };

            if (tomTomActivity.Aggregates != null)
            {
                
                halbotActivity.Climb = tomTomActivity.Aggregates.ClimbTotal;
                halbotActivity.Descent = tomTomActivity.Aggregates.DescentTotal;
                halbotActivity.Date = tomTomActivity.StartDatetimeUser.DateTime;
                halbotActivity.Distance = tomTomActivity.Aggregates.DistanceTotal;
                halbotActivity.Duration = tomTomActivity.Aggregates.ActiveTimeTotal;
                halbotActivity.Heartrate = tomTomActivity.Aggregates.HeartrateAvg;
                halbotActivity.Speed = tomTomActivity.Aggregates.SpeedAvg;
            }

            if (tomTomActivity.BoundingBox?.NorthEast != null && tomTomActivity.BoundingBox.SouthWest != null)
            {
                halbotActivity.Lat = (tomTomActivity.BoundingBox.NorthEast.Lat + tomTomActivity.BoundingBox.SouthWest.Lat) / 2;
                halbotActivity.Lng = (tomTomActivity.BoundingBox.NorthEast.Lng + tomTomActivity.BoundingBox.SouthWest.Lng) / 2;
            }

            halbotActivity.Url = tomTomActivity.Links?.Self;

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
            if ((ActivityDataType)record.DataType != ActivityDataType.Classic || string.IsNullOrEmpty(record.SerializedData))
            {
                result.Add(new HalbotActivity() { Id = record.Id });
                continue;
            }

            // valid data type, start parsing
            var classicActivity = JsonSerializer.Deserialize<ClassicJson>(record.SerializedData, DeserializeOptions);

            if (classicActivity == null)
            {
                result.Add(new HalbotActivity() { Id = record.Id });
                continue;
            }

            var halbotActivity = new HalbotActivity
            {
                Id = record.Id,
                Description = record.Description,
                IsRace = Convert.ToBoolean(record.IsRace),
                DataType = (ActivityDataType)record.DataType,
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