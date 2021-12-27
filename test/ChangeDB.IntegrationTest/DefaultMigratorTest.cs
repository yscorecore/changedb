using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xaml.Permissions;
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
        [InlineData("migrations/sqlserver_northwind.xml")]
        public async Task ShouldMigrationDatabaseAs(string xmlFile)
        {
            var serviceProvider = BuildServiceProvider();
            var migrate = serviceProvider.GetRequiredService<IDatabaseMigrate>();
            var xDocument = XDocument.Load(xmlFile);
            var sourceNode = xDocument.XPathSelectElement("root/source");
            var sourceType = sourceNode.Attribute("type").Value;

            using var sourceConnection = RandomDbConnection(sourceType);
            var sourceConnectionString = sourceConnection.ConnectionString;

            InitSourceDatabase(sourceType, sourceConnection, sourceNode);

            foreach (var targetNode in xDocument.XPathSelectElements("root/targets/target"))
            {
                var targetType = targetNode.Attribute("type").Value;
                var migrationContext = new MigrationContext()
                {
                    Setting = new MigrationSetting() { MaxTaskCount = 1 },
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
            dbConnection.ExecuteSqlScript(sql, split);
            //OutputTestData(dbConnection,"postgres");
        }

        private void OutputTestData(DbConnection dbConnection, string dbType)
        {
            var allTables = dbConnection.ExecuteReaderAsList<string, String>(
                "select t.table_schema,t.table_name  from information_schema.tables t  where t.table_type ='BASE TABLE'");
            //<target type="postgres">
            var rootElement = new XElement("target");
            rootElement.Add(new XAttribute("type", dbType));

            foreach (var table in allTables)
            {
                var tableFullName = $"\"{table.Item1}\".\"{table.Item2}\"";
                int totalCount =
                    dbConnection.ExecuteScalar<int>($"select count(1) from {tableFullName}");
                var schemas = dbConnection.ExecuteAsSchema(tableFullName);
                var dataTable = dbConnection.ExecuteReaderAsTable($"select * from {tableFullName}");

                var allDataJson = dataTable.Rows.OfType<DataRow>().Select(p => p.ItemArray.Select(ConvertToJsonText).ToArray()).ToArray();
                var serializeOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var dataJson = JsonSerializer.Serialize(allDataJson, serializeOptions);
                var metaDataElement = new XElement("meta",
                    schemas.Select(p => new XElement("column",
                        new XAttribute("name", p.Name),
                        new XAttribute("type", p.Type.FullName)
                        )).OfType<object>().ToArray()
                    );
                var dataElement = new XElement("data", new XCData(dataJson));
                var tableElement = new XElement("table",
                    new XAttribute("schema", table.Item1),
                    new XAttribute("name", table.Item2),
                    new XAttribute("count", totalCount),
                    metaDataElement,
                    dataElement

                );
                rootElement.Add(tableElement);
            }

            var text = rootElement.ToString();
            Console.WriteLine(text);
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
            countInDatabase.Should().Be(count, $"the data total count in table {tableName} should be same");
        }
        private void AssertMetaData(XElement tableElement, string tableName, DbConnection connection)
        {
            var actualColumns = connection.ExecuteAsSchema(tableName).ToDictionary(p => p.Name);

            var expectedColumns = tableElement.XPathSelectElements("meta/column")
                .Select(p => new
                {
                    Name = p.Attribute("name").Value,
                    Type = Type.GetType(p.Attribute("type").Value)
                }).ToDictionary(p => p.Name);

            actualColumns.Should().BeEquivalentTo(expectedColumns, $"the columns in table {tableName} should be same");
        }
        private void AssertData(XElement tableElement, string tableName, DbConnection connection)
        {
            var dataTable = connection.ExecuteReaderAsTable($"select * from {tableName}");
            var dataInDataBase = dataTable.Rows.OfType<DataRow>().Select(p => p.ItemArray.Select(ConvertToJsonText).ToArray()).ToArray();
            var serializeOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var dataJsonInDataBase = JsonSerializer.Serialize(dataInDataBase, serializeOptions);

            var dataExpected = JsonSerializer.Deserialize(tableElement.Element("data").Value, typeof(object), serializeOptions);
            var dataJsonExpected = JsonSerializer.Serialize(dataExpected, serializeOptions);
            dataJsonInDataBase.Should().Be(dataJsonExpected, $"the data in table {tableName} should be same");

        }
        private object ConvertToJsonText(object value)
        {
            if (Convert.IsDBNull(value))
            {
                return null;
            }
            if (value is TimeSpan ts)
            {
                return ts.ToString();
            }
            return value;
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
