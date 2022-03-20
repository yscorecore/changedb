using System.Collections.Generic;
using ChangeDB.Mapper;

namespace ChangeDB.Migration.Mapper
{
    public class TableDescriptorMapper : BaseMapper<TableDescriptor>
    {
        public List<ColumnDescriptorMapper> ColumnMappers { get; set; }
    }
}
