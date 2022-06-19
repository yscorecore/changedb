namespace ChangeDB.Agent.Sqlite
{

    public class SqliteAgent : BaseAgent
    {
        public override AgentSetting AgentSetting => new()
        {
            // there is no a limit itself for the object name.
            ObjectNameMaxLength = 1024,
            IdentityName = (_, table) => SqliteUtils.IdentityName(table),
            DatabaseType = "sqlite"
        };

    }
}
