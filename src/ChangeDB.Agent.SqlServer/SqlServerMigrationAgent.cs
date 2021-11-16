using System.Data.Common;
using ChangeDB.Migration;
using Microsoft.Data.SqlClient;
using YS.Knife;

namespace ChangeDB.Agent.SqlServer
{
    [DictionaryKey("sqlserver")]
    [Service]
    public class SqlServerMigrationAgent : IMigrationAgent
    {
        public IDataMigrator DataMigrator { get => SqlServerDataMigrator.Default; }
        public IMetadataMigrator MetadataMigrator { get => SqlServerMetadataMigrator.Default; }
        public IDatabaseTypeMapper DatabaseTypeMapper { get => SqlServerDatabaseTypeMapper.Default; }
        public ISqlExpressionTranslator ExpressionTranslator { get => SqlServerSqlExpressionTranslator.Default; }
        public IDatabaseManager DatabaseManger { get => SqlServerDatabaseManager.Default; }

        public DbConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}
