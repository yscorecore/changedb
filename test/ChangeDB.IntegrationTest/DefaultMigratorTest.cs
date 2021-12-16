using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using ChangeDB.Migration;
using FluentAssertions;
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
        [InlineData("migrations/sqlserver_datatype.xml")]
        [InlineData("migrations/sqlserver_basic.xml")]
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
            var dbConnection = RandomDbConnection(migrationContext.TargetDatabase.DatabaseType);
            dbConnection.ConnectionString = migrationContext.TargetDatabase.ConnectionString;
            foreach (var tableElement in xElement.XPathSelectElements("table"))
            {
                AssertTargetTable(tableElement, dbConnection);
            }
        }
        private void AssertTargetTable(XElement tableElement, DbConnection connection)
        {
            var schema = tableElement.Attribute("schema")?.Value;
            var name = tableElement.Attribute("name")?.Value;
            var tableFullName = string.IsNullOrEmpty(schema) ? $"\"{name}\"" : $"\"{schema}\".\"{name}\"";

            AssertDataCount(tableElement, tableFullName, connection);
            AssertMetaData(tableElement, tableFullName, connection);
            AssertData(tableElement, tableFullName, connection);

        }
        private void AssertDataCount(XElement tableElement, string tableName, DbConnection connection)
        {
            var count = int.Parse(tableElement.Attribute("count").Value);
            var countInDatabase = connection.ExecuteScalar<int>($"Select count(1) from {tableName}");
            countInDatabase.Should().Be(count, $"the data total count in table {tableName} should be same.");
        }
        private void AssertMetaData(XElement tableElement, string tableName, DbConnection connection)
        {
            using var reader = connection.ExecuteReader($"select * from {tableName}");
            var querySchema = reader.GetSchemaTable();
            var expectedColumns = tableElement.XPathSelectElements("meta/column")
                .Select(p => new
                {
                    Name = p.Attribute("name").Value,
                    Type = Type.GetType(p.Attribute("type").Value)
                }).ToDictionary(p => p.Name);
            querySchema.Rows.Count.Should().Be(expectedColumns.Count(), $"the column count in table {tableName} should be same.");


            var actualColumns =
                querySchema.Rows.OfType<DataRow>()
                .Select(p => new
                {
                    Name = p.Field<string>("ColumnName"),
                    Type = p.Field<Type>("DataType")
                }).ToDictionary(p => p.Name);

            actualColumns.Should().BeEquivalentTo(expectedColumns, $"the columns in table {tableName} should be same.");
        }
        private void AssertData(XElement tableElement, string tableName, DbConnection connection)
        {
            var dataTable = connection.ExecuteReaderAsTable($"select * from {tableName}");
            var dataInDataBase = dataTable.Rows.OfType<DataRow>().Select(p => p.ItemArray).ToArray();
            var dataJsonInDataBase = JsonSerializer.Serialize(dataInDataBase);

            var dataExpected = JsonSerializer.Deserialize(tableElement.Element("data").Value, typeof(object));
            var dataJsonExpected = JsonSerializer.Serialize(dataExpected);
            dataJsonInDataBase.Should().Be(dataJsonExpected, $"the data in table {tableName} should be same.");

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
