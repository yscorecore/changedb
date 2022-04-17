docker-compose -f mysql.yml -p changedb_mysql up -d
[Environment]::SetEnvironmentVariable('TESTDB_MYSQL', 'Server=127.0.0.1;Port=3306;Uid=root;Pwd=password;', 'Machine')