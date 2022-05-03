using System;
using System.Data;
using System.Data.Common;
using ChangeDB.Migration;
using Npgsql;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresAgent : BaseAgent
    {
        public override AgentSetting AgentSetting => new AgentSetting { ObjectNameMaxLength = 63, DefaultSchema = "public", SupportSchema = true, IdentityName = PostgresUtils.IdentityName, DatabaseType = "postgres" };

    }
}
