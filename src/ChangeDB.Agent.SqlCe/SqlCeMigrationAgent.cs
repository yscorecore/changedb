using System.Data.Common;
using System.Data.SqlServerCe;
using ChangeDB.Migration;
using YS.Knife;

namespace ChangeDB.Agent.SqlCe
{
    [DictionaryKey("sqlce")]
    [Service]
    public class SqlCeMigrationAgent : IMigrationAgent
    {
        public IDataMigrator DataMigrator { get => SqlCeDataMigrator.Default; }
        public IMetadataMigrator MetadataMigrator { get => SqlCeMetadataMigrator.Default; }
        public IDatabaseTypeMapper DatabaseTypeMapper { get => SqlCeDatabaseTypeMapper.Default; }
        public ISqlExpressionTranslator ExpressionTranslator { get => SqlCeSqlExpressionTranslator.Default; }

        public IDataTypeMapper DataTypeMapper => throw new System.NotImplementedException();

        public IDatabaseManager DatabaseManger => throw new System.NotImplementedException();

        public DbConnection CreateConnection(string connectionString)
        {
            return new SqlCeConnection(connectionString);
        }
    }
}
