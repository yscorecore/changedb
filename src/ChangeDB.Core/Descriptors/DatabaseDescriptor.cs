using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace ChangeDB
{
    public class DatabaseDescriptor
    {
        //public string Collation { get; set; }
        //public string DefaultSchema { get; set; }
        public List<TableDescriptor> Tables { get; set; } = new List<TableDescriptor>();

        public List<SequenceDescriptor> Sequences { get; set; } = new List<SequenceDescriptor>();


    }
}
