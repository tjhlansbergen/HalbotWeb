using Dapper;

public class ActivityQueries
{
    private readonly IDbConnectionFactory _factory;

    public ActivityQueries(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task InsertAsync(ActivityRecord record)
    {
        using var conn = _factory.CreateConnection();

        const string sql = """
            INSERT INTO ActivityRecords (Id, DataType, SerializedData, Description, IsRace, Gpx)
            VALUES (@Id, @DataType, @SerializedData, @Description, @IsRace, @Gpx)
        """;

        await conn.ExecuteAsync(sql, record);
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