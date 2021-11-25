using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;

namespace ChangeDB
{
    public static class ConnectionExtensions
    {
        public static object ExecuteScalar(this IDbConnection connection, string sql,IDictionary<string, object> args=null)
        {
            AlterOpen(connection);
            var command = connection.CreateCommand();
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
        public static T ExecuteScalar<T>(this IDbConnection connection, string sql,IDictionary<string, object> args=null)
        {
             return connection.ExecuteScalar(sql,args).ToValue<T>();
        }
        public static int ExecuteNonQuery(this IDbConnection connection, string sql)
        {
            return connection.ExecuteNonQuery(sql, default(IDictionary<string, object>));
        }
        public static int ExecuteNonQuery(this IDbConnection connection, string sql, IDictionary<string, object> args)
        {
            _ = sql ?? throw new ArgumentNullException(nameof(sql));
            AlterOpen(connection);
            var command = connection.CreateCommand();
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
        public static void ExecuteSqlFiles(this IDbConnection connection, IEnumerable<string> sqlFiles, string sqlSplit)
        {
            sqlFiles.Each(file => connection.ExecuteSqlFile(file, sqlSplit));
        }
        public static void ExecuteSqlFile(this IDbConnection connection, string sqlFile, string sqlSplit)
        {
            var allLines = File.ReadAllLines(sqlFile);
            var stringBuilder = new StringBuilder();
            var allSqls = new List<string>();
            allLines.Each((line) =>
            {
                if (sqlSplit.Equals(line.Trim(), StringComparison.InvariantCultureIgnoreCase))
                {
                    allSqls.Add(stringBuilder.ToString().Trim());
                    stringBuilder.Clear();
                }
                else
                {
                    stringBuilder.AppendLine(line);
                }
            });
            allSqls.Append(stringBuilder.ToString());
            connection.ExecuteNonQuery(allSqls.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray());
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

        public static DataTable ExecuteReaderAsTable(this IDbConnection connection, string sql)
        {
            AlterOpen(connection);
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            using var reader = command.ExecuteReader();
            var table = new DataTable();
            table.Load(reader);
            return table;
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
        public static List<Tuple<T1, T2, T3, T4,T5>> ExecuteReaderAsList<T1, T2, T3, T4,T5>(this IDbConnection connection, string sql)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p =>
                new Tuple<T1, T2, T3, T4,T5>(p.FieldValue<T1>(0), p.FieldValue<T2>(1), p.FieldValue<T3>(2), p.FieldValue<T4>(3),p.FieldValue<T5>(4))).ToList();
        }
        public static List<Tuple<T1, T2, T3, T4,T5,T6>> ExecuteReaderAsList<T1, T2, T3, T4,T5,T6>(this IDbConnection connection, string sql)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p =>
                new Tuple<T1, T2, T3, T4,T5,T6>(p.FieldValue<T1>(0), p.FieldValue<T2>(1), p.FieldValue<T3>(2), p.FieldValue<T4>(3),p.FieldValue<T5>(4),p.FieldValue<T6>(5))).ToList();
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

        public static void AlterOpen(IDbConnection connection)
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
        }
    }
}
