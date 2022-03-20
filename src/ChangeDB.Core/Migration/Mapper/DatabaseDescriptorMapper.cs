using System.Collections.Generic;
using ChangeDB.Mapper;

namespace ChangeDB.Migration.Mapper
{
    public class DatabaseDescriptorMapper : BaseMapper<DatabaseDescriptor>
    {
        public List<TableDescriptorMapper> ColumnMappers { get; set; }
    }
}
