using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB
{
    internal static class Extensions
    {
        static Dictionary<Type, int> TypeSize = new Dictionary<Type, int>
        {
            [typeof(int)] = sizeof(int),
            [typeof(uint)] = sizeof(uint),
            [typeof(char)] = sizeof(char),
            [typeof(byte)] = sizeof(byte),
            [typeof(sbyte)] = sizeof(sbyte),
            [typeof(short)] = sizeof(short),
            [typeof(long)] = sizeof(long),
            [typeof(ulong)] = sizeof(ulong),
            [typeof(float)] = sizeof(float),
            [typeof(double)] = sizeof(double),
            [typeof(decimal)] = sizeof(decimal),
            [typeof(Guid)] = 16,
            [typeof(DateTime)] = 8,
            [typeof(DateTimeOffset)] = 8,
            [typeof(bool)] = sizeof(bool),
        };
        public static int TotalSize(this DataTable dataTable)
        {
            return dataTable.Columns.OfType<DataColumn>().Sum(p => SumColumnSize(p));
        }
        private static int SumColumnSize(DataColumn column)
        {
            var rowCount = column.Table.Rows.Count;
            if (column.DataType == typeof(byte[]))
            {
                var itemType = column.DataType.GetElementType();
                return column.Table.Rows.OfType<DataRow>().Sum(p => p.Field<byte[]>(column)?.Length ?? 0);
            }
            else if (column.DataType == typeof(string))
            {
                return column.Table.Rows.OfType<DataRow>().Sum(p => p.Field<string>(column)?.Length ?? 0);
            }
            else if (column.DataType.IsValueType)
            {
                var itemType = Nullable.GetUnderlyingType(column.DataType) ?? column.DataType;
                return GetTypeSize(itemType) * rowCount;
            }
            else
            {
                return GetTypeSize(column.DataType) * rowCount;
            }
        }

        static int GetTypeSize(Type type)
        {
            if (TypeSize.TryGetValue(type, out var size))
            {
                return size;
            }
            return 4;
        }
    }
}
