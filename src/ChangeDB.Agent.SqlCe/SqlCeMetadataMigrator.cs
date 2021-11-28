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


        public override Task<DatabaseDescriptor> GetDatabaseDescriptor(DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            var databaseDescriptor = SqlCeUtils.GetDataBaseDescriptorByEFCore(dbConnection);
            return Task.FromResult(databaseDescriptor);
        }

        public override Task PreMigrate(DatabaseDescriptor databaseDescriptor, DbConnection dbConnection, MigrationSetting migrationSetting)
        {
            // clear schemas
            databaseDescriptor.Tables.Each(p => p.Schema = null);
            databaseDescriptor.Tables.SelectMany(p => p.ForeignKeys).Each(p => p.PrincipalSchema = null);
            databaseDescriptor.Sequences.Each(p => p.Schema = null);
            return base.PreMigrate(databaseDescriptor, dbConnection, migrationSetting);
        }

        protected override string IdentityName(string schema, string objectName)
        {
            return base.IdentityName(objectName);
        }

    }
}
