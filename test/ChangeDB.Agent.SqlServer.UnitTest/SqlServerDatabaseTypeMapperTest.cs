using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDatabaseTypeMapperTest
    {
        private readonly IMetadataMigrator _metadataMigrator = SqlServerMetadataMigrator.Default;
        private readonly MigrationSetting _migrationSetting = new MigrationSetting { DropTargetDatabaseIfExists = true };
        private readonly DbConnection _dbConnection;
        private readonly string _connectionString;

        public SqlServerDatabaseTypeMapperTest()
        {
            _connectionString = $"Server=127.0.0.1,1433;Database={TestUtils.RandomDatabaseName()};User Id=sa;Password=myStrong(!)Password;";
            _dbConnection = new SqlConnection(_connectionString);
            _dbConnection.CreateDatabase();
        }

        [Fact]
        public void SpikeGetAllTypes()
        {
            _dbConnection.Open();
            var table = _dbConnection.GetSchema();
            var schemas = _dbConnection.GetSchema("DataTypes");

        }
    }
}
