using System.Text.Json.Serialization;

public class ClassicJson
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("activity_type_id")]
    public long ActivityTypeId { get; set; }

    [JsonPropertyName("start_datetime")]
    public DateTimeOffset StartDatetime { get; set; }

    [JsonPropertyName("start_datetime_user")]
    public DateTimeOffset StartDatetimeUser { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("active_time_total")]
    public string? ActiveTimeTotal { get; set; }

    [JsonPropertyName("distance_total")]
    public string? DistanceTotal { get; set; }

    [JsonPropertyName("steps_total")]
    public string? StepsTotal { get; set; }

    [JsonPropertyName("elapsed_time_total")]
    public string? ElapsedTimeTotal { get; set; }

    [JsonPropertyName("metabolic_energy_total")]
    public string? MetabolicEnergyTotal { get; set; }

    [JsonPropertyName("speed_avg")]
    public string? SpeedAvg { get; set; }

    [JsonPropertyName("climb_total")]
    public string? ClimbTotal { get; set; }

    [JsonPropertyName("descent_total")]
    public string? DescentTotal { get; set; }

    [JsonPropertyName("heartrate_avg")]
    public string? HeartrateAvg { get; set; }

    [JsonPropertyName("hrz_dist")]
    public string? HrzDist { get; set; }

    [JsonPropertyName("hrz_none")]
    public long HrzNone { get; set; }

    [JsonPropertyName("moving_time_total")]
    public string? MovingTimeTotal { get; set; }

    [JsonPropertyName("moving_speed_avg")]
    public string? MovingSpeedAvg { get; set; }
}
