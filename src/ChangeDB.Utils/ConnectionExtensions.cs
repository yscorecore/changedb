using System;
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
            _ = sql ?? throw new ArgumentNullException(nameof(sql));
            AlterOpen(connection);
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
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
        public static void AlterOpen(IDbConnection connection)
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
        }
    }
}
