using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Agent.SqlServer;
using ChangeDB.Migration;
using Microsoft.Data.SqlClient;

namespace ChangeDB.Agent.SqlCe
{
    [Service(typeof(IMigrationAgent), Name = "sqlce")]
    public class SqlCeMigrationAgent : IMigrationAgent
    {
        public IDataMigrator DataMigrator { get => SqlCeDataMigrator.Default; }
        public IMetadataMigrator MetadataMigrator { get => SqlCeMetadataMigrator.Default; }
        public IDataTypeMapper DataTypeMapper { get => SqlCeDataTypeMapper.Default; }
        public ISqlExpressionTranslator ExpressionTranslator { get => SqlServerSqlExpressionTranslator.Default; }
        public IDatabaseManager DatabaseManger { get => SqlCeDatabaseManager.Default; }
        public AgentSetting AgentSetting { get => new AgentSetting { DefaultSchema = null, ObjectNameMaxLength = 128 }; }
        public IRepr Repr { get => SqlServerRepr.Default; }

        public DbConnection CreateConnection(string connectionString) => new SqlCeConnection(connectionString);

    }
}
