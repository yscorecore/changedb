using System.Data.Common;
using ChangeDB.Migration;
using YS.Knife;

namespace ChangeDB.Agent.SqlServer
{
    [DictionaryKey("sqlserver")]
    [Service]
    public class SqlServerMigrationAgent : IMigrationAgent
    {
        public IDataMigrator DataMigrator { get => SqlServerDataMigrator.Default; }
        public IMetadataMigrator MetadataMigrator { get => SqlServerMetadataMigrator.Default; }

        public DbConnection CreateConnection(string connectionString)
        {
            throw new System.NotImplementedException();
        }
    }
}
