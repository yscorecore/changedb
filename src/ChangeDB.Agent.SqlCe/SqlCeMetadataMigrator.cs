using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Agent.SqlCe;
using ChangeDB.Migration;


namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeMetadataMigrator : SqlServer.SqlServerMetadataMigrator, IMetadataMigrator
    {
        public static new readonly IMetadataMigrator Default = new SqlCeMetadataMigrator();


        public override Task<DatabaseDescriptor> GetDatabaseDescriptor(DbConnection dbConnection, MigrationContext migrationContext)
        {
            var databaseDescriptor = SqlCeUtils.GetDataBaseDescriptorByEFCore(dbConnection);
            return Task.FromResult(databaseDescriptor);
        }



    }
}
