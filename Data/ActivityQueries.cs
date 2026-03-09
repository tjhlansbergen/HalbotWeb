using Dapper;

public class ActivityQueries
{
    private readonly IDbConnectionFactory _factory;

    public ActivityQueries(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<ActivityRecord>> GetAllAsync()
    {
        using var conn = _factory.CreateConnection();

        const string sql = """
            SELECT Id, DataType, SerializedData, Description, IsRace, Gpx
            FROM ActivityRecords
            ORDER BY Id
        """;

        return await conn.QueryAsync<ActivityRecord>(sql);
    }

    public async Task<long> CountAllAsync()
    {
        using var conn = _factory.CreateConnection();

        const string sql = """
            SELECT COUNT(*)
            FROM ActivityRecords
        """;

        return await conn.ExecuteScalarAsync<long>(sql);
    }
}

public record ActivityRecord(
    long Id,
    ActivityDataType DataType,
    string SerializedData,
    string? Description,
    bool IsRace,
    string? Gpx
);