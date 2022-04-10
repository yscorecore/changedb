docker-compose -f sqlserver.yml -p changedb_sqlserver up -d
[Environment]::SetEnvironmentVariable('SQLSERVER__CONN', 'Server=127.0.0.1,1433;User Id=sa;Password=myStrong(!)Password;', 'Machine')