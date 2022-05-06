using System.IO;
using ChangeDB.Migration;

namespace ChangeDB.Dump
{
    public record DumpSetting : MigrationSetting
    {
        public TextWriter Writer { get; set; }
    }
}
