using System;
using System.Data;
using System.Data.Common;
using ChangeDB.Migration;
using Npgsql;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresAgent : BaseAgent
    {
        public override AgentSetting AgentSetting =>
            new ()
            {
                ObjectNameMaxLength = 63,
                DefaultSchema = "public",
                IdentityName = PostgresUtils.IdentityName,
                DatabaseType = "postgres",
                 ConnectionTemplate = "Server=127.0.0.1;Port=5432;Database=myDatabase;User Id=myUsername;Password=myPassword;"
            };

    }
}
