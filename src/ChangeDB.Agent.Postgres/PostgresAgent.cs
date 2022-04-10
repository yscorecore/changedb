using System;
using System.Data;
using System.Data.Common;
using ChangeDB.Migration;
using Npgsql;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresAgent : IAgent
    {
        public IDataMigrator DataMigrator => PostgresDataMigrator.Default;
        public IMetadataMigrator MetadataMigrator => PostgresMetadataMigrator.Default;
        public IDatabaseManager DatabaseManger => PostgresDatabaseManager.Default;
        public AgentSetting AgentSetting => new AgentSetting { ObjectNameMaxLength = 63, DefaultSchema = "public", SupportSchema = true, IdentityName = PostgresUtils.IdentityName,DatabaseType="postgres" };
        public IDataDumper DataDumper => PostgresDataDumper.Default;

        public IConnectionProvider ConnectionProvider => PostgresConnectionProvider.Default;

    }
}
