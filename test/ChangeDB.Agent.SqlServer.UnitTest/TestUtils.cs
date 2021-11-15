using System;

namespace ChangeDB.Agent.SqlServer
{
    public static class TestUtils
    {
        private static readonly Random random = new Random();
        public static string RandomDatabaseName() => $"testdb_{random.Next(100000):d6}";
    }
}
