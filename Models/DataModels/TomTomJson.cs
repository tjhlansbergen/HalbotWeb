using System.Text.Json.Serialization;

public class TomTomJson
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("activity_type_id")]
    public long ActivityTypeId { get; set; }

    [JsonPropertyName("start_datetime")]
    public DateTimeOffset StartDatetime { get; set; }

    [JsonPropertyName("start_datetime_user")]
    public DateTimeOffset StartDatetimeUser { get; set; }

    [JsonPropertyName("user")]
    public User User { get; set; }

    [JsonPropertyName("activity_type_id_tt")]
    public long ActivityTypeIdTt { get; set; }

    [JsonPropertyName("display_offset_seconds")]
    public long DisplayOffsetSeconds { get; set; }

    [JsonPropertyName("links")]
    public Links Links { get; set; }

    [JsonPropertyName("formats")]
    public string[] Formats { get; set; }

    [JsonPropertyName("zones")]
    public long[] Zones { get; set; }

    [JsonPropertyName("bounding_box")]
    public BoundingBox BoundingBox { get; set; }

    [JsonPropertyName("aggregates")]
    public Aggregates Aggregates { get; set; }
}

public class Aggregates
{
    [JsonPropertyName("active_time_total")]
    public double ActiveTimeTotal { get; set; }

    [JsonPropertyName("distance_total")]
    public double DistanceTotal { get; set; }

    [JsonPropertyName("steps_total")]
    public long StepsTotal { get; set; }

    [JsonPropertyName("elapsed_time_total")]
    public long ElapsedTimeTotal { get; set; }

    [JsonPropertyName("metabolic_energy_total")]
    public long MetabolicEnergyTotal { get; set; }

    [JsonPropertyName("speed_avg")]
    public double SpeedAvg { get; set; }

    [JsonPropertyName("climb_total")]
    public double ClimbTotal { get; set; }

    [JsonPropertyName("descent_total")]
    public double DescentTotal { get; set; }

    [JsonPropertyName("heartrate_avg")]
    public double HeartrateAvg { get; set; }

    [JsonPropertyName("hrz_dist")]
    public long[] HrzDist { get; set; }

    [JsonPropertyName("hrz_none")]
    public long HrzNone { get; set; }

    [JsonPropertyName("moving_time_total")]
    public long MovingTimeTotal { get; set; }

    [JsonPropertyName("moving_speed_avg")]
    public double MovingSpeedAvg { get; set; }
}

public partial class BoundingBox
{
    [JsonPropertyName("north_east")]
    public NorthEast NorthEast { get; set; }

    [JsonPropertyName("south_west")]
    public NorthEast SouthWest { get; set; }
}

public partial class NorthEast
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}

public partial class Links
{
    [JsonPropertyName("image")]
    public Uri Image { get; set; }

    [JsonPropertyName("webview")]
    public string Webview { get; set; }

    [JsonPropertyName("convert_to_trail")]
    public Uri ConvertToTrail { get; set; }

    [JsonPropertyName("self")]
    public Uri Self { get; set; }
}

public partial class User
{
    [JsonPropertyName("devices")]
    public object[] Devices { get; set; }

    [JsonPropertyName("first_name")]
    public string FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string LastName { get; set; }

    [JsonPropertyName("locale")]
    public string Locale { get; set; }

    [JsonPropertyName("traits")]
    public object[] Traits { get; set; }

    [JsonPropertyName("user_prefs")]
    public UserPrefs UserPrefs { get; set; }
}

public class UserPrefs
{
    [JsonPropertyName("default")]
    public Default Default { get; set; }

    [JsonPropertyName("overrides")]
    public Overrides Overrides { get; set; }

    [JsonPropertyName("clock")]
    public string Clock { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }

    [JsonPropertyName("wrist")]
    public string Wrist { get; set; }

    [JsonPropertyName("auto_pauses_enabled")]
    public AutoPausesEnabled AutoPausesEnabled { get; set; }
}

public class AutoPausesEnabled
{
    [JsonPropertyName("RUN")]
    public bool Run { get; set; }

    [JsonPropertyName("RUN_TRAIL")]
    public bool RunTrail { get; set; }

    [JsonPropertyName("CYCLE")]
    public bool Cycle { get; set; }

    [JsonPropertyName("WALK_HIKE")]
    public bool WalkHike { get; set; }

    [JsonPropertyName("SKI")]
    public bool Ski { get; set; }

    [JsonPropertyName("SNOWBOARD")]
    public bool Snowboard { get; set; }

    [JsonPropertyName("FREESTYLE")]
    public bool Freestyle { get; set; }
}

public class Default
{
    [JsonPropertyName("distance")]
    public string Distance { get; set; }

    [JsonPropertyName("energy")]
    public string Energy { get; set; }
}

public partial class Overrides
{
}
