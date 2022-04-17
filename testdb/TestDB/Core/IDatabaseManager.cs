using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDB
{
    public interface IDatabaseManager
    {
        void CleanDatabase(string connectionString);

        void CreateDatabase(string connectionString);

        void DropTargetDatabaseIfExists(string connectionString);

        void CloneDatabase(string connectionString, string newDatabaseName);
    }
}
