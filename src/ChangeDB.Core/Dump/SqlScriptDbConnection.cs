using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ChangeDB.Migration;

namespace ChangeDB.Dump
{
    internal class SqlScriptDbConnection : DbConnection
    {
        public SqlScriptDbConnection(string outputScriptFile, bool createNew, IRepr repr)
        {

            _ = outputScriptFile ?? throw new ArgumentNullException(nameof(outputScriptFile));
            Repr = repr ?? throw new ArgumentNullException(nameof(repr));
            FileStream = new Lazy<StreamWriter>(() =>
            {
                var baseStream = File.Open(outputScriptFile, createNew ? FileMode.CreateNew : FileMode.Create, FileAccess.Write, FileShare.Read);
                return new StreamWriter(baseStream);


            }, true);

        }
        public override string ConnectionString { get; set; }
        public override string Database { get; }
        public override string DataSource { get; }
        public override string ServerVersion { get; }
        public override ConnectionState State { get; }


        public IRepr Repr { get; }

        public Lazy<StreamWriter> FileStream { get; }

        public override void ChangeDatabase(string databaseName) { }

        public override void Close() { }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (FileStream.IsValueCreated)
            {
                FileStream.Value.Flush();
                FileStream.Value.Dispose();
            }
        }

        public override void Open() { }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
        protected override DbCommand CreateDbCommand() => new SqlScriptDbCommand(this);
    }

    internal class SqlScriptDbCommand : DbCommand
    {
        public new SqlScriptDbConnection Connection { get; }

        public SqlScriptDbCommand(SqlScriptDbConnection connection)
        {
            Connection = connection;
            DbParameterCollection = new SqlScriptDbParameterCollection();
        }

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
            var line = BuildCommandText();
            var writer = this.Connection.FileStream.Value;
            writer.WriteLine(line);
            writer.WriteLine();
            return 1;
        }
        private string BuildCommandText()
        {
            var dataDic = this.DbParameterCollection.OfType<DbParameter>()
               .ToDictionary(p => p.ParameterName, p => this.Connection.Repr.ReprValue(p.Value, p.DbType));
            if (dataDic.Count == 0) return this.CommandText;
            return Regex.Replace(CommandText, @"@\w+", (m) =>
            {
                if (dataDic.TryGetValue(m.Value, out var text))
                {
                    return text;
                }
                return m.Value;
            });
        }
        public override object ExecuteScalar() => throw new NotImplementedException();
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new SqlScriptDbParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
    }

    internal class SqlScriptDbParameterCollection : DbParameterCollection
    {
        readonly List<DbParameter> _dbParameters = new List<DbParameter>();
        public override int Count { get => _dbParameters.Count; }
        public override object SyncRoot { get => this; }

        public override int Add(object value)
        {
            if (value is not DbParameter dbParameter)
            {
                throw new ArgumentException($"value should be of type '{typeof(DbParameter)}'");
            }

            _dbParameters.Add(dbParameter);
            return 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var item in values)
            {
                Add(item);
            }
        }

        public override void Clear() => _dbParameters.Clear();

        public override bool Contains(object value) => throw new NotImplementedException();

        public override bool Contains(string value) => throw new NotImplementedException();

        public override void CopyTo(Array array, int index) { }

        public override IEnumerator GetEnumerator() => this._dbParameters.GetEnumerator();

        public override int IndexOf(object value) => throw new NotImplementedException();
        public override int IndexOf(string parameterName) => throw new NotImplementedException();

        public override void Insert(int index, object value) => throw new NotImplementedException();

        public override void Remove(object value) => throw new NotImplementedException();

        public override void RemoveAt(int index) => throw new NotImplementedException();

        public override void RemoveAt(string parameterName) => throw new NotImplementedException();

        protected override DbParameter GetParameter(int index) => _dbParameters[index];

        protected override DbParameter GetParameter(string parameterName) => throw new NotImplementedException();

        protected override void SetParameter(int index, DbParameter value) => throw new NotImplementedException();

        protected override void SetParameter(string parameterName, DbParameter value) => throw new NotImplementedException();
    }


    internal class SqlScriptDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; }
        public override int Size { get; set; }
        public override string SourceColumn { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override object Value { get; set; }
        public override void ResetDbType() { }
    }
}
