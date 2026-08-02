using System.Data;
using Npgsql;

namespace HomeDiary_api.Data;

public class DbConnectionFactory(string connectionString)
{
    public IDbConnection Create() => new NpgsqlConnection(connectionString);
}
