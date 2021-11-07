using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Agent.Postgres
{
     public static class TestUtils
    {
        private static readonly Random random = new Random();
        public static string RandomDatabaseName() => $"testdb_{random.Next(100000):d6}";
    }
}
