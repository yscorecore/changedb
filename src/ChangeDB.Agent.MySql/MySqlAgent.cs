using System.Data;
using System.Data.Common;
using ChangeDB.Migration;
using MySqlConnector;

namespace ChangeDB.Agent.MySql
{
    public class MySqlAgent : BaseAgent
    {
        public override AgentSetting AgentSetting => new AgentSetting
        {
            DefaultSchema = null,
            ObjectNameMaxLength = 64,
            DatabaseType = "mysql",
            IdentityName = MySqlUtils.IdentityName
        };

    }
}
