using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Agent.Postgres
{
    static class PostgresUtils
    {
        public static string IdentityName(string objectName, NameStyle nameStyle = NameStyle.Same)
        {
            _ = objectName ?? throw new ArgumentNullException(nameof(objectName));
            return nameStyle switch
            {
                NameStyle.Lower => $"\"{objectName.ToLower()}\"",
                NameStyle.Upper => $"\"{objectName.ToUpper()}\"",
                _ => $"\"{objectName}\""
            };
        }
        public static string IdentityName(string schema, string objectName, NameStyle nameStyle = NameStyle.Same)
        {
            if (string.IsNullOrEmpty(schema))
            {
                return IdentityName(objectName, nameStyle);
            }
            else
            {
                return $"{IdentityName(schema, nameStyle)}.{IdentityName(objectName, nameStyle)}";
            }
        }


    }
}
