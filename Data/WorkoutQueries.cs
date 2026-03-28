using Dapper;

public class WorkoutQueries
{
    private readonly IDbConnectionFactory _factory;

    public WorkoutQueries(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task InsertAsync(WorkoutRecord record)
    {
        using var conn = _factory.CreateConnection();

        const string sql = """
            INSERT INTO WorkoutRecords (Date, Minutes, Notes)
            VALUES (@Date, @Minutes, @Notes)
        """;

        await conn.ExecuteAsync(sql, record);
    }

    public async Task<IEnumerable<WorkoutRecord>> GetAllAsync()
    {
        using var conn = _factory.CreateConnection();

        const string sql = """
            SELECT Id, Date, Minutes, Notes
            FROM WorkoutRecords
        """;

        return await conn.QueryAsync<WorkoutRecord>(sql);
    }

    public async Task<long> CountAllAsync()
    {
        using var conn = _factory.CreateConnection();

        const string sql = """
            SELECT COUNT(*)
            FROM WorkoutRecords
        """;

        return await conn.ExecuteScalarAsync<long>(sql);
    }
}
