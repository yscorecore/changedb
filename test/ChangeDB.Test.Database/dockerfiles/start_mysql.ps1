docker-compose -f mysql.yml -p changedb_mysql up -d
[Environment]::SetEnvironmentVariable('MYSQL__CONN', 'Server=127.0.0.1;Port=3306;Uid=root;Pwd=password;', 'Machine')