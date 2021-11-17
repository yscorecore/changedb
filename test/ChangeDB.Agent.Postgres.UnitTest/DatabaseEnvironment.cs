using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Xunit;

namespace ChangeDB.Agent.Postgres
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
        private readonly DbConnection _dbConnection;
        public DatabaseEnvironment()
        {
            _dbConnection = new NpgsqlConnection($"Server=127.0.0.1;Port=5432;Database={TestUtils.RandomDatabaseName()};User Id=postgres;Password=mypassword;");
            _dbConnection.CreateDatabase();
        }

        public DbConnection DbConnection => _dbConnection;

        public void Dispose()
        {
        }
    }
}
