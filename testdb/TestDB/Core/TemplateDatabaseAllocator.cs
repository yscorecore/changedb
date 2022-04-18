using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TestDB.Core
{
    internal class TemplateDatabaseAllocator : IDisposable, IAsyncDisposable
    {
        private ConcurrentDictionary<string, TemplateDatabase> templateDatabases = new();
        private readonly IDatabaseServiceProvider serviceProvider;
        private readonly Random random = new();
        private readonly string connectionTemplate;

        public TemplateDatabaseAllocator(string connectionTemplate, IDatabaseServiceProvider serviceProvider)
        {
            this.connectionTemplate = connectionTemplate;
            this.serviceProvider = serviceProvider;
        }

        public void Dispose()
        {
            Parallel.ForEach(templateDatabases.Values, (Action<TemplateDatabase>)((p) => { this.serviceProvider.DropTargetDatabaseIfExists(p.OriginConnectionString); }));
        }
        public async ValueTask DisposeAsync()
        {
            await Task.Run(Dispose);
        }
        public IDatabase GetOrCreateTemplateFromScriptFile(string sqlScriptFile)
        {
            var dbName = $"temp_{GetFileHash(sqlScriptFile)}";
            if (!templateDatabases.ContainsKey(dbName))
            {
                lock (dbName)
                {
                    if (!templateDatabases.ContainsKey(dbName))
                    {
                        var databaseName = NewDatabaseName();
                        var newConnectionString = serviceProvider.ChangeDatabase(connectionTemplate, databaseName);
                        serviceProvider.CreateDatabase(newConnectionString);
                        using (var newConnection = serviceProvider.CreateConnection(newConnectionString))
                        {
                            newConnection.ExecuteNonQuery(serviceProvider.SplitFile(sqlScriptFile));
                        }
                        var readonlyConnection = serviceProvider.MakeReadOnly(newConnectionString);
                        templateDatabases.TryAdd(dbName, new TemplateDatabase
                        {
                            DatabaseName = databaseName,
                            OriginConnectionString = newConnectionString,
                            ConnectionString = readonlyConnection,
                            Connection = serviceProvider.CreateConnection(readonlyConnection)
                        });
                    }
                }
            }
            return templateDatabases[dbName];


        }
        public IDatabase GetOrCreateTemplateFromSqls(IEnumerable<string> initSqls)
        {
            var dbName = $"temp_{GetHash(initSqls)}";
            if (!templateDatabases.ContainsKey(dbName))
            {
                lock (dbName)
                {
                    if (!templateDatabases.ContainsKey(dbName))
                    {
                        var databaseName = NewDatabaseName();
                        var newConnectionString = serviceProvider.ChangeDatabase(connectionTemplate, databaseName);
                        serviceProvider.CreateDatabase(newConnectionString);
                        using (var newConnection = serviceProvider.CreateConnection(newConnectionString))
                        {
                            newConnection.ExecuteNonQuery(initSqls);
                        }
                        var readonlyConnection = serviceProvider.MakeReadOnly(newConnectionString);
                        templateDatabases.TryAdd(dbName, new TemplateDatabase
                        {
                            DatabaseName = databaseName,
                            OriginConnectionString = newConnectionString,
                            ConnectionString = readonlyConnection,
                            Connection = serviceProvider.CreateConnection(readonlyConnection)
                        });
                    }
                }
            }
            return templateDatabases[dbName];

        }

        public IDatabase GetTemplateFromScriptFile(string sqlScriptFile)
        {
            return templateDatabases.TryGetValue($"temp_{GetFileHash(sqlScriptFile)}", out var cachedDatabase)
                ? cachedDatabase : default;
        }
        public IDatabase GetTemplateFromSqls(IEnumerable<string> initSqls)
        {
            return templateDatabases.TryGetValue($"temp_{GetHash(initSqls)}", out var cachedDatabase)
                ? cachedDatabase : default;
        }
        public IDatabase CloneTemplateFromScriptFile(string sqlScriptFile)
        {
            var templateDatabase = GetOrCreateTemplateFromScriptFile(sqlScriptFile) as TemplateDatabase;
            var databaseName = NewDatabaseName("cldb");
            var newConnectionString = serviceProvider.ChangeDatabase(connectionTemplate, databaseName);
            serviceProvider.CloneDatabase(templateDatabase.OriginConnectionString, databaseName);

            return new DefaultDatabase(this.serviceProvider)
            {
                Connection = serviceProvider.CreateConnection(newConnectionString),
                ConnectionString = newConnectionString,
                DatabaseName = databaseName
            };
        }
        public IDatabase CloneTemplateFromSqls(IEnumerable<string> initSqls)
        {
            var templateDatabase = GetOrCreateTemplateFromSqls(initSqls) as TemplateDatabase;
            var databaseName = NewDatabaseName("cldb");
            var newConnectionString = serviceProvider.ChangeDatabase(connectionTemplate, databaseName);
            serviceProvider.CloneDatabase(templateDatabase.OriginConnectionString, databaseName);

            return new DefaultDatabase(this.serviceProvider)
            {
                Connection = serviceProvider.CreateConnection(newConnectionString),
                ConnectionString = newConnectionString,
                DatabaseName = databaseName
            };
        }

        private string NewDatabaseName(string prefix = "tmdb")
        {
            return $"{prefix}_{DateTime.Now:yyMMddHHmmss_fff}_{random.Next(1000, 9999)}";
        }
        private string GetHash(IEnumerable<string> initSqls)
        {
            using var sha = SHA256.Create();
            var hashs = (initSqls ?? Enumerable.Empty<string>())
                .Select(p => sha.ComputeHash(Encoding.UTF8.GetBytes(p ?? string.Empty)))
                .Aggregate(Enumerable.Empty<byte>(), (a, b) => a.Concat(b))
                .ToArray();
            var hash = sha.ComputeHash(hashs);
            return string.Join("", hash.Select(p => p.ToString("X2").Take(8)));
        }
        private string GetFileHash(string filePath)
        {
            using var sha = SHA256.Create();
            using var fileStream = File.OpenRead(filePath);
            var hash = sha.ComputeHash(fileStream);
            return string.Join("", hash.Take(8).Select(p => p.ToString("X2")));
        }


        private class TemplateDatabase : IDatabase
        {
            public DbConnection Connection { get; init; }
            public string ConnectionString { get; init; }
            public string DatabaseName { get; init; }
            public string OriginConnectionString { get; init; }

            public void Dispose()
            {
            }
            public async ValueTask DisposeAsync()
            {
                await Task.Run(Dispose);
            }
        }

        private class DefaultDatabase : IDatabase
        {
            private readonly IDatabaseManager databaseManager;

            public DefaultDatabase(IDatabaseManager databaseManager)
            {
                this.databaseManager = databaseManager;
            }

            public DbConnection Connection { get; init; }
            public string ConnectionString { get; init; }
            public string DatabaseName { get; init; }

            public void Dispose()
            {
                databaseManager.DropTargetDatabaseIfExists(ConnectionString);
            }
            public async ValueTask DisposeAsync()
            {
                await Task.Run(Dispose);
            }
        }

    }

}
