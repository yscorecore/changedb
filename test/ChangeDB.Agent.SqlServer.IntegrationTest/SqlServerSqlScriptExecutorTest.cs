using System;
using System.Data.Common;
using ChangeDB.Import;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    [Collection(nameof(DatabaseEnvironment))]
    public class SqlServerSqlScriptExecutorTest : IDisposable
    {
        private readonly DbConnection _dbConnection;



        public SqlServerSqlScriptExecutorTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.DbConnection;

        }
        public void Dispose()
        {
            _dbConnection.ClearDatabase();
        }
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

            using var tempFile = new TempFile(content);
            sqlScriptExecutor.ExecuteFile(tempFile.FilePath, _dbConnection);

            var rowCount = _dbConnection.ExecuteScalar<int>("select count(1) from lasttable");
            rowCount.Should().Be(0);
        }
    }
}
