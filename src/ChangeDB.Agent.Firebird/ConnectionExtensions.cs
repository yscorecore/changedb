using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;

namespace ChangeDB.Agent.Firebird
{
    public static class ConnectionExtensions
    {
        public static void DropDatabaseIfExists(this DbConnection connection)
        {
            //using (var newConnection = CreateNoDatabaseConnection(connection))
            //{
            //    var connectionInfo = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
            //    newConnection.ExecuteNonQuery(
            //         $"DROP DATABASE IF EXISTS {PostgresUtils.IdentityName(connectionInfo.Database)};"
            //         );
            //}
        }
        public static void CreateDatabase(this DbConnection connection)
        {
            FbConnection.CreateDatabase(connection.ConnectionString);
        }
    }
}
