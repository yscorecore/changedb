using System.Data;
using System.Data.Common;
using System.Data.SqlServerCe;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeAgent : BaseAgent
    {
        public override AgentSetting AgentSetting => 
            new AgentSetting
            {
                DefaultSchema = null, 
                ObjectNameMaxLength = 128, 
                IdentityName = SqlCeUtils.IdentityName, 
                DatabaseType = "sqlce"
            };
    }
}
