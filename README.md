# <p align="center">CHANGEDB</p>
<p align="center">ChangeDB is a multi-database cli convert tool, it's all about making database migration much more easier. </p>

<p align="center">
<img alt="build" src="https://github.com/yscorecore/changedb/workflows/build/badge.svg" />
<a href="https://img.shields.io/nuget/v/ChangeDB.ConsoleApp">
    <img alt="Nuget" src="https://img.shields.io/nuget/v/ChangeDB.ConsoleApp" />
</a>
<a href="https://github.com/yscorecore/changedb/blob/master/LICENSE">
    <img alt="GitHub" src="https://img.shields.io/github/license/yscorecore/changedb" />
</a>
<a href="https://codecov.io/gh/yscorecore/changedb">
    <img alt="codecov" src="https://codecov.io/gh/yscorecore/changedb/branch/master/graph/badge.svg" />
</a>
</p>

## 📕 How to use


1. Install Dotnet (net5/net6). `ChangeDB` is supported by dotnet sdk, please check the link [Here](https://dotnet.microsoft.com/download/dotnet) to setup your personal dotnet sdk.
1. Install ChangeDB tool. you can follow the command below to setup tool quite easily.
   ```shell
   dotnet tool install ChangeDB.ConsoleApp -g
   ``` 
1. Database converting. you can use `changedb migration` command convert database. Like the example command below, you need to provide source database type, source database connection string, target database type and target database connection string to establish migrate task. and for target database, `changedb` tool has the ability to create a new target database even if not exists. so you don't create a empty database everytime first.
    ```shell
   changedb migration {source-database-type} "{source-connection-string}" {target-database-type} "{target-connection-string}" 
   ```
1. Dump database to sql scripts, `changedb` tool also has the ability to generate sql scripts. you can use `changedb dumpsql` command，Like the example example below to create sql scripts.
   ```shell
   changedb dumpsql {source-database-type} "{source-connection-string}" {target-database-type} "{output-file}" 
   ```

## 📦 Supported Database 

 - **Sql Server**
 - **Postgres**
 - **Mysql**
 - **Sql Server Compact** (only supported in windows)

## 🏗️Architecture diagram
![image](./res/ArchitectureDiagram.png)

## 🔗Database Formate Connection String 

|Database | Format connection string  | Extend usages | 
|---|---|---|
|Sql Serve| `Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;` |[Link](https://www.connectionstrings.com/microsoft-data-sqlclient/) |
|Postgres| `Server=127.0.0.1;Port=5432;Database=myDataBase;User Id=myUsername;Password=myPassword;` |[Link](https://www.connectionstrings.com/npgsql/) |
|Mysql| `Server=myServerAddress;Database=myDataBase;Uid=myUsername;Pwd=myPassword;` |[Link](https://www.connectionstrings.com/mysql-connector-net-mysqlconnection/) |
|Sql Server Compact| `Data Source=MyData.sdf;Persist Security Info=False;` |[Link](https://www.connectionstrings.com/sqlserverce-sqlceconnection/)|
    
## 🧩Database Object Supported
| Category | Object | Sql Server| Postgres| Mysql | Sql Server Compact |
|---|---|---|---|---|---|
| Table|`identity`|✔️|✔️|✔️|✔️|
| Table|`index`|✔️|✔️|✔️|✔️|
| Table|`unique`|✔️|✔️|✔️|✔️|
| Table|`default constant value`|✔️|✔️|✔️|✔️|
| Table|`default function value`|✔️(`newid`,`getdate`)|✔️(`gen_random_uuid`,`now`)|✔️(`uuid`,`now`)|✔️(`newid`,`getdate`)|
| Table|`foreign key`|✔️|✔️|✔️|✔️|
| Table|`unique index`|✔️|✔️|✔️|✔️|



