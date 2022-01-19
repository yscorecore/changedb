using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using Xunit;

namespace ChangeDB.Agent.Firebird
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
       
        public DatabaseEnvironment()
        {
            DbConnection = NewDatabaseConnection();
            DbConnection.CreateDatabase();
        }

        public DbConnection DbConnection { get; }

        public DbConnection NewDatabaseConnection()
        {
            var connectionString = $"User=SYSDBA;Password=masterkey;Database={Utility.RandomDatabaseName()}.fdb;DataSource=localhost;Port=3050;Dialect=3;Charset=NONE;Role=;Connection lifetime=15;Pooling=true;MinPoolSize=0;MaxPoolSize=50;Packet Size=8192;ServerType = 1;";
            return new FbConnection(connectionString);
        }
        public void Dispose()
        {

        }



    }
}
