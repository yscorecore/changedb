using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Extensions.Logging;
using Npgsql.EntityFrameworkCore.PostgreSQL.Diagnostics.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Scaffolding.Internal;

namespace ChangeDB.Agent.Postgres
{
    public static class PostgresUtils
    {
        public const string IdentityNumbersToCache = "Npgsql::IdentityNumbersToCache";
        public const string IdentityType = "Npgsql::IdentityType";
        public const string IDENTITY_BYDEFAULT = "BY DEFAULT";
        public const string IDENTITY_ALWAYS = "ALWAYS";

        public static string IdentityName(string objectName)
        {
            _ = objectName ?? throw new ArgumentNullException(nameof(objectName));

            return $"\"{objectName}\"";

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
            var databaseModelFactory = new NpgsqlDatabaseModelFactory(
                   new DiagnosticsLogger<DbLoggerCategory.Scaffolding>(
                       loggerFactory,
                       new LoggingOptions(),
                       new DiagnosticListener("postgres"),
                       new NpgsqlLoggingDefinitions(),
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
            //https://github.com/npgsql/efcore.pg/blob/176fae2e08087ad43b9650768b7296a336d4f3f2/src/EFCore.PG/Scaffolding/Internal/NpgsqlDatabaseModelFactory.cs#L454
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
                    var valueStrategy = (NpgsqlValueGenerationStrategy)column[NpgsqlAnnotationNames.ValueGenerationStrategy];

                    if (IsIdentityType(valueStrategy, out var identityType))
                    {
                        baseColumnDesc.IsIdentity = true;
                        baseColumnDesc.IdentityInfo = FromNpgSqlIdentityData(GetNpgsqlIdentityData(column));
                        baseColumnDesc.IdentityInfo.Values[IdentityType] = identityType;
                    }
                    else
                    {
                        baseColumnDesc.IsIdentity = false;
                        baseColumnDesc.IdentityInfo = FromNpgSqlIdentityData(IdentitySequenceOptionsData.Empty);
                    }
                    // read current value from database
                    var sequenceName = dbConnection.ExecuteScalar<string>(
                        $"select pg_get_serial_sequence('{IdentityName(column.Table.Schema,column.Table.Name)}','{column.Name}')");
                    // nextval will throw exception when not called the sequence once
                    var lastValue =
                        dbConnection.ExecuteScalar<long?>(
                            $"select case when is_called then last_value else null end from {sequenceName}");
                    baseColumnDesc.IdentityInfo.CurrentValue = lastValue;
                    //var sequenceInfo = dbConnection.ExecuteReaderAsTable($"select * from {sequenceName}");
                }
                else
                {
                    // reassign defaultValue Sql, because efcore will filter clr default
                    // https://github.com/npgsql/efcore.pg/blob/176fae2e08087ad43b9650768b7296a336d4f3f2/src/EFCore.PG/Scaffolding/Internal/NpgsqlDatabaseModelFactory.cs#L403
                    baseColumnDesc.DefaultValueSql = allDefaultValues.Where(p =>
                            p.Item1 == column.Table.Schema && p.Item2 == column.Table.Name && p.Item3 == column.Name)
                        .Select(p => p.Item4).SingleOrDefault();
                }

                return baseColumnDesc;
            }
            IdentitySequenceOptionsData GetNpgsqlIdentityData(DatabaseColumn column)
            {
                var sequenceData = column[NpgsqlAnnotationNames.IdentityOptions];
                if (sequenceData != null)
                {
                    return IdentitySequenceOptionsData.Deserialize(sequenceData as string);
                }
                else
                {
                    return IdentitySequenceOptionsData.Empty;
                }
            }
            bool IsIdentityType(NpgsqlValueGenerationStrategy npgsqlIdentityStrategy, out string identityType)
            {
                identityType = string.Empty;
                if (npgsqlIdentityStrategy == NpgsqlValueGenerationStrategy.IdentityAlwaysColumn)
                {
                    identityType = IDENTITY_ALWAYS;
                    return true;
                } if (npgsqlIdentityStrategy == NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                {
                    identityType = IDENTITY_BYDEFAULT;
                    return true;
                }
                else
                {
                    identityType = string.Empty;
                    return false;
                }
            }
            IdentityDescriptor FromNpgSqlIdentityData(IdentitySequenceOptionsData data)
            {
                var identity = new IdentityDescriptor
                {
                    IncrementBy = (int)data.IncrementBy,
                    MaxValue = data.MaxValue,
                    MinValue = data.MinValue,
                    IsCyclic = data.IsCyclic,
                    StartValue = data.StartValue.HasValue ? data.StartValue.Value : 1,
                };
                if (data.NumbersToCache != IdentitySequenceOptionsData.Empty.NumbersToCache)
                {
                    identity.Values[IdentityNumbersToCache] = data.NumbersToCache;
                }
                return identity;
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
