using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB
{
    public class DBTypeDescriptor
    {
        public DBType DbType { get; set; }
        public int? Length { get; set; }
        public int? Accuracy { get; set; }
        public override string ToString()
        {
            if (Length is null)
            {
                return Type2String(DbType);
            }
            else
            {
                if (Accuracy is null)
                {
                    return $"{Type2String(DbType)}({Length})";
                }
                else
                {
                    return $"{Type2String(DbType)}({Length},{Accuracy})";
                }
            }
        }
        private string Type2String(DBType dbType) => $"{dbType.ToString().ToUpperInvariant().Replace("__"," ")}";
    }

    public enum DBType
    {
        Character__Varying,
        Int,
    }
}
