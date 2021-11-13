using System;
using System.Collections.Generic;
using System.Linq;

namespace ChangeDB
{
    public static class DatabaseDescriptorExtensions
    {
        public static IEnumerable<string> GetAllSchemas(this DatabaseDescriptor databaseDescriptor)
        {
            _ = databaseDescriptor ?? throw new ArgumentNullException(nameof(databaseDescriptor));
            var result = Enumerable.Empty<string>();
            if (databaseDescriptor.Tables != null)
            {
                result = result.Union(databaseDescriptor.Tables.Select(p => p.Schema));
            }
            if (databaseDescriptor.Sequences != null)
            {
                result= result.Union(databaseDescriptor.Tables.Select(p => p.Schema));
            }
            return result.Where(p => !string.IsNullOrEmpty(p)).Distinct();
        }
    }
}
