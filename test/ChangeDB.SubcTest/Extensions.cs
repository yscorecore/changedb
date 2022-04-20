using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace ChangeDB
{
    public static class Extensions
    {

        public static List<ColumnInfo> ExecuteAsSchema(this DbConnection connection, string tableName)
        {
            using var reader = connection.ExecuteReader($"select * from {tableName}");
            var querySchema = reader.GetSchemaTable();
            return querySchema.Rows.OfType<DataRow>()
                .Select(p => new ColumnInfo
                {
                    Name = p.Field<string>("ColumnName"),
                    Type = p.Field<Type>("DataType")
                })
                .ToList();
        }


    }

    public class ColumnInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
    }

}
