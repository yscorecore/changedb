using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChangeDB
{
    public static class TestDatabases
    {
        public const string POSTGRES = "postgres";
        public const string MYSQL = "mysql";
        public const string SQLCE = "sqlce";
        public const string SQLSERVER = "sqlserver";
        public static Func<string, string> BuildEnvName = (p) => $"{p.ToUpperInvariant()}__CONN";
        public static IDisposable SetupPostgresEnvironment()
        {
            var envName = BuildEnvName(POSTGRES);
            if (Environment.GetEnvironmentVariable(envName) != null)
            {
                return new EmptyTestEnvironment();
            }
            else
            {
                var port = Utility.GetRandomTcpPort();
                var dockerCompose = DockerCompose.Up(Path.Combine("dockerfiles", "postgres.yml"), new Dictionary<string, object> { ["POSTGRES_DBPORT"] = port }, "db:5432");
                Environment.SetEnvironmentVariable(envName, $"Server=127.0.0.1;Port={port};User Id=postgres;Password=mypassword;");
                return dockerCompose;
            }
        }
        public static IDisposable SetupSqlServerEnvironment()
        {
            var envName = BuildEnvName(SQLSERVER);
            if (Environment.GetEnvironmentVariable(envName) != null)
            {
                return new EmptyTestEnvironment();
            }
            else
            {
                var port = Utility.GetRandomTcpPort();
                var dockerCompose = DockerCompose.Up(Path.Combine("dockerfiles", "sqlserver.yml"), new Dictionary<string, object> { ["SQLSERVER_DBPORT"] = port }, "db:1433");
                Environment.SetEnvironmentVariable(envName, $"Server=127.0.0.1,{port};User Id=sa;Password=myStrong(!)Password;");
                return dockerCompose;
            }
        }
        public static IDisposable SetupMysqlEnvironment()
        {
            var envName = BuildEnvName(MYSQL);
            if (Environment.GetEnvironmentVariable(envName) != null)
            {
                return new EmptyTestEnvironment();
            }
            else
            {
                var port = Utility.GetRandomTcpPort();
                var dockerCompose = DockerCompose.Up(Path.Combine("dockerfiles", "mysql.yml"), new Dictionary<string, object> { ["MYSQL_DBPORT"] = port }, "db:3306");
                Environment.SetEnvironmentVariable(envName, $"Server=127.0.0.1;Port={port};Uid=root;Pwd=password;");
                return dockerCompose;
            }
        }
        public static IDisposable SetupSqlceEnvironment()
        {
            var envName = BuildEnvName(SQLCE);
            if (Environment.GetEnvironmentVariable(envName) != null)
            {
                return new EmptyTestEnvironment();
            }
            else
            {
                Environment.SetEnvironmentVariable(envName, $"Data Source=MyData.sdf;Max Database Size=4091;Persist Security Info=False;Persist Security Info=False;");
                return new EmptyTestEnvironment();
            }
        }

        public static IDisposable SetupEnvironment(string databaseType)
        {
            Func<IDisposable> func = databaseType switch
            {
                POSTGRES => SetupPostgresEnvironment,
                SQLSERVER => SetupSqlServerEnvironment,
                SQLCE => SetupSqlceEnvironment,
                MYSQL => SetupMysqlEnvironment,
                _ => throw new NotImplementedException()
            };
            return func();
        }
        public static IDisposable[] SetupEnvironments(params string[] databaseType)
        {
            return (databaseType ?? Array.Empty<string>()).Select(SetupEnvironment).ToArray();
        }
        public static IEnumerable<string> GetSupportedDatababeFromEnvironment()
        {
            foreach (string env in Environment.GetEnvironmentVariables().Keys)
            {
                var match = Regex.Match(env, @"^(?<nm>\w+)__CONN$");
                if (match.Success)
                {
                    var dbType = match.Groups["nm"].Value;
                    yield return dbType;
                }
            }
        }
        public static ITestDatabaseManager CreateManagerFromEnvironment(string dbType, bool cachedDatabase = true)
        {
            var envName = BuildEnvName(dbType);
            foreach (string env in Environment.GetEnvironmentVariables().Keys)
            {
                if (string.Equals(envName, env, StringComparison.InvariantCultureIgnoreCase))
                {
                    var val = Environment.GetEnvironmentVariable(env);
                    return cachedDatabase
                        ? new CachedTestDatabaseManager(dbType, val)
                        : new DefaultTestDatabaseManager(dbType, val);
                }
            }
            throw new NotSupportedException($"can not find environment variable '{envName}'.");
        }

        public static IDictionary<string, ITestDatabaseManager> CreateManagersFromEnvironment(bool cachedDatabase = true)
        {
            var result = new Dictionary<string, ITestDatabaseManager>(StringComparer.InvariantCultureIgnoreCase);
            foreach (string dbType in GetSupportedDatababeFromEnvironment())
            {
                var envName = BuildEnvName(dbType);
                var val = Environment.GetEnvironmentVariable(envName);
                result[dbType] = cachedDatabase
                    ? new CachedTestDatabaseManager(dbType, val)
                    : new DefaultTestDatabaseManager(dbType, val);
            }
            return result;
        }
    }
}
