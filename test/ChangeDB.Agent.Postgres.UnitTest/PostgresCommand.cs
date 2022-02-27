using System;

namespace ChangeDB.Agent.Postgres
{

    public static class PostgresCommand
    {

        public static void PSql(string sqlFile, string database, string host = "host.docker.internal", int port = 5432, string user = "postgres", string password = "mypassword")
        {
            //docker run -e PGPASSWORD=mypassword -v C:\Users\Administrator\Source\Repos\changedb\test\ChangeDB.Agent.Postgres.UnitTest\bin\Debug\net5:/app --rm postgres psql -h host.docker.internal -p 44201 -U postgres  -d testdb_084017 -f /app/dump_fq52xi3b.03v.sql
            var arguments = @$"run -e PGPASSWORD=""{password}"" -v {Environment.CurrentDirectory}:/wd --rm postgres psql -h {host} -p {port} -U {user} -d {database} -f /wd/{sqlFile}";
            Shell.Exec("docker", arguments);
        }
    }
}
