using System;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using ChangeDB.Migration;
using ChangeDB.Migration.Mapper;

namespace ChangeDB.Default
{
    public class DefaultTableDataMapper : ITableDataMapper
    {
        public Task<DataTable> MapDataTable(DataTable dataTable, TableDescriptorMapper tableDescriptorMapper, MigrationSetting migrationSetting)
        {
            _ = tableDescriptorMapper ?? throw new ArgumentNullException(nameof(tableDescriptorMapper));
            if (CanUseOriginTable(tableDescriptorMapper))
            {
                return Task.FromResult(dataTable);
            }
            else
            {
                var resultDataTable = new DataTable();
                tableDescriptorMapper.ColumnMappers.ForEach(p => resultDataTable.Columns.Add(p.Target.Name, p.Target.DataType.GetClrType()));
                // copy rows
                foreach (DataRow row in dataTable.Rows)
                {
                    var newRow = resultDataTable.NewRow();
                    foreach (var columnMapper in tableDescriptorMapper.ColumnMappers)
                    {
                        var sourceItemValue = row[columnMapper.Source.Name];
                        var targetItemValue = GetTargetItemValue(sourceItemValue, columnMapper.Source.DataType, columnMapper.Target.DataType);
                        newRow[columnMapper.Target.Name] = targetItemValue;
                    }

                    resultDataTable.Rows.Add(newRow);
                }
                return Task.FromResult(resultDataTable);
            }
        }

        private bool CanUseOriginTable(TableDescriptorMapper mapper)
        {
            return mapper.ColumnMappers.All(p => p.Source.Name == p.Target.Name && p.Source.DataType.GetClrType() == p.Target.DataType.GetClrType());
        }

        private object GetTargetItemValue(object sourceValue, DataTypeDescriptor sourceType, DataTypeDescriptor targetType)
        {
            if (Convert.IsDBNull(sourceValue) || sourceValue == null)
            {
                return sourceValue;
            }

            if (sourceType?.GetClrType() == targetType?.GetClrType())
            {
                return sourceValue;
            }
            return ChangeValueType(sourceType, targetType.GetClrType());
        }

        private object ChangeValueType(object value, Type targetType)
        {
            if (targetType == typeof(string))
            {
                if (value is string)
                {
                    return value;
                }
                if (value is bool or byte or short or int or long or float or double or decimal)
                {
                    return value.ToString();
                }
                return JsonSerializer.Serialize(value);
            }
            if (targetType == typeof(bool) || targetType == typeof(int) || targetType == typeof(short) || targetType == typeof(byte) || targetType == typeof(double) || targetType == typeof(float) || targetType == typeof(decimal))
            {
                return Convert.ChangeType(value, targetType);
            }
            if (targetType == typeof(byte[]))
            {
                if (value is Guid guid)
                {
                    return guid.ToByteArray();
                }
            }
            return value;
        }
    }
}
