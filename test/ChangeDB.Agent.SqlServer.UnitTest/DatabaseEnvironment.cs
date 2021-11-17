using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
        private readonly DbConnection _dbConnection;
        private readonly string _connectionString;
        public DatabaseEnvironment()
        {
            _connectionString = $"Server=127.0.0.1,1433;Database={TestUtils.RandomDatabaseName()};User Id=sa;Password=myStrong(!)Password;";
            _dbConnection = new SqlConnection(_connectionString);
            _dbConnection.CreateDatabase();
        }

        public DbConnection DbConnection => _dbConnection;

        public string ConnectionString => _connectionString;

        public void Dispose()
        {

        }




    }
}
