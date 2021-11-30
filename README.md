# CHANGEDB

ChangeDB is a database migration cli tool, made database migration easier. 

![build](https://github.com/yscorecore/RepoTemplate/workflows/build/badge.svg)
[![codecov](https://codecov.io/gh/yscorecore/RepoTemplate/branch/master/graph/badge.svg)](https://codecov.io/gh/yscorecore/RepoTemplate) 
[![Nuget](https://img.shields.io/nuget/v/YS.Hello)](https://nuget.org/packages/YS.Hello/) 
[![GitHub](https://img.shields.io/github/license/yscorecore/RepoTemplate)](https://github.com/yscorecore/RepoTemplate/blob/master/LICENSE)


## How to use

1. Install the dotnet (net5/net6), `ChangeDB` need dotnet runtime support, so you need install the dotnet first, you can install the dotnet runtime in [Here](https://dotnet.microsoft.com/download/dotnet) .
1. Install the ChangeDB tool, you can use the follow command to install the tool.
   ```shell
   dotnet tool install ChangeDB.ConsoleApp -g
   ``` 
1. Migration you database, after the last step, you can use `changedb` command now. In the follow command, you just need provide source database type, source database connection string, target database type, target database connection string. For the target database, you don't create a empty database first, just give a connection string, `changedb` will create a new target database if not exists.

    ```shell
   changedb migration {source-database-type} "{source-connection-string}" {target-database-type} "{target-connection-string}" 
   ```

## Support database types

 - sqlserver
 - postgres
 - sqlce (only can run in windows)

## Database connection string format

|database | connection string format | more usages | 
|---|---|---|
|sqlserver| `Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;` |[Link](https://www.connectionstrings.com/microsoft-data-sqlclient/) |
|postgres| `Server=127.0.0.1;Port=5432;Database=myDataBase;User Id=myUsername;Password=myPassword;` |[Link](https://www.connectionstrings.com/npgsql/) |
|sqlce| `Data Source=MyData.sdf;Persist Security Info=False;` |[Link](https://www.connectionstrings.com/sqlserverce-sqlceconnection/)|
    
