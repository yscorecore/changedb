using System.Collections.Generic;

namespace ChangeDB
{
    public class ForeignKeyDescriptor
    {

        public string Name { get; set; }

        public List<string> ColumnNames { get; set; }

        public string PrincipalSchema { get; set; }

        public string PrincipalTable { get; set; }

        public List<string> PrincipalNames { get; set; }

        public ReferentialAction? OnDelete { get; set; }
    }
}
