using System;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal;
using Microsoft.Extensions.Logging;

namespace ChangeDB.Agent.SqlServer
{
    public class SqlServerUtils
    {
        public static string IdentityName(string objectName)
        {
            _ = objectName ?? throw new ArgumentNullException(nameof(objectName));

            return $"[{objectName}]";

        }
        public static string IdentityName(string schema, string objectName)
        {
            if (string.IsNullOrEmpty(schema))
            {
                return IdentityName(objectName);
            }
            else
            {
                return $"{IdentityName(schema)}.{IdentityName(objectName)}";
            }
        }
        public static string IdentityName(string schema, string objectName, string subObjectName)
        {
            if (string.IsNullOrEmpty(schema))
            {
                return IdentityName(objectName, subObjectName);
            }
            else
            {
                return $"{IdentityName(schema)}.{IdentityName(objectName)}.{IdentityName(subObjectName)}";
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]
        public static DatabaseDescriptor GetDataBaseDescriptorByEFCore(DbConnection dbConnection)
        {
            var loggerFactory = new LoggerFactory();
            var databaseModelFactory = new SqlServerDatabaseModelFactory(
                new DiagnosticsLogger<DbLoggerCategory.Scaffolding>(
                    loggerFactory,
                    new LoggingOptions(),
                    new DiagnosticListener("sqlserver"),
                    new SqlServerLoggingDefinitions(),
                    new NullDbContextLogger()));
            var options = new DatabaseModelFactoryOptions();
            var model = databaseModelFactory.Create(dbConnection, options);
            return FromDatabaseModel(model, dbConnection);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]
        private static DatabaseDescriptor FromDatabaseModel(DatabaseModel databaseModel, DbConnection dbConnection)
        {
            // exclude views
            var allTables = dbConnection.ExecuteReaderAsList<string, string>("select table_schema ,table_name from information_schema.tables t where t.table_type ='BASE TABLE'");

            var allDefaultValues = dbConnection.ExecuteReaderAsList<string, string, string, string>(
                "SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS");

            return new DatabaseDescriptor
            {
                //Collation = databaseModel.Collation,
                //DefaultSchema = databaseModel.DefaultSchema,
                Tables = databaseModel.Tables.Where(IsTable).Select(FromTableModel).ToList(),
                Sequences = databaseModel.Sequences.Select(FromSequenceModel).ToList(),
            };
            bool IsTable(DatabaseTable table)
            {
                return allTables.Any(t => t.Item1 == table.Schema && t.Item2 == table.Name);
            }
            TableDescriptor FromTableModel(DatabaseTable table)
            {
                return new TableDescriptor
                {
                    Schema = table.Schema,
                    Comment = table.Comment,
                    Name = table.Name,
                    Columns = table.Columns.Select(FromColumnModel).ToList(),
                    PrimaryKey = FromPrimaryKeyModel(table.PrimaryKey),
                    Uniques = table.UniqueConstraints.Select(FromUniqueModel).ToList(),
                    Indexes = table.Indexes.Select(FromIndexModel).ToList(),
                    ForeignKeys = table.ForeignKeys.Select(FromForeignKeyModel).ToList()
                };
            }
            PrimaryKeyDescriptor FromPrimaryKeyModel(DatabasePrimaryKey primaryKey)
            {
                if (primaryKey == null) return null;
                return new PrimaryKeyDescriptor
                {
                    Name = primaryKey.Name,
                    Columns = primaryKey.Columns.Select(p => p.Name).ToList()
                };
            }
            UniqueDescriptor FromUniqueModel(DatabaseUniqueConstraint uniqueConstraint)
            {
                return new UniqueDescriptor
                {
                    Name = uniqueConstraint.Name,
                    Columns = uniqueConstraint.Columns.Select(p => p.Name).ToList()
                };
            }
            IndexDescriptor FromIndexModel(DatabaseIndex index)
            {
                return new IndexDescriptor
                {
                    Name = index.Name,
                    IsUnique = index.IsUnique,
                    Filter = index.Filter,
                    Columns = index.Columns.Select(p => p.Name).ToList(),

                };
            }
            //https://github.com/dotnet/efcore/blob/252ece7a6bdf14139d90525a4dd0099616a82b4c/src/EFCore.SqlServer/Scaffolding/Internal/SqlServerDatabaseModelFactory.cs
            ColumnDescriptor FromColumnModel(DatabaseColumn column)
            {
                var baseColumnDesc = new ColumnDescriptor
                {
                    Collation = column.Collation,
                    Comment = column.Comment,
                    ComputedColumnSql = column.ComputedColumnSql,
                    DefaultValueSql = column.DefaultValueSql,
                    Name = column.Name,
                    IsStored = column.IsStored ?? false,
                    IsNullable = column.IsNullable,
                    StoreType = column.StoreType,
                };

                if (column.ValueGenerated == ValueGenerated.OnAdd)
                {
                    var tableFullName = IdentityName(column.Table.Schema, column.Table.Name);
                    baseColumnDesc.IsIdentity = true;
                    baseColumnDesc.IdentityInfo = new IdentityDescriptor
                    {
                        IsCyclic = false,
                        StartValue = dbConnection.ExecuteScalar<long>($"SELECT IDENT_SEED('{tableFullName}')"),
                        IncrementBy = dbConnection.ExecuteScalar<int>($"SELECT IDENT_INCR('{tableFullName}')"),
                        CurrentValue = dbConnection.ExecuteScalar<long?>($"SELECT IDENT_CURRENT('{tableFullName}')"),
                    };
                    if (baseColumnDesc.IdentityInfo.StartValue == baseColumnDesc.IdentityInfo.CurrentValue && !dbConnection.ExecuteExists($"select top 1 * from {tableFullName}"))
                    {
                        baseColumnDesc.IdentityInfo.CurrentValue = null;
                    }
                    
                }
                else
                {
                    // reassign defaultValue Sql, because efcore will filter clr default
                    // https://github.com/dotnet/efcore/blob/252ece7a6bdf14139d90525a4dd0099616a82b4c/src/EFCore.SqlServer/Scaffolding/Internal/SqlServerDatabaseModelFactory.cs#L783
                    baseColumnDesc.DefaultValueSql = allDefaultValues.Where(p =>
                            p.Item1 == column.Table.Schema && p.Item2 == column.Table.Name && p.Item3 == column.Name)
                        .Select(p => p.Item4).SingleOrDefault();
                }

                return baseColumnDesc;
            }

            ForeignKeyDescriptor FromForeignKeyModel(DatabaseForeignKey foreignKey)
            {
                return new ForeignKeyDescriptor
                {
                    Name = foreignKey.Name,
                    PrincipalTable = foreignKey.PrincipalTable.Name,
                    PrincipalSchema = foreignKey.PrincipalTable.Schema,
                    OnDelete = (ReferentialAction?)(int?)foreignKey.OnDelete,
                    ColumnNames = foreignKey.Columns.Select(p => p.Name).ToList(),
                    PrincipalNames = foreignKey.PrincipalColumns.Select(p => p.Name).ToList()
                };
            }
            SequenceDescriptor FromSequenceModel(DatabaseSequence sequence)
            {
                return new SequenceDescriptor
                {
                    IncrementBy = sequence.IncrementBy,
                    IsCyclic = sequence.IsCyclic,
                    MaxValue = sequence.MaxValue,
                    MinValue = sequence.MinValue,
                    Name = sequence.Name,
                    Schema = sequence.Schema,
                    StartValue = sequence.StartValue,
                    StoreType = sequence.StoreType,
                };
            }

        }
    }
}
