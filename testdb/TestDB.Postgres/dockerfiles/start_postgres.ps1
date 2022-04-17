docker-compose -f postgres.yml -p changedb_postgres up -d
[Environment]::SetEnvironmentVariable('TESTDB_POSTGRES', 'Server=127.0.0.1;Port=5432;User Id=postgres;Password=mypassword;', 'Machine')