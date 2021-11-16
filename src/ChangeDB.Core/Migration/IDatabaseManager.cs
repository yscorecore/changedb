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
        Task DropDatabaseIfExists(DbConnection connection, MigrationSetting migrationSetting);

        Task CreateDatabase(DbConnection connection, MigrationSetting migrationSetting);

        public async Task ReCreateDatabase(DbConnection connection, MigrationSetting migrationSetting)
        {
            await DropDatabaseIfExists(connection, migrationSetting);
            await CreateDatabase(connection, migrationSetting);
        }
    }
}
