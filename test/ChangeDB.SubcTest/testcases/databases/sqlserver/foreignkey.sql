    create table tmain(id int primary key identity(1,1), val int);
    GO
    insert into tmain(val) values(1);
    insert into tmain(val) values(2);
    GO
    create table tsub(id int primary key identity(1,1), mainid int REFERENCES tmain(id));
    GO
    insert into tsub(mainid) values(1);
    insert into tsub(mainid) values(2);