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
            var type = storeType;
            int? arg1, arg2;
            arg1 = arg2 = 0;
            return type switch
            {
                "bit" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Boolean),
                "tinyint" => DatabaseTypeDescriptor.Create(CommonDatabaseType.TinyInt),
                "smallint" => DatabaseTypeDescriptor.Create(CommonDatabaseType.SmallInt),
                "int" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Int),
                "bigint" => DatabaseTypeDescriptor.Create(CommonDatabaseType.BigInt),
                "decimal" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Decimal, arg1, arg2),
                "numeric" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Decimal, arg1, arg2),
                "timestamp" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Binary, 8),
                "uniqueidentifier" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Uuid),
                "real" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Float),
                "text" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Text),
                "ntext" => DatabaseTypeDescriptor.Create(CommonDatabaseType.NText),
                "image" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Blob),
                "float" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Double, arg1),
                "smallmoney" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Decimal, 10, 4),
                "money" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Decimal, 19, 4),
                "binary" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Binary, arg1),
                "varbinary" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Varbinary, arg1),
                "char" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Char, arg1),
                "nchar" => DatabaseTypeDescriptor.Create(CommonDatabaseType.NChar, arg1),
                "varchar" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Varchar, arg1),
                "nvarchar" => DatabaseTypeDescriptor.Create(CommonDatabaseType.NVarchar, arg1),
                "xml" => DatabaseTypeDescriptor.Create(CommonDatabaseType.NText),
                "date" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Date),
                "time" => DatabaseTypeDescriptor.Create(CommonDatabaseType.Time, arg1),
                "datetime" => DatabaseTypeDescriptor.Create(CommonDatabaseType.DateTime),
                "smalldatetime" => DatabaseTypeDescriptor.Create(CommonDatabaseType.DateTime),
                "datetime2" => DatabaseTypeDescriptor.Create(CommonDatabaseType.DateTime, arg1),
                "datetimeoffset" => DatabaseTypeDescriptor.Create(CommonDatabaseType.DateTimeOffset, arg1),
                _ => throw new System.NotSupportedException($"not support dbtype {storeType}")
            };
        }

        public string ToDatabaseStoreType(DatabaseTypeDescriptor commonDatabaseType)
        {
            throw new System.NotImplementedException();
        }
    }
}
