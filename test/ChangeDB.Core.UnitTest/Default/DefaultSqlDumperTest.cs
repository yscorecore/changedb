using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Default;
using ChangeDB.Dump;
using ChangeDB.Migration;
using FluentAssertions;
using Moq;
using Xunit;

namespace ChangeDB.Core.Default
{
    public class DefaultSqlDumperTest
    {
        [Fact]
        public async Task ShouldIncludePostScript()
        {
            var emptyDatabaseTask = Task.FromResult(new DatabaseDescriptor());
            var mockRepr = Mock.Of<IRepr>();
            var mockAgentSetting = Mock.Of<AgentSetting>();
            var mockMetadataProvider = Mock.Of<IMetadataMigrator>(p => p.GetSourceDatabaseDescriptor(It.IsAny<MigrationContext>()) == emptyDatabaseTask);
            var mockSourceAgent = Mock.Of<IMigrationAgent>(p => p.MetadataMigrator == mockMetadataProvider && p.Repr == mockRepr && p.AgentSetting == mockAgentSetting);
            var mockAgentFactory = Mock.Of<IAgentFactory>(p => p.CreateAgent(It.IsAny<string>()) == mockSourceAgent);
            var dumper = new DefaultSqlDumper(mockAgentFactory);
            var customSqlScript = Path.GetTempFileName();
            File.WriteAllLines(customSqlScript, new string[]
            {
                "Hello;",
                "",
                "world;"

            });
            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            await dumper.DumpSql(new DumpContext
            {
                SourceDatabase = new DatabaseInfo { DatabaseType = "sourcedb" },
                TargetDatabase = new DatabaseInfo { DatabaseType = "targetdb" },
                Setting = new MigrationSetting
                {
                    PostScript = new CustomSqlScript { SqlFile = customSqlScript }
                },
                Writer = writer
            });
            using var reader = new StringReader(sb.ToString());
            var allLines = reader.ReadAllLines().ToArray();
            allLines[0].Should().Be("Hello;");
            allLines[1].Should().Be("");
            allLines[2].Should().Be("world;");
        }
    }
}
