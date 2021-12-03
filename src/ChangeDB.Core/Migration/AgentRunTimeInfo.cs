using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public record AgentRunTimeInfo
    {
        public string DatabaseType { get; init; }
        public IMigrationAgent Agent { get; init; }
        public DatabaseDescriptor Descriptor { get; init; }
        public DbConnection Connection { get; init; }
    }
}
