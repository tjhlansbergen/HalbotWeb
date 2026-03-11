public record ActivityRecord(
    long Id,
    long DataType,
    string SerializedData,
    string? Description,
    long IsRace,
    string? Gpx
);