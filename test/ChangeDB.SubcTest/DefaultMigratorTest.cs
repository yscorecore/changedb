using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using ChangeDB.Import;
using ChangeDB.Migration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TestDB;
using Xunit;

namespace ChangeDB.IntegrationTest
{
    public class DefaultMigratorTest : BaseTest
    {

        [Theory]
        [InlineData("migrations/sqlserver_default_value.xml")]
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

            using var sourceDatabase = await InitSourceDatabase(sourceType, sourceNode.Value);
            foreach (var targetNode in xDocument.XPathSelectElements("root/targets/target"))
            {
                var targetType = targetNode.Attribute("type").Value;
                using var targetDatabase = Databases.RequestDatabase(targetType);
                var migrationContext = new MigrationContext()
                {
                    Setting = CreateSetting(targetNode),
                    SourceDatabase = new DatabaseInfo() { DatabaseType = sourceType, ConnectionString = sourceDatabase.ConnectionString },
                    TargetDatabase = new DatabaseInfo() { DatabaseType = targetType, ConnectionString = targetDatabase.ConnectionString },
                };
                await migrate.MigrateDatabase(migrationContext);
                AssertTargetDatabase(targetNode, targetDatabase);
            }
        }
        private MigrationSetting CreateSetting(XElement xElement)
        {
            var setting = new MigrationSetting()
            {
                MaxTaskCount = 1
            };
            setting.MigrationScope = (MigrationScope)Enum.Parse(typeof(MigrationScope), xElement.Attribute("scope")?.Value ?? "All", true);
            setting.OptimizeInsertion = bool.Parse(xElement.Attribute("optimize-insertion")?.Value ?? "true");
            return setting;
        }

        private async Task<IDatabase> InitSourceDatabase(string agentType, string content)
        {
            await using var tempfile = new TempFile(content);
            await using var database = Databases.CreateDatabaseFromFile(agentType, true, tempfile.FilePath);
            return database;
        }



        private void AssertTargetDatabase(XElement xElement, IDatabase database)
        {
            foreach (var tableElement in xElement.XPathSelectElements("table"))
            {
                AssertTargetTable(tableElement, database);
            }
        }
        private void AssertTargetTable(XElement tableElement, IDatabase database)
        {
            var schema = tableElement.Attribute("schema")?.Value;
            var name = tableElement.Attribute("name")?.Value;
            var tableFullName = string.IsNullOrEmpty(schema) ? $"\"{name}\"" : $"\"{schema}\".\"{name}\"";

            AssertDataCount(tableElement, tableFullName, database.Connection);
            AssertMetaData(tableElement, tableFullName, database.Connection);
            AssertData(tableElement, tableFullName, database.Connection);

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


    }
}
