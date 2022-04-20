using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestDB;
using Xunit;

namespace ChangeDB.Agent.Postgres
{
    [Collection(nameof(DatabaseEnvironment))]
    public class BaseTest
    {
        public static IDatabase CreateDatabase(bool readOnly, params string[] initsqls)
        {
            return Databases.CreateDatabase(DatabaseEnvironment.DbType, readOnly, initsqls);
        }
        public static IDatabase CreateDatabaseFromFile(bool readOnly, string fileName)
        {
            return Databases.CreateDatabaseFromFile(DatabaseEnvironment.DbType, readOnly, fileName);
        }
        public static IDatabase RequestDatabase()
        {
            return Databases.RequestDatabase(DatabaseEnvironment.DbType);
        }
    }
}
