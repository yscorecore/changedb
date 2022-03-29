using System.Collections.Generic;
using ChangeDB.Mapper;

namespace ChangeDB.Migration.Mapper
{
    public record TableDescriptorMapper : BaseMapper<TableDescriptor>
    {
        public List<ColumnDescriptorMapper> ColumnMappers { get; } = new();
    }
}
