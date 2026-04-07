public static class DbLoggerExtensions
{
    public static ILoggingBuilder AddDbLogger(
        this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, DbLoggerProvider>();
        return builder;
    }
}

public class DbLoggerProvider : ILoggerProvider
{
    private readonly IServiceProvider _services;

    public DbLoggerProvider(IServiceProvider services)
    {
        _services = services;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new DbLogger(categoryName, _services);
    }

    public void Dispose()
    {
    }
}

public class DbLogger : ILogger
{
    private readonly string _category;
    private readonly IServiceProvider _services;

    public DbLogger(string category, IServiceProvider services)
    {
        _category = category;
        _services = services;
    }

    IDisposable ILogger.BeginScope<TState>(TState state) => null!;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel >= LogLevel.Information
        && _category != "Microsoft.Hosting.Lifetime";

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);

        using var scope = _services.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<LogQueries>();
        
        writer.WriteAsync(
            new LogRecord
            {
                DateTime = DateTime.UtcNow,
                Severity = logLevel switch
                {
                    LogLevel.Information => LogSeverityLevel.Info,
                    LogLevel.Warning => LogSeverityLevel.Warning,
                    LogLevel.Error => LogSeverityLevel.Error,
                    _ => LogSeverityLevel.Info
                },
                Message = message,
            }
        ).GetAwaiter().GetResult();
        
        writer.Rotate(200).GetAwaiter().GetResult();
    }
}