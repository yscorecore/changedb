using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using ChangeDB.Agent.SqlCe.EFCore.SqlServerCompact;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace ChangeDB.Agent.SqlCe
{
    internal class SqlCeUtils
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

        public static DatabaseDescriptor GetDataBaseDescriptorByEFCore(DbConnection dbConnection)
        {

            var databaseModelFactory = new SqlCeDatabaseModelFactory();
            var options = new DatabaseModelFactoryOptions();
            var model = databaseModelFactory.Create(dbConnection, options);
            return FromDatabaseModel(model, dbConnection);
        }

        private static DatabaseDescriptor FromDatabaseModel(DatabaseModel databaseModel, DbConnection dbConnection)
        {

            var allDefaultValues = dbConnection.ExecuteReaderAsList<string, string, string>(
                "SELECT TABLE_NAME,COLUMN_NAME,COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS");

            var addIdentityInfos = GetAllIdentityInfos();

            return new DatabaseDescriptor
            {
                //Collation = databaseModel.Collation,
                //DefaultSchema = databaseModel.DefaultSchema,
                Tables = databaseModel.Tables.Select(FromTableModel).ToList(),
                Sequences = databaseModel.Sequences.Select(FromSequenceModel).ToList(),
            };

            List<Tuple<string, string, long, int, long>> GetAllIdentityInfos()
            {

                var sql = "SELECT TABLE_SCHEMA,TABLE_NAME,AUTOINC_SEED,AUTOINC_INCREMENT,AUTOINC_NEXT FROM INFORMATION_SCHEMA.COLUMNS WHERE AUTOINC_INCREMENT IS NOT NULL";
                return dbConnection.ExecuteReaderAsList<string, string, long, int, long>(sql);
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
                    var identityInfo = addIdentityInfos.Single(p => p.Item1 == column.Table.Schema && p.Item2 == column.Table.Name);
                    baseColumnDesc.IdentityInfo = new IdentityDescriptor
                    {
                        IsCyclic = false,
                        StartValue = identityInfo.Item3,
                        IncrementBy = identityInfo.Item4,
                        CurrentValue = identityInfo.Item5 == identityInfo.Item3 ? null : identityInfo.Item5 - identityInfo.Item4,
                    };
                    if (baseColumnDesc.IdentityInfo.IncrementBy == 0)
                    {
                        baseColumnDesc.IdentityInfo.IncrementBy = 1;
                    }
                }
                else
                {
                    // reassign defaultValue Sql, because efcore will filter clr default
                    baseColumnDesc.DefaultValueSql = allDefaultValues.Where(p =>
                             p.Item1 == column.Table.Name && p.Item2 == column.Name)
                        .Select(p => p.Item3).SingleOrDefault();
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
