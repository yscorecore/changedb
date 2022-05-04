using System.Data;
using System.Data.Common;
using ChangeDB.Migration;
using Microsoft.Data.SqlClient;

namespace ChangeDB.Agent.SqlServer
{

    public class SqlServerAgent : BaseAgent
    {
        public override AgentSetting AgentSetting => new AgentSetting { ObjectNameMaxLength = 128, DefaultSchema = "dbo", IdentityName = SqlServerUtils.IdentityName, DatabaseType = "sqlserver" };

    }
}
