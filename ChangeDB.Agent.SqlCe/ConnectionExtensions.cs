using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Agent.SqlCe
{
    public static class  ConnectionExtensions
    {
        public static void ReCreateDatabase(this DbConnection connection)
        {
            throw new NotImplementedException();
        }
        public static int DropDatabaseIfExists(this DbConnection connection)
        {
            throw new NotImplementedException();
        }
        public static int CreateDatabase(this DbConnection connection)
        {
            throw new NotImplementedException();
        }
        public static string ExtractDatabaseName(this DbConnection connection)
        {
            throw new NotImplementedException();
        }

        private static DbConnection CreateNoDatabaseConnection(IDbConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
