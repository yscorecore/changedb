using System;
using System.Data;
using System.Data.Common;
using System.IO;

namespace ChangeDB.Dump
{
    internal class SqlScriptDbConnection : DbConnection
    {
        public SqlScriptDbConnection(TextWriter writer)
        {
            Writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }
        public override string ConnectionString { get; set; }
        public override string Database { get; }
        public override string DataSource { get; }
        public override string ServerVersion { get; }
        public override ConnectionState State { get; }
        private TextWriter Writer { get; }
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
        protected override DbCommand CreateDbCommand() => new SqlScriptDbCommand(this.Writer);
        
        
        internal class SqlScriptDbCommand : DbCommand
        {
            public SqlScriptDbCommand(TextWriter writer)
            {
                Writer = writer;
            }

            private TextWriter Writer { get; }
            public override string CommandText { get; set; }
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection DbConnection { get; set; }
            protected override DbParameterCollection DbParameterCollection { get; }
            protected override DbTransaction DbTransaction { get; set; }

            public override void Cancel() { }

            public override int ExecuteNonQuery()
            {
                this.Writer.WriteLine(this.CommandText);
                this.Writer.WriteLine();
                return 1;
            }
            public override object ExecuteScalar() => throw new NotImplementedException();
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => throw new NotSupportedException($"not support parameter in {nameof(SqlScriptDbCommand)}");
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
        }
    }

}
