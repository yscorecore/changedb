using System;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using ChangeDB.Migration;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerDatabaseTypeMapper : IDatabaseTypeMapper
    {
        public static IDatabaseTypeMapper Default = new SqlServerDatabaseTypeMapper();

        /*
smallint	16	5	smallint		System.Int16
int	8	10	int		System.Int32
real	13	7	real		System.Single
float	6	53	float({0})	number of bits used to store the mantissa	System.Double
money	9	19	money		System.Decimal
smallmoney	17	10	smallmoney		System.Decimal
bit	2	1	bit		System.Boolean
tinyint	20	3	tinyint		System.Byte
bigint	0	19	bigint		System.Int64
timestamp	19	8	timestamp		System.Byte[]
binary	1	8000	binary({0})	length	System.Byte[]
image	7	2147483647	image		System.Byte[]
text	18	2147483647	text		System.String
ntext	11	1073741823	ntext		System.String
decimal	5	38	decimal({0}, {1})	precision,scale	System.Decimal
numeric	5	38	numeric({0}, {1})	precision,scale	System.Decimal
datetime	4	23	datetime		System.DateTime
smalldatetime	15	16	smalldatetime		System.DateTime
sql_variant	23		sql_variant		System.Object
xml	25	2147483647	xml		System.String
varchar	22	2147483647	varchar({0})	max length	System.String
char	3	2147483647	char({0})	length	System.String
nchar	10	1073741823	nchar({0})	length	System.String
nvarchar	12	1073741823	nvarchar({0})	max length	System.String
varbinary	21	1073741823	varbinary({0})	max length	System.Byte[]
uniqueidentifier	14	16	uniqueidentifier		System.Guid
date	31	3	date		System.DateTime
time	32	5	time({0})	scale	System.TimeSpan
datetime2	33	8	datetime2({0})	scale	System.DateTime
datetimeoffset	34	10	datetimeoffset({0})	scale	System.DateTimeOffset

         */
        public DatabaseTypeDescriptor ToCommonDatabaseType(string storeType)
        {
            _ = storeType ?? throw new ArgumentNullException(nameof(storeType));
            var match =  Regex.Match(storeType.ToLowerInvariant(), @"^(?<name>\w+)(\((?<arg1>\w+)(,\s*(?<arg2>\w+))?\))?$");
            var type = match.Groups["name"].Value;
            string arg1 =match.Groups["arg1"].Value; 
            string arg2 = match.Groups["arg2"].Value ;
            bool isMax = arg1 == "max";
            int length = (isMax|| string.IsNullOrEmpty(arg1)) ? default : int.Parse(arg1);
            int scale = string.IsNullOrEmpty(arg2)?default: int.Parse(arg2);
            return type switch
            {
                "bit" => DatabaseTypeDescriptor.Boolean(),
                "tinyint" => DatabaseTypeDescriptor.TinyInt(),
                "smallint" => DatabaseTypeDescriptor.SmallInt(),
                "int" => DatabaseTypeDescriptor.Int(),
                "bigint" => DatabaseTypeDescriptor.BigInt(),
                "decimal" => DatabaseTypeDescriptor.Decimal( length, scale),
                "numeric" => DatabaseTypeDescriptor.Decimal(length, scale),
                "rowversion" => DatabaseTypeDescriptor.Binary( 8),
                "uniqueidentifier" => DatabaseTypeDescriptor.Uuid(),
                "real" => DatabaseTypeDescriptor.Float(),
                "text" => DatabaseTypeDescriptor.Text(),
                "ntext" => DatabaseTypeDescriptor.NText(),
                "image" => DatabaseTypeDescriptor.Blob(),
                "float" => DatabaseTypeDescriptor.Double(length),
                "smallmoney" => DatabaseTypeDescriptor.Decimal(10, 4),
                "money" => DatabaseTypeDescriptor.Decimal(19, 4),
                "binary" => DatabaseTypeDescriptor.Binary(length),
                "varbinary" =>isMax?DatabaseTypeDescriptor.Blob(): DatabaseTypeDescriptor.Varbinary(length),
                "char" =>  DatabaseTypeDescriptor.Char(length),
                "nchar" =>  DatabaseTypeDescriptor.NChar(length),
                "varchar" => isMax?DatabaseTypeDescriptor.Text():  DatabaseTypeDescriptor.Varchar( length),
                "nvarchar" =>isMax?DatabaseTypeDescriptor.NText(): DatabaseTypeDescriptor.NVarchar( length),
                "xml" => DatabaseTypeDescriptor.NText(),
                "date" => DatabaseTypeDescriptor.Date(),
                "time" => DatabaseTypeDescriptor.Time(length),
                "datetime" => DatabaseTypeDescriptor.DateTime(0),
                "smalldatetime" => DatabaseTypeDescriptor.DateTime(0),
                "datetime2" => DatabaseTypeDescriptor.DateTime(length),
                "datetimeoffset" => DatabaseTypeDescriptor.DateTimeOffset(length),
                _ => throw new System.NotSupportedException($"not support dbtype {storeType}.")
            };
        }

        public string ToDatabaseStoreType(DatabaseTypeDescriptor commonDatabaseType)
        {
            return commonDatabaseType.DbType switch
            {
                CommonDatabaseType.Boolean=>"bit",
                CommonDatabaseType.TinyInt=>"tinyint",
                CommonDatabaseType.SmallInt=>"smallint",
                CommonDatabaseType.Int=>"int",
                CommonDatabaseType.BigInt=>"bigint",
                CommonDatabaseType.Decimal=>$"decimal({commonDatabaseType.Arg1},{commonDatabaseType.Arg2})",
                CommonDatabaseType.Float=>"real",
                CommonDatabaseType.Double=>$"float({commonDatabaseType.Arg1})",
                CommonDatabaseType.Binary=>$"binary({commonDatabaseType.Arg1})",
                CommonDatabaseType.Varbinary=>$"varbinary({commonDatabaseType.Arg1})",
                CommonDatabaseType.Blob=>"image",
                CommonDatabaseType.Uuid=>"uniqueidentifier",
                CommonDatabaseType.Char=>$"char({commonDatabaseType.Arg1})",
                CommonDatabaseType.NChar=>$"nchar({commonDatabaseType.Arg1})",
                CommonDatabaseType.Varchar=>$"varchar({commonDatabaseType.Arg1})",
                CommonDatabaseType.NVarchar=>$"nvarchar({commonDatabaseType.Arg1})",
                CommonDatabaseType.Text=>$"text",
                CommonDatabaseType.NText=>$"ntext",
                CommonDatabaseType.Time=>$"time({commonDatabaseType.Arg1})",
                CommonDatabaseType.Date=>$"date",
                CommonDatabaseType.DateTime=>$"datetime2({commonDatabaseType.Arg1})",
                CommonDatabaseType.DateTimeOffset=>$"datetimeoffset({commonDatabaseType.Arg1})",
                _ => throw new NotSupportedException($"can not convert from common database type {commonDatabaseType}")
            };
        }
    }
}
