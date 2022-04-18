using System;
using System.Data.Common;
using ChangeDB.Import;
using FluentAssertions;
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
            ISqlScriptExecutor sqlScriptExecutor = new SqlServerSqlScriptExecutor();
            using var database = CreateDatabase(false);
            using var tempFile = new TempFile(content);
            sqlScriptExecutor.ExecuteFile(tempFile.FilePath, database.Connection);
            var rowCount = database.Connection.ExecuteScalar<int>("select count(1) from lasttable");
            rowCount.Should().Be(0);
        }
    }
}
