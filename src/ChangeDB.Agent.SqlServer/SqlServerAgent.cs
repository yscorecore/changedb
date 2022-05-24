using System.Data;
using System.Data.Common;
using ChangeDB.Migration;
using Microsoft.Data.SqlClient;

namespace ChangeDB.Agent.SqlServer
{

    public class SqlServerAgent : BaseAgent
    {
        public override AgentSetting AgentSetting => new ()
        {
            ObjectNameMaxLength = 128, 
            DefaultSchema = "dbo", 
            IdentityName = SqlServerUtils.IdentityName, 
            DatabaseType = "sqlserver",
            ConnectionTemplate = "Server=127.0.0.1,1433;Database=myDatabase;User Id=myUsername;Password=myPassword",
        };

    }
}
