public record LogRecord
{
    public DateTime DateTime { get; set; }
    public LogSeverityLevel Severity { get; set; }
    public string? Message { get; set; }
}

public enum LogSeverityLevel
{
    Info,
    Warning,
    Error
}