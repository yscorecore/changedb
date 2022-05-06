using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Threading.Tasks;
using ChangeDB.Migration;
using static ChangeDB.Agent.SqlCe.SqlCeUtils;

namespace ChangeDB.Agent.SqlCe
{
    public class SqlCeDatabaseManager : IDatabaseManager
    {
        public static readonly IDatabaseManager Default = new SqlCeDatabaseManager();

        public Task CreateDatabase(string connectionString)
        {
            SqlCeEngine engine = new SqlCeEngine(connectionString);
            engine.CreateDatabase();
            return Task.CompletedTask;
        }

        public Task DropDatabaseIfExists(string connectionString)
        {
            var builder = new SqlCeConnectionStringBuilder(connectionString);
            var fileName = builder.DataSource;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            return Task.CompletedTask;
        }

     
        
    }
}
