using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
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
            var databaseFile = Path.Combine(Environment.CurrentDirectory, $"{Utility.RandomDatabaseName()}.fdb");
            // server type =0 default, server type =1  Embedded
            return new FbConnection($@"data source=localhost;user id=SYSDBA;password=masterkey;initial catalog={databaseFile};server type=0");
        }
        public void Dispose()
        {

        }



    }
}
