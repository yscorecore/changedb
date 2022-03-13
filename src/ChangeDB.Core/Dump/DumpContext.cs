using System.IO;
using ChangeDB.Migration;

namespace ChangeDB.Dump
{
    public record DumpContext : MigrationContext
    {
        public TextWriter Writer { get; set; }
    }
}
