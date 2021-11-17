using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace ChangeDB
{
    public static class ConnectionExtensions
    {
        public static object ExecuteScalar(this IDbConnection connection, string sql)
        {
            AlterOpen(connection);
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
           return command.ExecuteScalar();
        }

        public static T ExecuteScalar<T>(this IDbConnection connection, string sql)
        {
            var result = connection.ExecuteScalar(sql);
            if (result == null || result == DBNull.Value)
            {
                return default(T);
            }
            else
            {
                var valType = Nullable.GetUnderlyingType(typeof(T));
                return (T)Convert.ChangeType(result, valType??typeof(T));
            }
            
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
        public static void ExecuteSqlFiles(this IDbConnection connection, string[] sqlFiles, string sqlSplit)
        { 
            
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
        public static List<Tuple<T1,T2>> ExecuteReaderAsList<T1,T2>(this IDbConnection connection, string sql)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p => new Tuple<T1,T2>(p.Field<T1>(0),p.Field<T2>(1))).ToList();
        }
        public static List<Tuple<T1,T2,T3>> ExecuteReaderAsList<T1,T2,T3>(this IDbConnection connection, string sql)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p => new Tuple<T1,T2,T3>(p.Field<T1>(0),p.Field<T2>(1),p.Field<T3>(2))).ToList();
        }
        public static List<Tuple<T1,T2,T3,T4>> ExecuteReaderAsList<T1,T2,T3,T4>(this IDbConnection connection, string sql)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p => 
                new Tuple<T1,T2,T3,T4>(p.Field<T1>(0),p.Field<T2>(1),p.Field<T3>(2),p.Field<T4>(3))).ToList();
        }
        public static List<T> ExecuteReaderAsList<T>(this IDbConnection connection, string sql, int columnIndex = 0)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p => p.Field<T>(columnIndex)).ToList();
        }
        public static List<T> ExecuteReaderAsList<T>(this IDbConnection connection, string sql, string columnName)
        {
            var table = ExecuteReaderAsTable(connection, sql);
            return table.AsEnumerable().Select(p => p.Field<T>(columnName)).ToList();
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
