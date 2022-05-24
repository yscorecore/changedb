using System.Data;
using System.Data.Common;
using ChangeDB.Migration;
using MySqlConnector;

namespace ChangeDB.Agent.MySql
{
    public class MySqlAgent : BaseAgent
    {
        public override AgentSetting AgentSetting => new ()
        {
            DefaultSchema = null,
            ObjectNameMaxLength = 64,
            DatabaseType = "mysql",
            IdentityName = MySqlUtils.IdentityName,
            ConnectionTemplate = "Server=127.0.0.1;Port=3306;Database=myDatabase;Uid=myUsername;Pwd=myPassword;"
        };

    }
}
