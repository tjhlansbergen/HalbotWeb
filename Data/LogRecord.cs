public record LogRecord(
    DateTime DateTime,
    LogSeverityLevel Severity, 
    string Message
);

public enum LogSeverityLevel { Info, Warning, Error }