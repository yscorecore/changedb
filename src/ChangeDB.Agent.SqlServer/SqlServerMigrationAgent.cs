﻿using System.Data.Common;
using ChangeDB.Migration;
using Microsoft.Data.SqlClient;

namespace ChangeDB.Agent.SqlServer
{

    public class SqlServerMigrationAgent : IMigrationAgent
    {
        public IDataMigrator DataMigrator => SqlServerDataMigrator.Default;
        public IMetadataMigrator MetadataMigrator => SqlServerMetadataMigrator.Default;
        public IDataTypeMapper DataTypeMapper => SqlServerDataTypeMapper.Default;
        public ISqlExpressionTranslator ExpressionTranslator => SqlServerSqlExpressionTranslator.Default;
        public IDatabaseManager DatabaseManger => SqlServerDatabaseManager.Default;
        public AgentSetting AgentSetting => new AgentSetting { ObjectNameMaxLength = 128, DefaultSchema = "dbo", SupportSchema = true, IdentityName = SqlServerUtils.IdentityName };
        public IRepr Repr => SqlServerRepr.Default;
        public IDataDumper DataDumper => SqlServerDataDumper.Default;

        public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    }
}
