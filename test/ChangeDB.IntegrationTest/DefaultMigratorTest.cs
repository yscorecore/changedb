using System;
using System.Data.Common;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using ChangeDB.Migration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ChangeDB.IntegrationTest
{
    [Collection(nameof(DatabaseEnvironment))]
    public class DefaultMigratorTest
    {
        private readonly DatabaseEnvironment databaseEnvironment;

        public DefaultMigratorTest(DatabaseEnvironment databaseEnvironment)
        {
            this.databaseEnvironment = databaseEnvironment;
        }

        [Theory]
        [InlineData("migrations/basic_sqlserver.xml")]
        public async Task ShouldMigrationDatabaseAs(string xmlFile)
        {
            var serviceProvider = BuildServiceProvider();
            var migrate = serviceProvider.GetRequiredService<IDatabaseMigrate>();
            var xdocument = XDocument.Load(xmlFile);
            var sourceNode = xdocument.XPathSelectElement("root/source");
            var sourceType = sourceNode.Attribute("type").Value;

            using var sourceConnection = RandomDbConnection(sourceType);
            var sourceConnectionString = sourceConnection.ConnectionString;

            InitSourceDatabase(sourceType, sourceConnection, sourceNode);

            foreach (var targetNode in xdocument.XPathSelectElements("root/targets/target"))
            {
                var targetType = targetNode.Attribute("type").Value;
                var migrationContext = new MigrationContext()
                {
                    Setting = new MigrationSetting(),
                    SourceDatabase = new DatabaseInfo() { DatabaseType = sourceType, ConnectionString = sourceConnectionString },
                    TargetDatabase = new DatabaseInfo() { DatabaseType = targetType, ConnectionString = RandomDbConnectionString(targetType) }

                };
                await migrate.MigrateDatabase(migrationContext);
                AssertTargetDatabase(targetNode, migrationContext);
            }
        }
        private void InitSourceDatabase(string agentType, DbConnection dbConnection, XElement xElement)
        {

            var split = xElement.Attribute("split").Value;
            var sql = xElement.Value;
            CreateSourceDatabase(agentType, dbConnection);
            dbConnection.ExecuteMutilSqls(sql, split);
        }
        private void CreateSourceDatabase(string agentType, DbConnection dbConnection)
        {
            switch (agentType?.ToLowerInvariant())
            {
                case "sqlserver":
                    Agent.SqlServer.ConnectionExtensions.CreateDatabase(dbConnection);
                    break;
                case "postgres":
                    Agent.Postgres.ConnectionExtensions.CreateDatabase(dbConnection);
                    break;
                default:
                    throw new NotSupportedException($"not support database type {agentType}");
            }
        }

        private void AssertTargetDatabase(XElement xElement, MigrationContext migrationContext)
        {

        }

        private IServiceProvider BuildServiceProvider()
        {
            var sc = new ServiceCollection();
            sc.AddChangeDb();
            return sc.BuildServiceProvider();
        }
        private DbConnection RandomDbConnection(string agentType)
        {
            return agentType?.ToLowerInvariant() switch
            {
                "sqlserver" => databaseEnvironment.NewSqlServerConnection(),
                "postgres" => databaseEnvironment.NewPostgresConnection(),
                _ => throw new NotSupportedException($"not support database type {agentType}")
            };
        }
        private string RandomDbConnectionString(string agentType)
        {
            using var connection = RandomDbConnection(agentType);
            return connection.ConnectionString;
        }

    }
}
