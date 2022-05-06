using System;
using System.Data.Common;
using ChangeDB.Import;
using FluentAssertions;
using TestDB;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerSqlScriptExecutorTest : BaseTest
    {

        [Theory]
        [InlineData("create table lasttable(id int);")]
        [InlineData(@"create table firsttable(id int);

create table lasttable(id int);
")]
        [InlineData(@"create table firsttable(id int);
go
create table lasttable(id int);
")]
        [InlineData(@"create table firsttable(id int);
Go
create table lasttable(id int);
")]
        [InlineData(@"create table firsttable(id int);
GO 
create table lasttable(id int);
")]
        [InlineData(@"create table firsttable(id int);
GO
insert into firsttable(id) values(1);
GO 
create table lasttable(id int);
")]
        [InlineData(@"create table firsttable(id int,val varchar(2000));
GO
insert into firsttable(id,val) values(1,'Go');
GO 
create table lasttable(id int);
")]
        [InlineData(@"create table firsttable(id int,val varchar(2000));
GO
insert into firsttable(id,val) values(1,'
Go
');
GO 
create table lasttable(id int);
")]
        [InlineData(@"create table firsttable(id int,val varchar(2000));
GO
insert into firsttable(id,val) values(1,'abc

bcd
');
GO 
create table lasttable(id int);
")]


        [InlineData(@"GO
GO
GO 
create table lasttable(id int);
")]
        public void ShouldSplitSqlScript(string content)
        {
            ISqlExecutor sqlExecutor = new SqlServerSqlExecutor();
            using var database = CreateDatabase(false);
            using var tempFile = new TempFile(content);
            sqlExecutor.ExecuteFile(tempFile.FilePath, CreateContext(database));
            var rowCount = database.Connection.ExecuteScalar<int>("select count(1) from lasttable");
            rowCount.Should().Be(0);
        }

        private AgentContext CreateContext(IDatabase database)
        {
            return new AgentContext()
            {
                Agent = new SqlServerAgent(),
                Connection = database.Connection,
                ConnectionString = database.ConnectionString

            };
        }
    }
}
