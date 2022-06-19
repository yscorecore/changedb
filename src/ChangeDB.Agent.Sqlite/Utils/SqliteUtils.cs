using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ChangeDB.Descriptors;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Sqlite.Design.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace ChangeDB.Agent.Sqlite
{
    internal class SqliteUtils
    {
        public static string IdentityName(string objectName) => $"\"{objectName}\"";

        public static string IdentityName(TableDescriptor table) => IdentityName(table.Name);

        public static DatabaseDescriptor GetDataBaseDescriptorByEFCore(DbConnection dbConnection)
        {
            var databaseModelFactory = GetModelFactory();
            var model = databaseModelFactory.Create(dbConnection, new DatabaseModelFactoryOptions());
            return FromDatabaseModel(model, dbConnection);
        }

        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
        private static IDatabaseModelFactory GetModelFactory()
        {
            var sc = new ServiceCollection();
            var designerService = new SqliteDesignTimeServices();
            sc.AddEntityFrameworkSqlite();
            designerService.ConfigureDesignTimeServices(sc);
            var provider = sc.BuildServiceProvider();
            return provider.GetRequiredService<IDatabaseModelFactory>();
        }

        private static DatabaseDescriptor FromDatabaseModel(DatabaseModel databaseModel, DbConnection conn)
        {
            return new DatabaseDescriptor
            {
                Tables = GetTables()
            };
            List<TableDescriptor> GetTables()
            {
                var tables = new List<TableDescriptor>();
                foreach (var table in databaseModel.Tables)
                {
                    tables.Add(new TableDescriptor
                    {
                        Name = table.Name,
                        Columns = table.Columns.Select(c =>
                        {
                            var identity = GetIdentity(table, c);
                            var columnDesc = new ColumnDescriptor
                            {
                                Name = c.Name,
                                Collation = c.Collation,
                                Comment = c.Comment,
                                DataType = SqliteDataTypeMapper.Default.ToCommonDatabaseType(c.StoreType),
                                IsNullable = c.IsNullable,
                                IdentityInfo = identity,
                                IsIdentity = identity is not null,
                                DefaultValue = SqliteSqlExpressionTranslator.Default.ToCommonSqlExpression(c.DefaultValueSql, c.StoreType, conn)
                            };
                            columnDesc.SetOriginStoreType(c.StoreType);
                            columnDesc.SetOriginDefaultValue(c.DefaultValueSql);
                            return columnDesc;
                        }).ToList(),
                        PrimaryKey = GetPrimaryKey(table.PrimaryKey),
                        Indexes = table.Indexes.Select(i => new IndexDescriptor
                        {
                            Name = i.Name,
                            IsUnique = i.IsUnique,
                            Filter = i.Filter,
                            Columns = i.Columns.Select(c => c.Name).ToList()
                        }).ToList(),
                        ForeignKeys = table.ForeignKeys.Select(f => new ForeignKeyDescriptor
                        {
                            Name = f.Name,
                            ColumnNames = f.Columns.Select(c => c.Name).ToList(),
                            PrincipalNames = f.PrincipalColumns.Select(c => c.Name).ToList(),
                            PrincipalSchema = f.PrincipalTable.Schema,
                            PrincipalTable = f.PrincipalTable.Name,
                            OnDelete = (ReferentialAction?)f.OnDelete
                        }).ToList(),
                        Uniques = table.UniqueConstraints.Select(u => new UniqueDescriptor
                        {
                            Name = u.Name,
                            Columns = u.Columns.Select(c => c.Name).ToList()
                        }).ToList()
                    });
                }
                return tables;
            }

            IdentityDescriptor GetIdentity(DatabaseTable table, DatabaseColumn column)
            {
                if (table.PrimaryKey != null && table.PrimaryKey.Columns.Count == 1
                    && table.PrimaryKey.Columns[0] == column
                    && column.ValueGenerated == ValueGenerated.OnAdd
                    && "INTEGER".Equals(column.StoreType, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new IdentityDescriptor
                    {
                        StartValue = 1,
                        IncrementBy = 1,
                        CurrentValue = GetSequenceCurrentValue(table.Name),
                    };
                }
                return default;
            }

            PrimaryKeyDescriptor GetPrimaryKey(DatabasePrimaryKey pk)
            {
                if (pk == null)
                {
                    return null;
                }
                return new PrimaryKeyDescriptor
                {
                    Name = pk.Name,
                    Columns = pk.Columns.Select(c => c.Name).ToList()
                };
            }

            long GetSequenceCurrentValue(string table)
            {
                const string sql = "SELECT seq FROM sqlite_sequence where name = @table;";
                return conn.ExecuteScalar<long>(sql, new Dictionary<string, object>
                {
                    ["table"] = table
                });
            }
        }
    }
}
