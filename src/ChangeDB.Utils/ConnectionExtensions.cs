﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace ChangeDB
{
    public static class ConnectionExtensions
    {
        public static T ExecuteScalar<T>(this IDbConnection connection, string sql)
        {
            AlterOpen(connection);
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            return (T)Convert.ChangeType(command.ExecuteScalar(), typeof(T));
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

        public static void AlterOpen(IDbConnection connection)
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
        }
    }
}