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
}

public record ActivityRecord(
    long Id,
    long DataType,
    string? SerializedData,
    string? Description,
    long IsRace,
    string? Gpx
);