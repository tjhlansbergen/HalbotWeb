public record ActivityRecord
{
    public long Id { get; set; }
    public ActivityDataType DataType { get; set; }
    public string? SerializedData { get; set; }
    public string? Description { get; set; }
    public Boolean IsRace { get; set; }
    public string? Gpx { get; set; }

}