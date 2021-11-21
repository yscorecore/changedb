using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ChangeDB.Agent.SqlCe
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
        private readonly DbConnection _dbConnection;
        private readonly string _connectionString;
        public DatabaseEnvironment()
        {
            _connectionString = $"Data Source={TestUtils.RandomDatabaseName()}.sdf;Persist Security Info=False;";
            _dbConnection = new SqlCeConnection(_connectionString);
            _dbConnection.CreateDatabase();
        }

        public DbConnection DbConnection => _dbConnection;

        public string ConnectionString => _connectionString;

        public void Dispose()
        {

        }




    }
}
