using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using static ChangeDB.Agent.Firebird.FirebirdUtils;

namespace ChangeDB.Agent.Firebird
{
    public static class ConnectionExtensions
    {
        public static void DropDatabaseIfExists(this DbConnection connection)
        {
            var connectionInfo = new FbConnectionStringBuilder(connection.ConnectionString);
            if (File.Exists(connectionInfo.Database))
            {
                FbConnection.DropDatabase(connection.ConnectionString);
            }
        }
        public static void CreateDatabase(this DbConnection connection)
        {
            FbConnection.CreateDatabase(connection.ConnectionString);
        }
        public static void ClearDatabase(this DbConnection connection)
        {
            connection.DropAllForeignConstraints();
            connection.DropAllTables();
        }

        private static void DropAllForeignConstraints(this DbConnection connection)
        {
            //var allForeignConstraints = connection.ExecuteReaderAsList<string, string, string>($"SELECT TABLE_NAME ,CONSTRAINT_NAME,TABLE_SCHEMA from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc where tc.CONSTRAINT_TYPE ='FOREIGN KEY';");
            //allForeignConstraints.ForEach(p => connection.ExecuteNonQuery($"ALTER TABLE {IdentityName(p.Item3, p.Item1)} drop constraint {IdentityName(p.Item2)};"));
        }



        public static void DropAllTables(this DbConnection connection)
        {
            var sql = @"SELECT
        trim(r.RDB$RELATION_NAME)
    FROM RDB$RELATIONS r
    WHERE r.RDB$SYSTEM_FLAG is distinct from 1";

            var allTables = connection.ExecuteReaderAsList<string>(sql);
            allTables.ForEach(p => connection.DropTable(p));

        }
        public static void DropTable(this DbConnection connection, string table)
        {
            connection.ExecuteNonQuery($"drop table {IdentityName(table)}");
        }

    }
}
