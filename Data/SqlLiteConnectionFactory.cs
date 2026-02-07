using System.Data;
using Microsoft.Data.Sqlite;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Default")!;
    }

    public IDbConnection CreateConnection()
        => new SqliteConnection(_connectionString);
}
