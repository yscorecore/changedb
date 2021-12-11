using System.Data.Common;

namespace ChangeDB.Agent.SqlCe.EFCore.SqlServerCompact
{
    public static class SqlDataReaderExtension
    {
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public static T GetValueOrDefault<T>(this DbDataReader reader, string name)
        {
            var idx = reader.GetOrdinal(name);
            return reader.IsDBNull(idx)
                ? default(T)
                : reader.GetFieldValue<T>(idx);
        }
    }
}
