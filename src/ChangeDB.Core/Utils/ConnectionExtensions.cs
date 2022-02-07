using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace ChangeDB
{
    public static class ConnectionExtensions
    {
        public static object ExecuteScalar(this IDbConnection connection, string sql, IDictionary<string, object> args = null)
        {
            TryOpen(connection);
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            if (args?.Count > 0)
            {
                foreach (var kv in args)
                {
                    var parameter = command.CreateParameter();
                    parameter.Value = kv.Value;
                    parameter.ParameterName = kv.Key;
                    command.Parameters.Add(parameter);
                }
            }
            var result = command.ExecuteScalar();
            return result == DBNull.Value ? null : result;

        }

        private static T ToValue<T>(this object val)
        {
            if (val == null || val == DBNull.Value)
            {
                return default(T);
            }
            else
            {
                var valType = Nullable.GetUnderlyingType(typeof(T));
                return (T)Convert.ChangeType(val, valType ?? typeof(T));
            }
        }

        private static T FieldValue<T>(this DataRow row, int index) => row[index].ToValue<T>();
        private static T FieldValue<T>(this DataRow row, string columnName) => row[columnName].ToValue<T>();
        public static T ExecuteScalar<T>(this IDbConnection connection, string sql, IDictionary<string, object> args = null)
        {
            return connection.ExecuteScalar(sql, args).ToValue<T>();
        }
        public static int ExecuteNonQuery(this IDbConnection connection, string sql)
        {
            return connection.ExecuteNonQuery(sql, default(IDictionary<string, object>));
        }
        public static int ExecuteNonQuery(this IDbConnection connection, string sql, IDictionary<string, object> args)
        {
            _ = sql ?? throw new ArgumentNullException(nameof(sql));
            TryOpen(connection);
            using var command = connection.CreateCommand();
            command.CommandText = sql.EndsWith(';') ? sql : sql + ';';
            command.CommandType = CommandType.Text;
            if (args?.Count > 0)
            {
                foreach (var kv in args)
                {
                    if (kv.Value is IDbDataParameter dataParameter)
                    {
                        dataParameter.ParameterName = kv.Key;
                        command.Parameters.Add(dataParameter);
                    }
                    else
                    {
                        var parameter = command.CreateParameter();
                        parameter.Value = kv.Value;
                        parameter.ParameterName = kv.Key;
                        command.Parameters.Add(parameter);
                    }
                }
            }
            return command.ExecuteNonQuery();
        }
        public static void ExecuteNonQuery(this IDbConnection connection, params string[] sqls)
        {
            if (sqls != null)
            {
                Array.ForEach(sqls, p =>
                     ExecuteNonQuery(connection, p)
                );
            }
        }

        public static void ExecuteSqlScriptFile(this IDbConnection connection, string sqlFile, string sqlSplit = "", Action<(int StartLine, int LineCount, string Sql, int Result)> callback = null)
        {
            using var reader = new StreamReader(sqlFile);
            ExecuteSqlScript(connection, reader, sqlSplit, callback);
        }
        public static void ExecuteSqlScript(this IDbConnection connection, TextReader textReader, string sqlSplit = "", Action<(int StartLine, int LineCount, string Sql, int Result)> callback = null)
        {
            var lines = new List<string>();
            var seq = CollectionExtensions.NewSequence(1);
            var (currentLine, segmentStartLine) = (1, 1);
            while (true)
            {
                string line = textReader.ReadLine()?.Trim();
                if (line == null)
                {
                    ExecuteCurrentStringBuilder();
                    break;
                }
                currentLine = seq();
                if (line.Equals(sqlSplit ?? string.Empty, StringComparison.InvariantCultureIgnoreCase))
                {
                    ExecuteCurrentStringBuilder();
                }
                else
                {
                    lines.Add(line);
                }
            }


            void ExecuteCurrentStringBuilder()
            {
                var sql = string.Join('\n', lines);
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    var result = connection.ExecuteNonQuery(sql);
                    callback?.Invoke((segmentStartLine, lines.Count, sql, result));
                }
                lines.Clear();
                segmentStartLine = currentLine + 1;

            }

        }
        public static void ExecuteSqlScript(this IDbConnection connection, string sqlScripts, string sqlSplit = "", Action<(int StartLine, int LineCount, string Sql, int Result)> callback = null)
        {
            using var reader = new StringReader(sqlScripts ?? string.Empty);
            ExecuteSqlScript(connection, reader, sqlSplit, callback);
        }
        public static bool ExecuteExists(this IDbConnection connection, string sql, Func<DataRow, bool> condition = null)
        {
            var table = connection.ExecuteReaderAsTable(sql);
            if (condition != null)
            {
                return table.AsEnumerable().Any(condition);
            }
            return table.AsEnumerable().Any();
        }
        public static IDataReader ExecuteReader(this IDbConnection connection, string sql)
        {
            TryOpen(connection);
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            return command.ExecuteReader();

        }
        public static DataTable ExecuteReaderAsTable(this IDbConnection connection, string sql)
        {
            using var reader = ExecuteReader(connection, sql);
            return reader.LoadDataTable();
        }
        public static List<Dictionary<string, object>> ExecuteReaderAsDataList(this IDbConnection connection, string sql)
        {
            using var reader = ExecuteReader(connection, sql);
            return reader.LoadData().ToList();
        }
        public static List<Tuple<T1, T2>> ExecuteReaderAsList<T1, T2>(this IDbConnection connection, string sql)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p => new Tuple<T1, T2>(p.FieldValue<T1>(0), p.FieldValue<T2>(1))).ToList();
        }
        public static List<Tuple<T1, T2, T3>> ExecuteReaderAsList<T1, T2, T3>(this IDbConnection connection, string sql)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p => new Tuple<T1, T2, T3>(p.FieldValue<T1>(0), p.FieldValue<T2>(1), p.FieldValue<T3>(2))).ToList();
        }
        public static List<Tuple<T1, T2, T3, T4>> ExecuteReaderAsList<T1, T2, T3, T4>(this IDbConnection connection, string sql)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p =>
                new Tuple<T1, T2, T3, T4>(p.FieldValue<T1>(0), p.FieldValue<T2>(1), p.FieldValue<T3>(2), p.FieldValue<T4>(3))).ToList();
        }
        public static List<Tuple<T1, T2, T3, T4, T5>> ExecuteReaderAsList<T1, T2, T3, T4, T5>(this IDbConnection connection, string sql)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p =>
                new Tuple<T1, T2, T3, T4, T5>(p.FieldValue<T1>(0), p.FieldValue<T2>(1), p.FieldValue<T3>(2), p.FieldValue<T4>(3), p.FieldValue<T5>(4))).ToList();
        }
        public static List<Tuple<T1, T2, T3, T4, T5, T6>> ExecuteReaderAsList<T1, T2, T3, T4, T5, T6>(this IDbConnection connection, string sql)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p =>
                new Tuple<T1, T2, T3, T4, T5, T6>(p.FieldValue<T1>(0), p.FieldValue<T2>(1), p.FieldValue<T3>(2), p.FieldValue<T4>(3), p.FieldValue<T5>(4), p.FieldValue<T6>(5))).ToList();
        }
        public static List<T> ExecuteReaderAsList<T>(this IDbConnection connection, string sql, int columnIndex = 0)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p => p.FieldValue<T>(columnIndex)).ToList();
        }
        public static List<T> ExecuteReaderAsList<T>(this IDbConnection connection, string sql, string columnName)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p => p.FieldValue<T>(columnName)).ToList();
        }

        public static void TryOpen(this IDbConnection connection)
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
        }
        private static DataTable LoadDataTable(this IDataReader reader)
        {
            //Create datatable to hold schema and data seperately
            //Get schema of our actual table
            DataTable schema = reader.GetSchemaTable();
            DataTable table = new DataTable();
            if (schema == null)
            {
                //This is our datatable filled with data
                return table;
            }
            for (int i = 0; i < schema.Rows.Count; i++)
            {
                //Create new column for each row in schema table
                //Set properties that are causing errors and add it to our datatable
                //Rows in schema table are filled with information of columns in our actual table
                DataColumn Col = new DataColumn(schema.Rows[i]["ColumnName"].ToString(), (Type)schema.Rows[i]["DataType"]);
                Col.AllowDBNull = true;
                Col.Unique = false;
                Col.AutoIncrement = false;
                table.Columns.Add(Col);
            }

            while (reader.Read())
            {
                //Read data and fill it to our datatable
                DataRow Row = table.NewRow();
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    Row[i] = reader[i];
                }
                table.Rows.Add(Row);
            }
            //This is our datatable filled with data
            return table;
        }
        private static IEnumerable<Dictionary<string, object>> LoadData(this IDataReader reader)
        {
            using var schema = reader.GetSchemaTable();
            if (schema != null)
            {
                var allColumns = schema.Rows.OfType<DataRow>().Select(p => p.FieldValue<string>("ColumnName")).ToList();
                while (reader.Read())
                {
                    yield return allColumns.ToDictionary(p => p, p => reader[p].ToValue<object>());
                }
            }
        }
    }
}
