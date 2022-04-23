﻿    create table test_bit(val bit);
    GO
    insert into test_bit(val) values(1);
    GO         
        
    create table test_tinyint(val tinyint);
    GO
    insert into test_tinyint(val) values(123);
    GO        
  
    create table test_smallint(val smallint);
    GO
    insert into test_smallint(val) values(123);
    GO
    
    create table test_int(val int);
    GO
    insert into test_int(val) values(123);
    GO
    
    create table test_bigint(val bigint);
    GO
    insert into test_bigint(val) values(123);
    GO
    
    create table test_uuid(val uniqueidentifier);
    GO
    insert into test_uuid(val) values('AEBD4A2C-D6A7-4F9A-889C-CF1AA8145D78');
    GO
    
    create table test_char(val char, val2 char(5), val3 nchar, val4 nchar(5));
    GO
    insert into test_char(val, val2, val3, val4) values('a', 'b', 'c', N'的的的');
    GO
    
    create table test_varchar(val varchar, val2 varchar(5), val3 nvarchar, val4 nvarchar(5));
    GO
    insert into test_varchar(val, val2, val3, val4) values('a', 'b', 'c', N'的的的');
    GO
    
    create table test_text(id int, val text, val2 ntext);
    GO
    insert into test_text(id, val, val2) values(1, 'a', N'的的的');
    GO
    
    create table test_xml(id int, val xml);
    GO
    insert into test_xml(id, val) values(1, '<a>abc</a>');
    GO
    
    create table test_binary(val binary,val2 binary(5));
    GO
    insert into test_binary(val, val2) values(0x01,0x02);
    GO
    
    create table test_varbinary(val varbinary,val2 varbinary(5));
    GO
    insert into test_varbinary(val, val2) values(0x01,0x02);
    GO
    
    
    create table test_timestamp(id int,val timestamp);
    GO
    insert into test_timestamp(id) values(1);
    GO
    
    create table test_rowversion(id int,val rowversion);
    GO
    insert into test_rowversion(id) values(1);
    GO
    
    create table test_image(id int,val image);
    GO
    insert into test_image(id, val) values(1,0x0102);
    GO
    
    create table test_money(id int identity(1,1) primary key, val money);
    GO
    insert into test_money(val) values(1);
    insert into test_money(val) values(1.2345);
    insert into test_money(val) values(1.23456);
    insert into test_money(val) values(123456789012345.67891);
     insert into test_money(val) values(-123456789012345.67891);
    GO
    
    create table test_smallmoney(id int identity(1,1) primary key,val money);
    GO
    insert into test_smallmoney(val) values(1);
    insert into test_smallmoney(val) values(1.2345);
    insert into test_smallmoney(val) values(1.23456);
    insert into test_smallmoney(val) values(123456789.01234);
    insert into test_smallmoney(val) values(-123456789.01234);
    GO
    
    create table test_defaultdecimal(id int identity(1,1) primary key, val decimal);
    GO
    insert into test_defaultdecimal(val) values(1);
    insert into test_defaultdecimal(val) values(123456789012345678);
    insert into test_defaultdecimal(val) values(-123456789012345678);
    GO
    
    create table test_decimal(id int identity(1,1) primary key, val decimal(38,20));
    GO
    insert into test_decimal(val) values(1);
    insert into test_decimal(val) values(12345678.12345678901234567890);
    insert into test_decimal(val) values(-12345678.12345678901234567890);
    GO
    
    create table test_real(id int identity(1,1) primary key, val real);
    GO
    insert into test_real(val) values(1);
    insert into test_real(val) values(123.4567);
    insert into test_real(val) values(-123.4567);
    GO
    
    create table test_float(id int identity(1,1) primary key, val float);
    GO
    insert into test_float(val) values(1);
    insert into test_float(val) values(12345.0123456789012345);
    insert into test_float(val) values(-12345.0123456789012345);
    GO
    
    create table test_float23(id int identity(1,1) primary key, val float(23));
    GO
    insert into test_float23(val) values(1);
    insert into test_float23(val) values(12345.01);
    insert into test_float23(val) values(-12345.01);
    GO
    
    create table test_smalldatetime(id int identity(1,1) primary key, val smalldatetime);
    GO
    -- https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/smalldatetime-transact-sql?view=sql-server-ver15
    -- Accuracy	One minute
    insert into test_smalldatetime(val) values('2021-12-16 18:26:00');
    insert into test_smalldatetime(val) values('1900-01-01 01:02:00');
    insert into test_smalldatetime(val) values('2079-06-06 01:02:00');
    GO
    
    create table test_datetime(id int identity(1,1) primary key, val datetime);
    GO
    -- https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/datetime-transact-sql?view=sql-server-ver15
    -- datetime values are rounded to increments of .000, .003, or .007 seconds, as shown in the following table.
    insert into test_datetime(val) values('2021-12-16 18:26:23.450');
    insert into test_datetime(val) values('1753-01-01 18:26:23.453');
    insert into test_datetime(val) values('9999-12-31 18:26:23.457');
    GO
    
    create table test_datetime2(id int identity(1,1) primary key, val0 datetime2, val1 datetime2(0), val2 datetime2(1), val3 datetime2(3), val4 datetime2(6),val5 datetime2(7));
    GO
    -- https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/datetime2-transact-sql?view=sql-server-ver15
    insert into test_datetime2(val0,val1,val2,val3,val4,val5) values('2021-12-16T18:26:23.1234567','2021-12-16T18:26:23.1234567','2021-12-16T18:26:23.1234567','2021-12-16T18:26:23.1234567','2021-12-16T18:26:23.1234567','2021-12-16T18:26:23.1234567');
    insert into test_datetime2(val0,val1,val2,val3,val4,val5) values('0001-01-01T00:00:00.1234567','0001-01-01T00:00:00.1234567','0001-01-01T00:00:00.1234567','0001-01-01T00:00:00.1234567','0001-01-01T00:00:00.1234567','0001-01-01T00:00:00.1234567');
    insert into test_datetime2(val0,val1,val2,val3,val4,val5) values('9999-12-31T18:26:23.1234567','9999-12-31T18:26:23.1234567','9999-12-31T18:26:23.1234567','9999-12-31T18:26:23.1234567','9999-12-31T18:26:23.1234567','9999-12-31T18:26:23.1234567');
    GO
    
    create table test_date(id int identity(1,1) primary key, val date);
    GO
    -- https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/date-transact-sql?view=sql-server-ver15
    insert into test_date(val) values('2021-12-16');
    insert into test_date(val) values('0001-01-01');
    insert into test_date(val) values('9999-12-31');
    GO
    
    create table test_time(id int identity(1,1) primary key, val1 time, val2 time(0), val3 time(1), val4 time(3), val5 time(6), val6 time(7));
    GO
    -- https://docs.microsoft.com/zh-cn/sql/t-sql/time-types/date-transact-sql?view=sql-server-ver15
    insert into test_time(val1,val2,val3,val4,val5,val6) values('12:34:56.7890123','12:34:56.7890123','12:34:56.7890123','12:34:56.7890123','12:34:56.7890123','12:34:56.7890123');
    insert into test_time(val1,val2,val3,val4,val5,val6) values('00:00:00.0000000','00:00:00.0000000','00:00:00.0000000','00:00:00.0000000','00:00:00.0000000','00:00:00.0000000');
    insert into test_time(val1,val2,val3,val4,val5,val6) values('23:59:59.9999999','23:59:59.9999999','23:59:59.9999999','23:59:59.9999999','23:59:59.9999999','23:59:59.9999999');
    GO
    
    create table test_datetimeoffset(id int identity(1,1) primary key, val0 datetimeoffset, val1 datetimeoffset(0), val2 datetimeoffset(1), val3 datetimeoffset(3), val4 datetimeoffset(6),val5 datetimeoffset(7));
    GO
    -- https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/datetimeoffset-transact-sql?view=sql-server-ver15
    insert into test_datetimeoffset(val0,val1,val2,val3,val4,val5) values('2021-12-16 18:26:23.1234567 +08:00','2021-12-16 18:26:23.1234567 +08:00','2021-12-16 18:26:23.1234567 +08:00','2021-12-16 18:26:23.1234567 +08:00','2021-12-16 18:26:23.1234567 +08:00','2021-12-16 18:26:23.1234567 +08:00');
    insert into test_datetimeoffset(val0,val1,val2,val3,val4,val5) values('2021-12-16 18:26:23.1234567 -14:00','2021-12-16 18:26:23.1234567 +14:00','2021-12-16 18:26:23.1234567 -08:00','2021-12-16 18:26:23.1234567 +08:00','2021-12-16 18:26:23.1234567 -04:30','2021-12-16 18:26:23.1234567 +08:00');
    insert into test_datetimeoffset(val0,val1,val2,val3,val4,val5) values('0001-01-02 00:00:00.1234567 +08:00','0001-01-02 00:00:00.1234567 +08:00','0001-01-02 00:00:00.1234567 +08:00','0001-01-02 00:00:00.1234567 +08:00','0001-01-02 00:00:00.1234567 +08:00','0001-01-02 00:00:00.1234567 +08:00');
    insert into test_datetimeoffset(val0,val1,val2,val3,val4,val5) values('9999-12-31 18:26:23.1234567 +08:00','9999-12-31 18:26:23.1234567 +08:00','9999-12-31 18:26:23.1234567 +08:00','9999-12-31 18:26:23.1234567 +08:00','9999-12-31 18:26:23.1234567 +08:00','9999-12-31 18:26:23.1234567 +08:00');
    GO