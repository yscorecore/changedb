using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using ChangeDB.Dump;
using ChangeDB.Import;
using ChangeDB.IntegrationTest;
using ChangeDB.Migration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TestDB;
using Xunit;

namespace ChangeDB.IntegrationTest
{
    public class DefaultSqlDumperTest : BaseTest
    {


        [Theory]
        [InlineData("dumpsql/sqlserver_basic.xml")]
        public async Task ShouldDumpSql(string xmlFile)
        {
            var serviceProvider = BuildServiceProvider();
            var dumper = serviceProvider.GetRequiredService<IDatabaseSqlDumper>();
            var xDocument = XDocument.Load(xmlFile);
            var sourceNode = xDocument.XPathSelectElement("root/source");
            var sourceType = sourceNode.Attribute("type").Value;
            using var sourceDatabase = await InitSourceDatabase(sourceType, sourceNode.Value);


            foreach (var targetNode in xDocument.XPathSelectElements("root/targets/target"))
            {
                var targetType = targetNode.Attribute("type").Value;
                using var tempFile = new TempFile();
                using (var textwriter = new StreamWriter(tempFile.FilePath))
                {
                    var dumpContext = new DumpContext()
                    {
                        Setting = CreateSetting(targetNode),
                        SourceDatabase = new DatabaseInfo() { DatabaseType = sourceType, ConnectionString = sourceDatabase.ConnectionString },
                        TargetDatabase = new DatabaseInfo() { DatabaseType = targetType },
                        Writer = textwriter,
                    };
                    await dumper.DumpSql(dumpContext);
                    await textwriter.FlushAsync();
                }
                AssertTargetSqlStript(targetNode, tempFile.FilePath);
            }
        }

        private void AssertTargetSqlStript(XElement targetNode, string sqlScriptFile)
        {
            var content = File.ReadAllText(sqlScriptFile).Trim().Replace("\r", string.Empty);
            var allSql = targetNode.Value.Trim().Replace("\r", string.Empty);
            content.Should().Be(allSql, "the dump scripts should be same");
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

        private IServiceProvider BuildServiceProvider()
        {
            var sc = new ServiceCollection();
            sc.AddChangeDb();
            return sc.BuildServiceProvider();
        }

    }
}
