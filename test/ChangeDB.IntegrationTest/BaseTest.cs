using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestDB;
using Xunit;

namespace ChangeDB.IntegrationTest
{
    [Collection(nameof(DatabaseEnvironment))]
    public class BaseTest
    {
        public static IEnumerable<object[]> GetScriptFiles()
        {
            var subDirectories = Directory.GetDirectories("testscripts");
            var supportAgents = DatabaseEnvironment.SupportedDatabases;
            foreach (var sourceTypeFoler in subDirectories)
            {
                var sourceType = Path.GetFileName(sourceTypeFoler);
                if (!supportAgents.Contains(sourceType, StringComparer.InvariantCultureIgnoreCase))
                {
                    continue;
                }
                foreach (var sqlFile in Directory.GetFiles(sourceTypeFoler, "*.sql"))
                {
                    yield return new object[] { sourceType, sqlFile };
                }
            }
        }
        public static IEnumerable<object[]> GetTestCases()
        {
            var subDirectories = Directory.GetDirectories("testscripts");
            var supportAgents = DatabaseEnvironment.SupportedDatabases;
            foreach (var sourceTypeFoler in subDirectories)
            {
                var sourceType = Path.GetFileName(sourceTypeFoler);
                if (!supportAgents.Contains(sourceType, System.StringComparer.InvariantCultureIgnoreCase))
                {
                    continue;
                }
                foreach (var sqlFile in Directory.GetFiles(sourceTypeFoler, "*.sql"))
                {
                    foreach (var targetType in supportAgents)
                    {
                        yield return new object[] { sourceType, sqlFile, targetType };
                    }
                }
            }
        }
    }
}
