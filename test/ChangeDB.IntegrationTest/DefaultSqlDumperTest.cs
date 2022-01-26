using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using ChangeDB.Dump;
using ChangeDB.Import;
using ChangeDB.Migration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ChangeDB
{
    [Collection(nameof(DatabaseEnvironment))]
    public class DefaultSqlDumperTest
    {
        private readonly DatabaseEnvironment _databaseEnvironment;

        public DefaultSqlDumperTest(DatabaseEnvironment databaseEnvironment)
        {
            this._databaseEnvironment = databaseEnvironment;
        }

        [Theory]
        [InlineData("dumpsql/sqlserver_basic.xml")]
        public async Task ShouldDumpSql(string xmlFile)
        {
            var serviceProvider = BuildServiceProvider();
            var dumper = serviceProvider.GetRequiredService<IDatabaseSqlDumper>();
            var importer = serviceProvider.GetRequiredService<IDatabaseSqlImporter>();
            var xDocument = XDocument.Load(xmlFile);
            var sourceNode = xDocument.XPathSelectElement("root/source");
            var sourceType = sourceNode.Attribute("type").Value;
            var sourceConnectionString = _databaseEnvironment.NewConnectionString(sourceType);
            await InitSourceDatabase(importer, sourceType, sourceConnectionString, sourceNode);

            foreach (var targetNode in xDocument.XPathSelectElements("root/targets/target"))
            {
                var targetType = targetNode.Attribute("type").Value;
                var scope = (MigrationScope)Enum.Parse(typeof(MigrationScope), targetNode.Attribute("scope")?.Value ?? "All", true);
                var tempFile = Path.GetRandomFileName();
                var dumpContext = new DumpContext()
                {
                    Setting = new MigrationSetting() { MaxTaskCount = 1, MigrationScope = scope },
                    SourceDatabase = new DatabaseInfo() { DatabaseType = sourceType, ConnectionString = sourceConnectionString },
                    TargetDatabase = new DatabaseInfo() { DatabaseType = targetType },
                    DumpInfo = new SqlScriptInfo { DatabaseType = targetType, SqlScriptFile = tempFile },
                    MigrationType = MigrationType.SqlScript
                };
                await dumper.DumpSql(dumpContext);
                AssertTargetSqlStript(targetNode, tempFile);
            }
        }

        private void AssertTargetSqlStript(XElement targetNode, string sqlScriptFile)
        {
            var content = File.ReadAllText(sqlScriptFile).Trim().Replace("\r", string.Empty);
            var allSql = targetNode.Value.Trim().Replace("\r", string.Empty);
            content.Should().Be(allSql, "the dump scripts should be same");
        }

        private async Task InitSourceDatabase(IDatabaseSqlImporter databaseSqlImporter, string agentType,
            string dbConnectionString, XElement xElement)
        {
            await using var dbConnection = _databaseEnvironment.NewConnection(agentType, dbConnectionString);
            var split = xElement.Attribute("split").Value;
            var sql = xElement.Value;
            var tempFile = System.IO.Path.GetRandomFileName();
            await File.WriteAllTextAsync(tempFile, sql);
            var importContext = new ImportContext
            {
                TargetDatabase = new DatabaseInfo { DatabaseType = agentType, ConnectionString = dbConnectionString },
                ReCreateTargetDatabase = true,
                SqlScripts = new CustomSqlScript { SqlFile = tempFile, SqlSplit = split }
            };
            await databaseSqlImporter.Import(importContext);
        }

        private IServiceProvider BuildServiceProvider()
        {
            var sc = new ServiceCollection();
            sc.AddChangeDb();
            return sc.BuildServiceProvider();
        }

    }
}
