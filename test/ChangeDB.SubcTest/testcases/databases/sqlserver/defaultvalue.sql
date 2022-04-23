    create table test_default_value(id int primary key, val varchar(10) default 'abc');
    GO
    insert into test_default_value(id, val) values(1, null);
    insert into test_default_value(id, val) values(2, 'aaa');
    insert into test_default_value(id) values(3);
    GO         
