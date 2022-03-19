using System;
using ChangeDB.Migration;

namespace ChangeDB.Agent.MySql
{
    public class MySqlDataTypeMapper : IDataTypeMapper
    {
        public static readonly IDataTypeMapper Default = new MySqlDataTypeMapper();
        public DataTypeDescriptor ToCommonDatabaseType(string storeType)
        {
            /*
TypeName            |ProviderDbType      |CreateFormat        |DataType            
//BOOL                |-1                  |BOOL                |System.Boolean      
//TINYINT             |1                   |TINYINT             |System.SByte        
//TINYINT             |501                 |TINYINT UNSIGNED    |System.Byte         
//SMALLINT            |2                   |SMALLINT            |System.Int16        
//SMALLINT            |502                 |SMALLINT UNSIGNED   |System.UInt16       
//INT                 |3                   |INT                 |System.Int32        
//INT                 |503                 |INT UNSIGNED        |System.UInt32       
//MEDIUMINT           |9                   |MEDIUMINT           |System.Int32        
//MEDIUMINT           |509                 |MEDIUMINT UNSIGNED  |System.UInt32       
//BIGINT              |8                   |BIGINT              |System.Int64        
//BIGINT              |508                 |BIGINT UNSIGNED     |System.UInt64       
//BIT                 |16                  |BIT                 |System.UInt64       
//DECIMAL             |246                 |DECIMAL({0},{1})    |System.Decimal      
//DOUBLE              |5                   |DOUBLE              |System.Double       
//FLOAT               |4                   |FLOAT               |System.Single       
//VARCHAR             |253                 |VARCHAR({0})        |System.String       
//CHAR                |254                 |CHAR({0})           |System.String       
//TINYTEXT            |749                 |TINYTEXT            |System.String       
//TEXT                |752                 |TEXT                |System.String       
//MEDIUMTEXT          |750                 |MEDIUMTEXT          |System.String       
//LONGTEXT            |751                 |LONGTEXT            |System.String       
ENUM                |247                 |ENUM                |System.String       
SET                 |248                 |SET                 |System.String       
//JSON                |245                 |JSON                |System.String       
//BLOB                |252                 |BLOB                |System.Byte[]       
//BINARY              |600                 |BINARY({0})         |System.Byte[]       
//VARBINARY           |601                 |VARBINARY({0})      |System.Byte[]       
//TINYBLOB            |249                 |TINYBLOB            |System.Byte[]       
//MEDIUMBLOB          |250                 |MEDIUMBLOB          |System.Byte[]       
//LONGBLOB            |251                 |LONGBLOB            |System.Byte[]
       
GEOMETRY            |255                 |GEOMETRY            |System.Byte[]       
POINT               |255                 |POINT               |System.Byte[]       
LINESTRING          |255                 |LINESTRING          |System.Byte[]       
POLYGON             |255                 |POLYGON             |System.Byte[]       
MULTIPOINT          |255                 |MULTIPOINT          |System.Byte[]       
MULTILINESTRING     |255                 |MULTILINESTRING     |System.Byte[]       
MULTIPOLYGON        |255                 |MULTIPOLYGON        |System.Byte[]       
GEOMETRYCOLLECTION  |255                 |GEOMETRYCOLLECTION  |System.Byte[]       
GEOMCOLLECTION      |255                 |GEOMCOLLECTION      |System.Byte[]     
  
//DATETIME            |12                  |DATETIME            |System.DateTime     
//DATE                |10                  |DATE                |System.DateTime     
//TIME                |11                  |TIME                |System.TimeSpan     
//TIMESTAMP           |7                   |TIMESTAMP           |System.DateTime     
//YEAR                |13                  |YEAR                |System.Int32        
//GUID                |800                 |CHAR(36)            |System.Guid 
             */


            //link show all datatype required stroage  https://dev.mysql.com/doc/refman/5.7/en/storage-requirements.html
            _ = storeType ?? throw new ArgumentNullException(nameof(storeType));
            var (type, arg1, arg2) = ParseStoreType(storeType);
            var storeTypeUpper = storeType.ToUpperInvariant();
            return type.ToUpperInvariant() switch
            {
                // number
                "BOOL" => DataTypeDescriptor.Boolean(),
                "TINYINT" => arg1 == 1 ? DataTypeDescriptor.Boolean() : DataTypeDescriptor.TinyInt(),
                "TINYINT UNSIGNED" => DataTypeDescriptor.TinyInt(),// TODO handle overflow
                "SMALLINT" => DataTypeDescriptor.SmallInt(),
                "SMALLINT UNSIGNED" => DataTypeDescriptor.SmallInt(),// TODO handle overflow
                "MEDIUMINT" => DataTypeDescriptor.Int(),
                "MEDIUMINT UNSIGNED" => DataTypeDescriptor.Int(),
                "INT" => DataTypeDescriptor.Int(),
                "INT UNSIGNED" => DataTypeDescriptor.Int(), // TODO handle overflow
                "BIGINT" => DataTypeDescriptor.BigInt(),
                "BIGINT UNSIGNED" => DataTypeDescriptor.BigInt(),// TODO handle overflow
                "BIT" => arg1 == 1 ? DataTypeDescriptor.Boolean() : DataTypeDescriptor.BigInt(),
                "DECIMAL" => DataTypeDescriptor.Decimal(arg1 ?? 10, arg2 ?? 0),
                "FLOAT" => DataTypeDescriptor.Float(),
                "DOUBLE" => DataTypeDescriptor.Double(),

                // datetime
                "TIMESTAMP" => DataTypeDescriptor.DateTime(arg1 ?? 0),
                "DATETIME" => DataTypeDescriptor.DateTime(arg1 ?? 0),
                "DATE" => DataTypeDescriptor.Date(),
                "TIME" => DataTypeDescriptor.Time(arg1 ?? 0),
                "YEAR" => DataTypeDescriptor.Int(),

                // text
                "CHAR" => DataTypeDescriptor.NChar(arg1 ?? 1),
                "VARCHAR" => DataTypeDescriptor.NVarchar(arg1 ?? 1),
                "TINYTEXT" => DataTypeDescriptor.NText(),
                "MEDIUMTEXT" => DataTypeDescriptor.NText(),
                "TEXT" => DataTypeDescriptor.NText(),
                "LONGTEXT" => DataTypeDescriptor.NText(),
                "JSON" => DataTypeDescriptor.NText(),

                //binary
                "BINARY" => arg1 == 16 ? DataTypeDescriptor.Uuid() : DataTypeDescriptor.Binary(arg1 ?? 1),
                "VARBINARY" => DataTypeDescriptor.Varbinary(arg1 ?? 1),
                "TINYBLOB" => DataTypeDescriptor.Blob(),
                "MEDIUMBLOB" => DataTypeDescriptor.Blob(),
                "BLOB" => DataTypeDescriptor.Blob(),
                "LONGBLOB" => DataTypeDescriptor.Blob(),

                //"ENUM" => DataTypeDescriptor.Char(arg1 ?? 1),//https://dev.mysql.com/doc/refman/5.7/en/enum.html
                //"SET" => DataTypeDescriptor.Char(arg1 ?? 1),//https://dev.mysql.com/doc/refman/5.7/en/set.html
                // TODO support enum and set
                _ => throw new NotSupportedException($"the data type '{storeType}' not supported.")
            };
        }

        private static (string Type, int? Arg1, int? Arg2) ParseStoreType(string storeType)
        {
            var index1 = storeType.IndexOf('(');
            var index2 = storeType.IndexOf(')');
            if (index1 > 0 && index2 > 0)
            {
                var type = storeType[..index1] + storeType.Substring(index2 + 1);
                var index3 = storeType.IndexOf(',', index1);
                if (index3 > 0)
                {
                    return (type, int.Parse(storeType.Substring(index1 + 1, index3 - index1 - 1).Trim()),
                        int.Parse(storeType.Substring(index3 + 1, index2 - index3 - 1).Trim()));
                }
                else
                {
                    return (type, int.Parse(storeType.Substring(index1 + 1, index2 - index1 - 1).Trim()), null);
                }
            }

            return (storeType.ToLower(), null, null);
        }

        public string ToDatabaseStoreType(DataTypeDescriptor commonDataType)
        {
            return commonDataType.DbType switch
            {
                CommonDataType.Boolean => "tinyint(1)",
                CommonDataType.Varchar => CreateVarcharType(commonDataType.Arg1 ?? 1),
                CommonDataType.Char => $"CHAR({commonDataType.Arg1})",
                CommonDataType.NVarchar => CreateVarcharType(commonDataType.Arg1 ?? 1),
                CommonDataType.NChar => $"CHAR({commonDataType.Arg1})",
                CommonDataType.Float => "float",
                CommonDataType.Double => "double",
                CommonDataType.Binary => $"binary({commonDataType.Arg1})",
                CommonDataType.Int => "int",
                CommonDataType.SmallInt => "smallint",
                CommonDataType.BigInt => "bigint",
                CommonDataType.TinyInt => "tinyint",
                CommonDataType.Varbinary => CreateBinaryType(commonDataType.Arg1 ?? 1),
                CommonDataType.Decimal => $"decimal({commonDataType.Arg1},{commonDataType.Arg2})",
                CommonDataType.Date => "date",
                CommonDataType.Time => $"time({Math.Min(commonDataType?.Arg1 ?? 0, 6)})",
                CommonDataType.DateTime => $"datetime({Math.Min(commonDataType?.Arg1 ?? 0, 6)})",
                CommonDataType.Text => "longtext",
                CommonDataType.NText => "longtext",
                CommonDataType.Blob => "longblob",
                CommonDataType.Uuid => "binary(16)",
                CommonDataType.DateTimeOffset => $"datetime({Math.Min(commonDataType?.Arg1 ?? 0, 6)})",
                _ => throw new NotSupportedException()
            };

            string CreateVarcharType(int length)
            {
                // TODO need handle max length
                return $"varchar({length})";
            }

            string CreateBinaryType(int length)
            {
                // TODO need handle max length
                return $"varbinary({length})";
            }


        }
    }
}
