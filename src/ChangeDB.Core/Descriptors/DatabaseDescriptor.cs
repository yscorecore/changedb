﻿using System.Collections.Generic;
using System.Collections.Immutable;

namespace ChangeDB
{
    public class DatabaseDescriptor
    {
        public string Name { get; set; }

        public List<SchemaDescriptor> Schemas { get; set; }
        public List<TableDescriptor> Tables { get; set; }
    }
}
