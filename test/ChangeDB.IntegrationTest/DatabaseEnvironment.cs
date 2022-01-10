using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using ChangeDB.Environments;
using Microsoft.Data.SqlClient;
using Npgsql;
using Xunit;

namespace ChangeDB
{
    [CollectionDefinition(nameof(DatabaseEnvironment))]
    public class DatabaseEnvironment : IDisposable, ICollectionFixture<DatabaseEnvironment>
    {
        private readonly IDictionary<string, IDatabaseEnvironment> _databases =
            new ConcurrentDictionary<string, IDatabaseEnvironment>();
        public DatabaseEnvironment()
        {
            var envs = new[] {
                new {Name = "postgres", Type = typeof(PostgresEnvironment)},
                new {Name = "sqlserver", Type = typeof(SqlServerEnvironment)}};
            Parallel.ForEach(envs, (e, s) =>
            {
                _databases[e.Name] = Activator.CreateInstance(e.Type) as IDatabaseEnvironment;
            });
        }



        public void Dispose()
        {
            Parallel.ForEach(_databases.Values, (a, p) => a.Dispose());
        }




        public string NewConnectionString(string dbType)
        {
            if (_databases.TryGetValue(dbType, out var environment))
            {
                return environment.NewConnectionString();
            }

            throw new NotSupportedException($"not support database type {dbType}");
        }


        public DbConnection CreateConnection(string dbType, string connectionString)
        {
            if (_databases.TryGetValue(dbType, out var environment))
            {
                return environment.CreateConnection(connectionString);
            }

            throw new NotSupportedException($"not support database type {dbType}");
        }
    }
}
