using System;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB
{
    public class CachedTestDatabaseManager : IDisposable, IAsyncDisposable, ITestDatabaseManager
    {
        private ConcurrentDictionary<CachedDatabase, bool> cachedDatabase = new();

        private readonly IConnectionProvider connectionProvider;
        private readonly IDatabaseManager databaseManager;
        private readonly string connectionTemplate;
        private readonly Random random = new();
        private readonly AgentSetting agentSetting;

        private bool isClear = false;


        public CachedTestDatabaseManager(string dbType, string connectionTemplate)
        {
            this.connectionTemplate = connectionTemplate;
            var agent = TestAgentFactorys.GetAgentByDbType(dbType);
            connectionProvider = agent.ConnectionProvider;
            databaseManager = agent.DatabaseManger;
            agentSetting = agent.AgentSetting;
        }


        public ITestDatabase CreateDatabase()
        {
            CheckStatus();
            var databases = cachedDatabase.Where(p => p.Value == false)
                 .Select(p => p.Key).ToList();
            foreach (var db in databases)
            {
                if (cachedDatabase.TryUpdate(db, true, false))
                {
                    return db;
                }
            }
            var databaseName = NewDatabaseName();
            var newConnectionString = connectionProvider.ChangeDatabase(connectionTemplate, databaseName);
            var newConnection = connectionProvider.CreateConnection(newConnectionString);
            databaseManager.CreateDatabase(newConnectionString, new MigrationSetting());
            var database = new CachedDatabase(this, newConnectionString, newConnection, databaseName, agentSetting.ScriptSplit);
            cachedDatabase.TryAdd(database, true);
            return database;

        }
        private string NewDatabaseName()
        {
            return $"cb{DateTime.Now:yyMMddHHmmss}_{random.Next(100000, 999999)}";
        }
        public ITestDatabase RequestDatabase()
        {
            var databaseName = NewDatabaseName();
            var newConnectionString = connectionProvider.ChangeDatabase(connectionTemplate, databaseName);
            var newConnection = connectionProvider.CreateConnection(newConnectionString);
            var database = new CachedDatabase(this, newConnectionString, newConnection, databaseName, agentSetting.ScriptSplit);
            cachedDatabase.TryAdd(database, true);
            return database;
        }


        private void ReleaseDatabase(ITestDatabase testdatabase)
        {
            var database = (CachedDatabase)testdatabase;
            CheckStatus();
            if (cachedDatabase.ContainsKey(database))
            {
                databaseManager.CleanDatabase(testdatabase.Connection, new MigrationSetting()).Wait();
                Debug.Assert(cachedDatabase.TryUpdate(database, false, true), "should update success.");
            }
        }
        private void CheckStatus([CallerMemberName] string callMemberName = "")
        {
            if (isClear)
            {
                throw new InvalidOperationException($"can not invoke {callMemberName} when cleaned.");
            }
        }
        public void Clean()
        {
            if (!isClear)
            {
                Parallel.ForEach(this.cachedDatabase, (kv) =>
                {
                    kv.Key.Connection.Dispose();
                    databaseManager.DropTargetDatabaseIfExists(kv.Key.ConnectionString, new MigrationSetting());
                });
                isClear = true;
            }
        }
        public void Dispose()
        {
            Clean();
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Run(Dispose);
        }



        private class CachedDatabase : ITestDatabase
        {
            public CachedDatabase(CachedTestDatabaseManager databaseManager, string connection, IDbConnection dbConnection, string databaseName, string scriptSplit)
            {
                this.databaseManager = databaseManager;
                this.ConnectionString = connection ?? throw new ArgumentNullException(nameof(connection));
                this.Connection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
                this.DatabaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
                this.ScriptSplit = scriptSplit ?? string.Empty;
            }
            private readonly CachedTestDatabaseManager databaseManager;

            public IDbConnection Connection { get; }
            public string ConnectionString { get; }
            public string DatabaseName { get; }
            public string ScriptSplit { get; }

            public void Dispose()
            {
                databaseManager.ReleaseDatabase(this);
            }

            public async ValueTask DisposeAsync()
            {
                await Task.Run(Dispose);
            }
        }
    }
}
