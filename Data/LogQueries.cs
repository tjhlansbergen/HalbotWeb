using Dapper;

public class LogQueries
{
    private readonly IDbConnectionFactory _factory;

    public LogQueries(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<LogRecord>> ReadAllAsync()
    {
        using var conn = _factory.CreateConnection();

        const string sql = """
            SELECT *
            FROM LogRecords
        """;

        return await conn.QueryAsync<LogRecord>(sql);
    }

    public async Task WriteAsync(LogRecord logRecord)
    {
        using var conn = _factory.CreateConnection();

        const string sql = """
            INSERT INTO LogRecords (DateTime, Severity, Message)
            VALUES (@DateTime, @Severity, @Message)
        """;

        await conn.ExecuteAsync(sql, new
        {
            logRecord.DateTime,
            logRecord.Severity,
            logRecord.Message,
        });
    }

    public async Task Rotate(int maxRecords)
    {
        using var conn = _factory.CreateConnection();

        const string sql = """
            DELETE FROM LogRecords
            WHERE rowid IN (
                SELECT rowid
                FROM LogRecords
                ORDER BY DateTime DESC, rowid DESC
                LIMIT -1 OFFSET @MaxRecords
            )
        """;

        await conn.ExecuteAsync(sql, new { MaxRecords = maxRecords });
    }
}