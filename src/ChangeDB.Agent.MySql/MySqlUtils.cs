using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql.Design.Internal;
using Pomelo.EntityFrameworkCore.MySql.Diagnostics.Internal;
using Pomelo.EntityFrameworkCore.MySql.Internal;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace ChangeDB.Agent.MySql
{
    public static class MySqlUtils
    {
        private static string[] NumberTypes = new[]
             {"bit", "int", "tinyint", "smallint", "int", "bigint", "decimal", "double", "float"};
        public static string IdentityName(string objectName) => $"`{objectName}`";
        public static string IdentityName(string schema, string objectName) => $"{IdentityName(objectName)}";
        public static string IdentityName(TableDescriptor table) => IdentityName(table.Schema, table.Name);

        public static DatabaseDescriptor GetDataBaseDescriptorByEFCore(DbConnection dbConnection)
        {
            var databaseModelFactory = GetModelFactory();
            var model = databaseModelFactory.Create(dbConnection, new DatabaseModelFactoryOptions());
            return FromDatabaseModel(model, dbConnection);
        }

        private static IDatabaseModelFactory GetModelFactory()
        {
            var sc = new ServiceCollection();
            var designerService = new MySqlDesignTimeServices();
            sc.AddEntityFrameworkMySql();
            designerService.ConfigureDesignTimeServices(sc);
            var provider = sc.BuildServiceProvider();
            return provider.GetRequiredService<IDatabaseModelFactory>();
        }

        private static DatabaseDescriptor FromDatabaseModel(DatabaseModel databaseModel, DbConnection dbConnection)
        {
            var allDefaultValues = dbConnection.ExecuteReaderAsList<string, string, string, string, string>(
                @"SELECT TABLE_NAME ,COLUMN_NAME , COLUMN_DEFAULT, COLUMN_TYPE, EXTRA
from information_schema.`COLUMNS` c 
where c.TABLE_SCHEMA =database() and COLUMN_DEFAULT is not null;");

            var allUniqueConstraints = GetAllUniqueConstraint();
            //var globalIdentityInfo = GetGlobalIdentityInfo();
            return new DatabaseDescriptor
            {
                //Collation = databaseModel.Collation,
                //DefaultSchema = databaseModel.DefaultSchema,
                Tables = databaseModel.Tables.Select(FromTableModel).ToList(),
                Sequences = databaseModel.Sequences.Select(FromSequenceModel).ToList(),
            };

            IEnumerable<(string TableName, string UniqueName, string ColumnName)> GetAllUniqueConstraint()
            {
                var sql = @"select a.TABLE_NAME ,a.CONSTRAINT_NAME ,b.COLUMN_NAME
from information_schema.TABLE_CONSTRAINTS a
left join information_schema.KEY_COLUMN_USAGE b  on a.CONSTRAINT_CATALOG =b.CONSTRAINT_CATALOG and a.CONSTRAINT_SCHEMA =b.CONSTRAINT_SCHEMA and a.CONSTRAINT_NAME =b.CONSTRAINT_NAME 
where  a.constraint_type = 'UNIQUE' and a.CONSTRAINT_SCHEMA  = DATABASE ()
order by a.TABLE_NAME , a.CONSTRAINT_NAME,b.ORDINAL_POSITION";
                return dbConnection.ExecuteReaderAsList<string, string, string>(sql).Select(p => (p.Item1, p.Item2, p.Item3)).ToList();
            }

            // (int Increment, int Offset) GetGlobalIdentityInfo()
            // {
            //    var vars= dbConnection.ExecuteReaderAsList<string, int>("show variables like 'auto_increment_%';");
            //    var increment = vars.FirstOrDefault(p => p.Item1 == "auto_increment_increment");
            //    var offset = vars.FirstOrDefault(p => p.Item1 == "auto_increment_offset");
            //    return (increment?.Item2 ?? 1, offset?.Item2 ?? 1);
            // }


            TableDescriptor FromTableModel(DatabaseTable table)
            {
                return new TableDescriptor
                {
                    Schema = table.Schema,
                    Comment = table.Comment,
                    Name = table.Name,
                    Columns = table.Columns.Select(FromColumnModel).ToList(),
                    PrimaryKey = FromPrimaryKeyModel(table.PrimaryKey),
                    Uniques = BuildUniqueList(table),
                    Indexes = BuildIndexesList(table),
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

            List<UniqueDescriptor> BuildUniqueList(DatabaseTable table)
            {
                var result = table.UniqueConstraints.Select(FromUniqueModel).ToList();
                if (result.Count == 0)
                {
                    // mysql database model not contain unique constraint
                    var tableUniques = allUniqueConstraints.Where(p => p.TableName == table.Name)
                        .GroupBy(p => p.UniqueName)
                        .Select(p => new UniqueDescriptor
                        {
                            Name = p.Key,
                            Columns = p.Select(t => t.ColumnName).ToList()
                        });
                    result.AddRange(tableUniques);
                }

                return result;

            }

            List<IndexDescriptor> BuildIndexesList(DatabaseTable table)
            {
                // in mysql unique index == unique constraint
                // foreign key always create a index, so need ignore foreign key index
                var foreignKeys = table.ForeignKeys.Select(p => p.Name).ToHashSet();
                return table.Indexes.Where(p => !p.IsUnique && !foreignKeys.Contains(p.Name)).Select(FromIndexModel).ToList();
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
            //
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
                        StartValue = 1,
                        IncrementBy = 1,
                        // in mysql, identity column must be a key, so use max no performance issues
                        CurrentValue = dbConnection.ExecuteScalar<long?>($"select max({IdentityName(column.Name)}) from {tableFullName}")
                    };
                    if (baseColumnDesc.IdentityInfo.IncrementBy == 0)
                    {
                        baseColumnDesc.IdentityInfo.IncrementBy = 1;
                    }
                }
                else
                {
                    // reassign defaultValue Sql, because efcore will filter clr default
                    // https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/blob/1974429313f541eedfbe9d5ad748cd44f44989fa/src/EFCore.MySql/Scaffolding/Internal/MySqlDatabaseModelFactory.cs#L371
                    baseColumnDesc.DefaultValueSql = allDefaultValues.Where(p =>
                            p.Item1 == column.Table.Name && p.Item2 == column.Name)
                        .Select(p => NormalizeDefaultValue(p.Item3, p.Item4, p.Item5)).SingleOrDefault();
                }


                return baseColumnDesc;
            }

            static string NormalizeDefaultValue(string defaultValue, string dataType, string extra)
            {
                if (defaultValue == null) return null;
                var isFunction = extra == "DEFAULT_GENERATED";
                if (isFunction)
                {
                    return HasWrapBrackets() ? defaultValue : $"({defaultValue})";
                }

                return IsNumberType() ? defaultValue : $"'{defaultValue}'";

                bool HasWrapBrackets() => defaultValue.StartsWith("(") && defaultValue.EndsWith(")");

                bool IsNumberType() => NumberTypes.Any(dataType.StartsWith);
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
