using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace TestDB.Core
{
    internal class DefaultDatabaseAllocator : IDisposable, IAsyncDisposable
    {

        private readonly IDatabaseServiceProvider serviceProvider;
        private readonly Random random = new();
        public DefaultDatabaseAllocator(string connectionTemplate, IDatabaseServiceProvider serviceProvider)
        {

            this.connectionTemplate = connectionTemplate;
            this.serviceProvider = serviceProvider;
        }
        private string connectionTemplate;

        public void Dispose()
        {
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Run(Dispose);
        }

        public IDatabase FromScriptFile(string sqlScriptFile, bool readOnly)
        {
            var databaseName = NewDatabaseName();
            var newConnectionString = serviceProvider.ChangeDatabase(this.connectionTemplate, databaseName);
            var newConnection = serviceProvider.CreateConnection(newConnectionString);
            serviceProvider.CreateDatabase(newConnectionString);
            newConnection.ExecuteNonQuery(serviceProvider.SplitFile(sqlScriptFile));
            return ReturnTestDatabase(readOnly, databaseName, newConnectionString, newConnection);

        }

        public IDatabase FromSqls(IEnumerable<string> initSqls, bool readOnly)
        {
            var databaseName = NewDatabaseName();
            var newConnectionString = serviceProvider.ChangeDatabase(this.connectionTemplate, databaseName);
            var newConnection = serviceProvider.CreateConnection(newConnectionString);
            serviceProvider.CreateDatabase(newConnectionString);
            newConnection.ExecuteNonQuery(initSqls);
            return ReturnTestDatabase(readOnly, databaseName, newConnectionString, newConnection);
        }

        private IDatabase ReturnTestDatabase(bool readOnly, string databaseName, string newConnectionString, DbConnection newConnection)
        {
            if (readOnly)
            {
                newConnection.Close();
                var readonlyConnectionString = serviceProvider.MakeReadOnly(newConnectionString);
                var readonlyConnection = serviceProvider.CreateConnection(readonlyConnectionString);
                return new DefaultTestDatabase(serviceProvider, readonlyConnection, readonlyConnectionString, databaseName, newConnectionString);
            }
            else
            {
                return new DefaultTestDatabase(serviceProvider, newConnection, newConnectionString, databaseName);
            }
        }

        public IDatabase RequestDatabase()
        {
            var databaseName = NewDatabaseName();
            var newConnectionString = serviceProvider.ChangeDatabase(this.connectionTemplate, databaseName);
            var newConnection = serviceProvider.CreateConnection(newConnectionString);
            return new DefaultTestDatabase(serviceProvider, newConnection, newConnectionString, databaseName);
        }
        private string NewDatabaseName(string prefix = "db")
        {
            return $"{prefix}_{DateTime.Now:yyMMddHHmmss_fff}_{random.Next(1000, 9999)}";
        }
        private class DefaultTestDatabase : IDatabase
        {
            private readonly IDatabaseManager databaseManager;

            public DefaultTestDatabase(IDatabaseManager databaseManager, DbConnection connection, string connectionString, string databaseName)
                : this(databaseManager, connection, connectionString, databaseName, connectionString)
            {

            }

            public DefaultTestDatabase(IDatabaseManager databaseManager, DbConnection connection, string connectionString, string databaseName, string originConnectionString)
            {
                this.databaseManager = databaseManager;
                Connection = connection;
                ConnectionString = connectionString;
                DatabaseName = databaseName;
                OriginConnectionString = originConnectionString;
            }
            public DbConnection Connection { get; set; }
            public string ConnectionString { get; set; }
            public string DatabaseName { get; set; }
            public string OriginConnectionString { get; }

            public void Dispose()
            {
                this.Connection.Dispose();
                databaseManager.DropTargetDatabaseIfExists(this.OriginConnectionString);
            }

            public async ValueTask DisposeAsync()
            {
                await Task.Run(Dispose);
            }
        }
    }

}
