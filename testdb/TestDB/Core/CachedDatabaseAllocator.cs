using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace TestDB.Core
{
    internal class CachedDatabaseAllocator : IDisposable, IAsyncDisposable
    {
        private readonly DatabasePooledPolicy databasePooledPolicy;
        private readonly ObjectPool<DatabaseInfo> databasePool;
        private readonly IDatabaseServiceProvider serviceProvider;

        public CachedDatabaseAllocator(string connectionTemplate, IDatabaseServiceProvider serviceProvider)
        {
            databasePooledPolicy = new DatabasePooledPolicy(connectionTemplate, serviceProvider);
            databasePool = ObjectPool.Create(databasePooledPolicy);
            this.serviceProvider = serviceProvider;
        }
        public void Dispose()
        {
            databasePooledPolicy.Dispose();
        }
        public async ValueTask DisposeAsync()
        {
            await Task.Run(Dispose);
        }


        public IDatabase CreateFromScriptFile(string sqlScriptFile)
        {
            var database = databasePool.Get();
            var connection = serviceProvider.CreateConnection(database.ConnectionString);
            connection.ExecuteNonQuery(serviceProvider.SplitFile(sqlScriptFile));
            return new CachedDatabase(database, databasePool, connection);
        }

        public IDatabase CreateFromSqlScripts(IEnumerable<string> sqlScripts)
        {
            var database = databasePool.Get();
            var connection = serviceProvider.CreateConnection(database.ConnectionString);
            connection.ExecuteNonQuery(sqlScripts);
            return new CachedDatabase(database, databasePool, connection);
        }
        private record DatabaseInfo
        {
            public string ConnectionString { get; set; }
            public string DatabaseName { get; set; }
        }
        private class DatabasePooledPolicy : IPooledObjectPolicy<DatabaseInfo>, IDisposable, IAsyncDisposable
        {
            private readonly ConcurrentDictionary<string, DatabaseInfo> cachedDatabases = new();
            private readonly IDatabaseServiceProvider serviceProvider;
            private readonly string connectionTemplate;
            private readonly Random random = new();

            public DatabasePooledPolicy(string connectionTemplate, IDatabaseServiceProvider serviceProvider)
            {
                this.connectionTemplate = connectionTemplate;
                this.serviceProvider = serviceProvider;
            }


            public DatabaseInfo Create()
            {
                var newDBName = NewDatabaseName();
                var connection = serviceProvider.ChangeDatabase(connectionTemplate, NewDatabaseName());
                serviceProvider.CreateDatabase(connection);
                var dbInfo = new DatabaseInfo { DatabaseName = newDBName, ConnectionString = connection };
                cachedDatabases.TryAdd(newDBName, dbInfo);
                return dbInfo;
            }
            private string NewDatabaseName(string prefix = "shdb")
            {
                return $"{prefix}_{DateTime.Now:yyMMddHHmmss_fff}_{random.Next(1000, 9999)}";
            }
            public bool Return(DatabaseInfo databaseInfo)
            {
                if (databaseInfo?.ConnectionString == null) return false;
                serviceProvider.CleanDatabase(databaseInfo.ConnectionString);
                return true;
            }


            public void Dispose()
            {
                Parallel.ForEach(cachedDatabases.Values, (p) => { serviceProvider.DropTargetDatabaseIfExists(p.ConnectionString); });
            }

            public async ValueTask DisposeAsync()
            {
                await Task.Run(Dispose);
            }
        }
        private class CachedDatabase : IDatabase
        {
            private readonly ObjectPool<DatabaseInfo> databasePool;
            private readonly DatabaseInfo databaseInfo;

            public CachedDatabase(DatabaseInfo databaseInfo, ObjectPool<DatabaseInfo> databasePool, IDbConnection connection)
            {
                this.databaseInfo = databaseInfo;
                this.databasePool = databasePool;
                Connection = connection;
            }

            public IDbConnection Connection { get; }
            public string ConnectionString => databaseInfo.ConnectionString;
            public string DatabaseName => databaseInfo.DatabaseName;

            public void Dispose()
            {
                databasePool.Return(this.databaseInfo);
            }

            public async ValueTask DisposeAsync()
            {
                await Task.Run(Dispose);
            }

        }

    }

}
