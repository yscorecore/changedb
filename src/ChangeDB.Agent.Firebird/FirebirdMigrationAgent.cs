using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FirebirdSql.Data.FirebirdClient;

namespace ChangeDB.Agent.Firebird
{
    public class FirebirdMigrationAgent : IMigrationAgent
    {
        public DbConnection CreateConnection(string connectionString) => new FbConnection(connectionString);

        public IDataMigrator DataMigrator => FirebirdDataMigrator.Default;

        public IMetadataMigrator MetadataMigrator => FirebirdMetadataMigrator.Default;

        public IDataTypeMapper DataTypeMapper => FirebirdDataTypeMapper.Default;

        public ISqlExpressionTranslator ExpressionTranslator => FirebirdExpressionTranslator.Default;

        public IDatabaseManager DatabaseManger => FirebirdDatabaseManager.Default;

        public IRepr Repr => FirebirdRepr.Default;

        public AgentSetting AgentSetting => new AgentSetting
        { SupportSchema = false, DefaultSchema = null, ObjectNameMaxLength = 64, IdentityName = FirebirdUtils.IdentityName };

    }
}

