using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB
{
    public class TempFile : IDisposable,IAsyncDisposable
    {
        public TempFile()
        {
            this.FilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }
        public TempFile(string content) : this()
        {
            File.WriteAllText(this.FilePath, content ?? string.Empty);
        }
        public string FilePath { get; private set; }
        public void Dispose()
        {
            if (File.Exists(this.FilePath))
            {
                File.Delete(this.FilePath);
            }
        }
        public async ValueTask DisposeAsync()
        {
            await Task.Run(Dispose);
        }
    }
}
