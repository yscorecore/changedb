using System.Data;
using System.Data.Common;
using ChangeDB.Import;
using ChangeDB.Migration;
using Moq;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{

    public class SqlServerSqlScriptExecutorTest
    {

        private AgentContext CreateContext(DbConnection connection)
        {
            return new AgentContext()
            {
                Agent = new SqlServerAgent(),
                Connection = connection
                
            };
        }

        [Theory]
        [InlineData("")]
        [InlineData("\n")]
        [InlineData("\r\ngo\ngo\r\n\n")]
        public void ShouldExecuteNoneTime(string sqls)
        {
            ISqlExecutor instance = new SqlServerSqlExecutor();

            var dbConnection = Mock.Of<DbConnection>();
            instance.ExecuteSqls(sqls, CreateContext(dbConnection));
            var mockInstance = Mock.Get(dbConnection);
            mockInstance.Verify(p => p.CreateCommand(), Times.Never());
        }

        [Theory]
        [InlineData("create table table1(id int);")]
        [InlineData("\r\ncreate table table1(id int);\n")]
        [InlineData("\ngo\ncreate table table1(id int);\rgo\n")]
        public void ShouldExecuteSingleLine(string sqls)
        {
            ISqlExecutor instance = new SqlServerSqlExecutor();
            var dbCommand = Mock.Of<IDbCommand>();
            var dbConnection = Mock.Of<DbConnection>(p => p.CreateCommand() == dbCommand);
            instance.ExecuteSqls(sqls, CreateContext(dbConnection));
            var mockInstance = Mock.Get(dbCommand);
            mockInstance.Verify(p => p.ExecuteNonQuery(), Times.Once());
            var firstSql = "create table table1(id int);";
            mockInstance.VerifySet(p => p.CommandText = firstSql, Times.Once());
        }

        [Fact]
        public void ShouldExecuteTwoSqls()
        {
            ISqlExecutor instance = new SqlServerSqlExecutor();
            var dbCommand = Mock.Of<IDbCommand>();
            var dbConnection = Mock.Of<DbConnection>(p => p.CreateCommand() == dbCommand);
            var sqls = @"create table table1(id int);
create table table1(id int);
create table table1(id int);
go
 
Go
GO
create table table2(id int);
";
            instance.ExecuteSqls(sqls, CreateContext(dbConnection));
            var mockInstance = Mock.Get(dbCommand);
            mockInstance.Verify(p => p.ExecuteNonQuery(), Times.Exactly(2));
            var firstSql = "create table table1(id int);\ncreate table table1(id int);\ncreate table table1(id int);";
            var secondSql = "create table table2(id int);";
            mockInstance.VerifySet(p => p.CommandText = firstSql, Times.Once());
            mockInstance.VerifySet(p => p.CommandText = secondSql, Times.Once());
        }

        [Theory]

        [InlineData(@"insert into abc(id) values('a

go

go');")]
        [InlineData(@"insert into [abc

        go

        bcd
        ] values('val');")]
        [InlineData(@"insert into ""abc

go

bcd
"" values('val');")]
        [InlineData(@"insert into abc(val) values('
go

go

');")]
        public void ShouldExecuteSqlsWhenKeyworkInStringContent(string sqls)
        {
            ISqlExecutor instance = new SqlServerSqlExecutor();
            var dbCommand = Mock.Of<IDbCommand>();
            var dbConnection = Mock.Of<DbConnection>(p => p.CreateCommand() == dbCommand);
            instance.ExecuteSqls(sqls, CreateContext(dbConnection));
            var mockInstance = Mock.Get(dbCommand);
            mockInstance.Verify(p => p.ExecuteNonQuery(), Times.Once());

            mockInstance.VerifySet(p => p.CommandText = sqls, Times.Once());
        }
        [Theory]

        [InlineData(@"insert into abc(id)\nvalues('a

go
-- help
go');")]
        public void ShouldExecuteSqlsWhenCommentInScript(string sqls)
        {
            ISqlExecutor instance = new SqlServerSqlExecutor();
            var dbCommand = Mock.Of<IDbCommand>();
            var dbConnection = Mock.Of<DbConnection>(p => p.CreateCommand() == dbCommand);
            instance.ExecuteSqls(sqls, CreateContext(dbConnection));
            var mockInstance = Mock.Get(dbCommand);
            mockInstance.Verify(p => p.ExecuteNonQuery(), Times.Once());

            mockInstance.VerifySet(p => p.CommandText = sqls, Times.Once());
        }
    }
}
