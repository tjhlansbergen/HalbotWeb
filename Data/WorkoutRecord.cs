public record WorkoutRecord
{
    public long Id { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int Minutes { get; set; }
    public string? Notes { get; set; }
}
