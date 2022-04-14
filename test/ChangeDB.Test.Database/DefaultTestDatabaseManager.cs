using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB
{

    public class DefaultTestDatabaseManager : ITestDatabaseManager
    {
        private readonly IConnectionProvider connectionProvider;
        private readonly IDatabaseManager databaseManager;
        private readonly string connectionTemplate;
        private readonly Random random = new();
        private readonly AgentSetting agentSetting;

        public DefaultTestDatabaseManager(string dbType, string connectionTemplate)
        {
            this.connectionTemplate = connectionTemplate;
            var agent = TestAgentFactorys.GetAgentByDbType(dbType);
            connectionProvider = agent.ConnectionProvider;
            databaseManager = agent.DatabaseManger;
            agentSetting = agent.AgentSetting;
        }



        public ITestDatabase CreateDatabase()
        {
            var databaseName = NewDatabaseName();
            var newConnectionString = connectionProvider.ChangeDatabase(connectionTemplate, databaseName);
            var newConnection = connectionProvider.CreateConnection(newConnectionString);
            databaseManager.CreateDatabase(newConnectionString, new MigrationSetting());
            return new DefaultTestDatabase(this.databaseManager)
            {
                ConnectionString = newConnectionString,
                Connection = newConnection,
                DatabaseName = databaseName,
                ScriptSplit = agentSetting.ScriptSplit
            };
        }

        public void Dispose()
        {

        }

        public async ValueTask DisposeAsync()
        {
            await Task.Run(Dispose);
        }

        public ITestDatabase RequestDatabase()
        {
            var databaseName = NewDatabaseName();
            var newConnectionString = connectionProvider.ChangeDatabase(connectionTemplate, databaseName);
            var newConnection = connectionProvider.CreateConnection(newConnectionString);
            return new DefaultTestDatabase(this.databaseManager)
            {
                ConnectionString = newConnectionString,
                Connection = newConnection,
                DatabaseName = databaseName,
                ScriptSplit = agentSetting.ScriptSplit
            };
        }


        private string NewDatabaseName()
        {
            return $"db{DateTime.Now:yyMMddHHmmss}_{random.Next(100000, 999999)}";
        }
        private class DefaultTestDatabase : ITestDatabase
        {
            private readonly IDatabaseManager databaseManager;

            public DefaultTestDatabase(IDatabaseManager databaseManager)
            {
                this.databaseManager = databaseManager;
            }
            public IDbConnection Connection { get; set; }
            public string ConnectionString { get; set; }
            public string DatabaseName { get; set; }
            public string ScriptSplit { get; set; }

            public void Dispose()
            {
                this.Connection.Dispose();
                databaseManager.DropTargetDatabaseIfExists(this.ConnectionString, new MigrationSetting());
            }

            public async ValueTask DisposeAsync()
            {
                await Task.Run(Dispose);
            }
        }
    }
}
