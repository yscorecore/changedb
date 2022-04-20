using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Design.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace ChangeDB.Agent.SqlServer
{
    internal class SqlServerUtils
    {
        public static string IdentityName(string objectName) => $"[{objectName}]";

        public static string IdentityName(string schema, string objectName) => string.IsNullOrEmpty(schema) ? IdentityName(objectName) : $"{IdentityName(schema)}.{IdentityName(objectName)}";

        public static string IdentityName(TableDescriptor table) => IdentityName(table.Schema, table.Name);

        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]
        [Obsolete]
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
            var designerService = new SqlServerDesignTimeServices();
            sc.AddEntityFrameworkSqlServer();
            designerService.ConfigureDesignTimeServices(sc);
            var provider = sc.BuildServiceProvider();
            return provider.GetRequiredService<IDatabaseModelFactory>();
        }

        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Pending>")]
        [Obsolete]
        private static DatabaseDescriptor FromDatabaseModel(DatabaseModel databaseModel, DbConnection dbConnection)
        {
            var dataTypeMapper = SqlServerDataTypeMapper.Default;
            var sqlExpressionTranslator = SqlServerSqlExpressionTranslator.Default;
            // exclude views
            var allTables = dbConnection.ExecuteReaderAsList<string, string>("select table_schema ,table_name from information_schema.tables t where t.table_type ='BASE TABLE'");

            var allDefaultValues = dbConnection.ExecuteReaderAsList<string, string, string, string>(
                "SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS");

            var addIdentityInfos = GetAllIdentityInfos();

            return new DatabaseDescriptor
            {
                //Collation = databaseModel.Collation,
                //DefaultSchema = databaseModel.DefaultSchema,
                Tables = databaseModel.Tables.Where(IsTable).Select(FromTableModel).ToList(),
                Sequences = databaseModel.Sequences.Select(FromSequenceModel).ToList(),
            };

            List<Tuple<string, string, int, int, int?, int>> GetAllIdentityInfos()
            {
                var sqlLines = databaseModel.Tables.Where(p => p.Columns.Any(c => c.ValueGenerated == ValueGenerated.OnAdd))
                     .Select(t => $"SELECT '{t.Schema}' as s, '{t.Name}' as t, IDENT_SEED('{SqlServerUtils.IdentityName(t.Schema, t.Name)}') as seed ,IDENT_INCR('{SqlServerUtils.IdentityName(t.Schema, t.Name)}') as incr,IDENT_CURRENT('{SqlServerUtils.IdentityName(t.Schema, t.Name)}') as currentValue,(select top 1 1 from {SqlServerUtils.IdentityName(t.Schema, t.Name)}) as hasrow");
                var allSql = string.Join("\nunion all\n", sqlLines);
                if (string.IsNullOrEmpty(allSql))
                {
                    return new List<Tuple<string, string, int, int, int?, int>>();
                }
                return dbConnection.ExecuteReaderAsList<string, string, int, int, int?, int>(allSql);
            }

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
                var defaultValue = allDefaultValues.Where(p =>
                          p.Item1 == column.Table.Schema && p.Item2 == column.Table.Name && p.Item3 == column.Name)
                    .Select(p => p.Item4).SingleOrDefault();
                var baseColumnDesc = new ColumnDescriptor
                {
                    Collation = column.Collation,
                    Comment = column.Comment,
                    ComputedColumnSql = column.ComputedColumnSql,
                    //DefaultValueSql = defaultValue,
                    Name = column.Name,
                    IsStored = column.IsStored ?? false,
                    IsNullable = column.IsNullable,
                    //StoreType = column.StoreType,
                    DataType = dataTypeMapper.ToCommonDatabaseType(column.StoreType),
                    DefaultValue = sqlExpressionTranslator.ToCommonSqlExpression(defaultValue, column.StoreType, dbConnection)
                };
                baseColumnDesc.SetOriginStoreType(column.StoreType);
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    baseColumnDesc.SetOriginDefaultValue(defaultValue);
                }
                if (column.ValueGenerated == ValueGenerated.OnAdd)
                {
                    baseColumnDesc.IsIdentity = true;
                    var identityInfo = addIdentityInfos.Single(p => p.Item1 == column.Table.Schema && p.Item2 == column.Table.Name);
                    baseColumnDesc.IdentityInfo = new IdentityDescriptor
                    {
                        IsCyclic = false,
                        StartValue = identityInfo.Item3,
                        IncrementBy = identityInfo.Item4,
                        CurrentValue = identityInfo.Item6 == 1 ? identityInfo.Item5 : null,
                    };
                    if (baseColumnDesc.IdentityInfo.IncrementBy == 0)
                    {
                        baseColumnDesc.IdentityInfo.IncrementBy = 1;
                    }

                    baseColumnDesc.DefaultValue = null;
                    baseColumnDesc.SetOriginDefaultValue(null);
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
