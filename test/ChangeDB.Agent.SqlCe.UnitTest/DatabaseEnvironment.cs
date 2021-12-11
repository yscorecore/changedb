using System;
using System.Data.Common;
using System.Data.SqlServerCe;
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
            _connectionString = $"Data Source={Utility.RandomDatabaseName()}.sdf;Persist Security Info=False;";
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
