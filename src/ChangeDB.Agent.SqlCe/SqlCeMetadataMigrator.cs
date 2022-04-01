using System.Threading.Tasks;
using ChangeDB.Agent.SqlServer;
using ChangeDB.Migration;


namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeMetadataMigrator : SqlServer.SqlServerMetadataMigrator, IMetadataMigrator
    {
        public static new readonly IMetadataMigrator Default = new SqlCeMetadataMigrator();
        public override Task<DatabaseDescriptor> GetSourceDatabaseDescriptor(MigrationContext migrationContext)
        {
            var databaseDescriptor = SqlCeUtils.GetDataBaseDescriptorByEFCore(migrationContext.SourceConnection);
            return Task.FromResult(databaseDescriptor);
        }
    }
}
