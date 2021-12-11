using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IDatabaseManager
    {
        Task DropDatabaseIfExists(DbConnection connection, MigrationContext migrationContext);

        Task CreateDatabase(DbConnection connection, MigrationContext migrationContext);

        public async Task ReCreateDatabase(DbConnection connection, MigrationContext migrationContext)
        {
            await DropDatabaseIfExists(connection, migrationContext);
            await CreateDatabase(connection, migrationContext);
        }
    }
}
