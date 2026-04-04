using System.Globalization;

public class HalbotActivity : IComparable<HalbotActivity>
{
    public long Id { get; set; } // unique id
    public double Distance { get; set; } // in meters
    public Uri? Url { get; set; } = null; // link to tomtom/garmin page for activity
    public DateTime Date { get; set; } // date of activity
    public double Heartrate { get; set; } // average heartrate
    public double Cadence { get; set; } // average cadence
    public double TrainingEffect { get; set; }
    public double AnaerobicTrainingEffect { get; set; }
    public double Speed { get; set; } // average speed of activity in m/s
    public double Climb { get; set; } // climb in meters
    public double Descent { get; set; } // descent in meters
    public double MaxElevation { get; set; }
    public double MinElevation { get; set; }
    public double Lat { get; set; } // location as lat-lng
    public double Lng { get; set; } // location as lat-lng
    public double Duration { get; set; } // active time total in seconds
    public string? Description { get; set; } // description of the activity
    public bool IsRace { get; set; }
    public ActivityDataType DataType { get; set; }
    public string? Journal { get; set; }

    //Conversion properties
    public string Pace => PaceForSpeed(Speed);
    public int Effort => (int) Math.Round(((Distance + (Climb * 8)) * Speed) / 1000);

    //comparer for sorting (by date)
    public int CompareTo(HalbotActivity? other)
    {
        return (other == null) ? 1 : other.Date.CompareTo(this.Date);
    }
    
    public int Week()=> WeekOfYear(Date);

    public static int WeekOfYear(DateTime date)
    {
        var day = (int)CultureInfo.CurrentCulture.Calendar.GetDayOfWeek(date);
        return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.AddDays(4 - (day == 0 ? 7 : day)), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    public static string PaceForSpeed(double speed)
    {
        if (speed <= 0)
            return "-:--"; // or throw exception?

        double secondsPerKm = 1000 / speed; // because speed is m/s
        int minutes = (int)(secondsPerKm / 60);
        int seconds = (int)Math.Round(secondsPerKm % 60);

        // Handle rounding like 5:60 → 6:00
        if (seconds == 60)
        {
            minutes++;
            seconds = 0;
        }

        return $"{minutes}:{seconds:00}";
    }
}
