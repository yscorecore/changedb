using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Descriptors;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.Postgres
{
    [Collection(nameof(DatabaseEnvironment))]
    public class PostgresMetadataMigratorTest : IDisposable

    {
        private readonly IMetadataMigrator _metadataMigrator = PostgresMetadataMigrator.Default;
        private readonly MigrationContext _migrationContext;
        private readonly DbConnection _dbConnection;

        public PostgresMetadataMigratorTest(DatabaseEnvironment databaseEnvironment)
        {
            _dbConnection = databaseEnvironment.DbConnection;
            _migrationContext = new MigrationContext
            {
                TargetConnection = _dbConnection,
                SourceConnection = _dbConnection
            };
        }

        public void Dispose()
        {
            _dbConnection.ClearDatabase();
        }
        #region GetDescription
        [Fact]
        public async Task ShouldReturnEmptyDescriptorWhenGetDatabaseDescriptionAndGivenEmptyDatabase()
        {

            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Should().BeEquivalentTo(new DatabaseDescriptor
            {
                Tables = new List<TableDescriptor>(),
                //DefaultSchema = "public",
                //Collation = "en_US.utf8"
            });
        }
        [Fact]
        public async Task ShouldIncludeTableInfoWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
               "create schema ts",
               "create table ts.table1(id int ,nm varchar(64));");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Should().HaveCount(1);
            databaseDesc.Tables.First().Should().Match<TableDescriptor>(p => p.Schema == "ts" && p.Name == "table1");
        }

        [Fact]
        public async Task ShouldNotIncludeViewInfoWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
               "create schema ts",
               "create table ts.table1(id int ,nm varchar(64));",
               "create view ts.view2 as select * from ts.table1"
               );
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);

            databaseDesc.Tables.Should().HaveCount(1);
            databaseDesc.Tables.Should().NotContain(p => p.Schema == "ts" && p.Name == "view2");

        }

        [Fact]
        public async Task ShouldIncludeColumnInfoWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
               "create schema ts",
               "create table ts.table1(id integer)");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var columns = databaseDesc.Tables.First().Columns;

            columns.First().Should().BeEquivalentTo(new ColumnDescriptor
            {
                Name = "id",
                IsNullable = true,
                DataType = DataTypeDescriptor.Int(),
                Values = new Dictionary<string, object>
                {
                    [ColumnDescriptorExtensions.OriginStoreTypeKey] = "integer"
                }
            });

        }


        [Fact]
        public async Task ShouldIncludeNullableColumnInfoWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
               "create schema ts",
               "create table ts.table1(id integer ,nm integer NOT NULL);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var columns = databaseDesc.Tables.First().Columns;

            columns.Should().HaveCount(2);
            columns.First().IsNullable.Should().BeTrue();
            columns.Last().IsNullable.Should().BeFalse();

        }

        [Fact]
        public async Task ShouldIncludePrimaryKeyWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key,nm varchar(64));");

            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Should().HaveCount(1);
            databaseDesc.Tables.First().PrimaryKey.Should()
                .Match<PrimaryKeyDescriptor>(p => p.Name != null)
                .And
                .Match<PrimaryKeyDescriptor>(p => p.Columns.Count == 1 && p.Columns[0] == "id");
        }
        [Fact]
        public async Task ShouldIncludeMultipleColumnPrimaryKeyWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int,nm varchar(64),primary key(id,nm));");

            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Should().HaveCount(1);
            databaseDesc.Tables.First().PrimaryKey.Should()
                .Match<PrimaryKeyDescriptor>(p => p.Name != null)
                .And
                .Match<PrimaryKeyDescriptor>(p => p.Columns.Count == 2 && p.Columns[0] == "id" && p.Columns[1] == "nm");
        }

        [Fact]
        public async Task ShouldIncludeIndexeWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int,nm varchar(64));",
                "create index nm_index ON table1 (nm);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.First().Indexes.Should()
                .ContainSingle().And
                .ContainEquivalentOf(
                    new IndexDescriptor
                    {
                        Name = "nm_index",
                        Columns = new List<string> { "nm" }
                    });
        }

        [Fact]
        public async Task ShouldIncludeMultipleColumnIndexeWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int,nm varchar(64));",
                "create index id_nm_index ON table1 (id,nm);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.First().Indexes.Should()
                .ContainSingle().And
                .ContainEquivalentOf(
                    new IndexDescriptor
                    {
                        Name = "id_nm_index",
                        Columns = new List<string> { "id", "nm" }
                    });
        }

        [Fact]
        public async Task ShouldIncludeUniqueIndexeWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int,nm varchar(64));",
                "create unique index nm_index ON table1 (nm);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.First().Indexes.Should()
                .ContainSingle().And
                .ContainEquivalentOf(
                    new IndexDescriptor
                    {
                        Name = "nm_index",
                        IsUnique = true,
                        Columns = new List<string> { "nm" }
                    });
        }
        [Fact]
        public async Task ShouldExcludePrimaryIndexesWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key,nm varchar(64));",
                "create index nm_index ON table1 (nm);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.First().Indexes.Should()
                .ContainSingle().And
                .ContainEquivalentOf(
                new IndexDescriptor
                {
                    Name = "nm_index",
                    Columns = new List<string> { "nm" }
                });
        }

        [Fact]
        public async Task ShouldIncludeMultipleColumnForeignKeyWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key,nm varchar(64));",
                "create table table2(id int, id1 int references table1(id));");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table2").Single().ForeignKeys.Should()
                .ContainSingle().And.ContainEquivalentOf(new ForeignKeyDescriptor
                {
                    Name = "table2_id1_fkey",
                    OnDelete = ReferentialAction.NoAction,
                    PrincipalTable = "table1",
                    PrincipalSchema = "public",
                    ColumnNames = new List<string> { "id1" },
                    PrincipalNames = new List<string> { "id" }
                });
        }
        [Fact]
        public async Task ShouldIncludeForeignKeyWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key,nm varchar(64));",
                "create table table2(id int, id1 int references table1(id));");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table2").Single().ForeignKeys.Should()
                .ContainSingle().And.ContainEquivalentOf(new ForeignKeyDescriptor
                {
                    Name = "table2_id1_fkey",
                    OnDelete = ReferentialAction.NoAction,
                    PrincipalTable = "table1",
                    PrincipalSchema = "public",
                    ColumnNames = new List<string> { "id1" },
                    PrincipalNames = new List<string> { "id" }
                });
        }

        [Fact]
        public async Task ShouldIncludeMutilpleColumnForeignKeyWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int,nm int,primary key(id,nm));",
                "create table table2(id2 int, nm2 int, foreign key (id2, nm2) references table1 (id, nm));");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table2").Single().ForeignKeys.Should()
                .ContainSingle().And.ContainEquivalentOf(new ForeignKeyDescriptor
                {
                    Name = "table2_id2_nm2_fkey",
                    OnDelete = ReferentialAction.NoAction,
                    PrincipalTable = "table1",
                    PrincipalSchema = "public",
                    ColumnNames = new List<string> { "id2", "nm2" },
                    PrincipalNames = new List<string> { "id", "nm" }
                });
        }

        [Fact]
        public async Task ShouldIncludeUniqueWhenGetDatabaseDescription()
        {

            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key,nm varchar(64) unique);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Single().Uniques.Should()
                .ContainSingle().And.ContainEquivalentOf(new UniqueDescriptor
                {
                    Name = "table1_nm_key",
                    Columns = new List<string> { "nm" }
                });
        }

        [Fact]
        public async Task ShouldIncludeMutilpleColumnUniqueWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int,nm int,unique(id,nm));");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Single().Uniques.Should()
                .ContainSingle().And.ContainEquivalentOf(new UniqueDescriptor
                {
                    Name = "table1_id_nm_key",
                    Columns = new List<string> { "id", "nm" }
                });
        }
        [Fact]
        public async Task ShouldIncludeIdentityAlwaysColumnWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                   "create table table1(id integer generated always as identity);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var column = databaseDesc.Tables.Single().Columns.Single();
            column.IsIdentity.Should().BeTrue();
            column.IdentityInfo.Should().BeEquivalentTo(new IdentityDescriptor
            {
                IsCyclic = false,
                Values = new Dictionary<string, object>
                {
                    [PostgresUtils.IdentityType] = "ALWAYS",
                }
            });

        }
        [Fact]
        public async Task ShouldIncludeIdentityAlwaysColumnWithStartValueWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id integer generated always as identity, val integer);",
                    "INSERT INTO table1(val) VALUES(1)"
                );
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var column = databaseDesc.Tables.Single().Columns.First();
            column.IsIdentity.Should().BeTrue();
            column.IdentityInfo.Should().BeEquivalentTo(new IdentityDescriptor
            {
                IsCyclic = false,
                Values = new Dictionary<string, object>
                {
                    [PostgresUtils.IdentityType] = "ALWAYS",
                },
                CurrentValue = 1
            });
        }

        [Fact]
        public async Task ShouldIncludeIdentityAlwaysColumnWithCurrentValueWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id integer generated always as identity, val integer);",
                "INSERT INTO table1(val) VALUES(1)",
                "INSERT INTO table1(val) VALUES(1)"
            );
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);

            var column = databaseDesc.Tables.Single().Columns.First();
            column.IsIdentity.Should().BeTrue();
            column.IdentityInfo.Should().BeEquivalentTo(new IdentityDescriptor
            {
                IsCyclic = false,
                Values = new Dictionary<string, object>
                {
                    [PostgresUtils.IdentityType] = "ALWAYS",
                },
                CurrentValue = 2
            });
        }

        [Fact]
        public async Task ShouldIncludeIdentityByDefaultColumnWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                   "create table table1(id bigint generated by default as identity);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);

            var column = databaseDesc.Tables.Single().Columns.First();
            column.IsIdentity.Should().BeTrue();
            column.IdentityInfo.Should().BeEquivalentTo(new IdentityDescriptor
            {
                IsCyclic = false,
                Values = new Dictionary<string, object>
                {
                    [PostgresUtils.IdentityType] = "BY DEFAULT",
                }
            });
        }
        [Fact]
        public async Task ShouldIncludeSerialColumnWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int, val serial);",
                "INSERT INTO table1(id) VALUES(1);"
            );
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var column = databaseDesc.Tables.Single().Columns.Last();
            column.IsIdentity.Should().BeFalse();
            column.IdentityInfo.Should().BeEquivalentTo(new IdentityDescriptor
            {
                IsCyclic = false,
                CurrentValue = 1,
            });


        }

        [Fact]
        public async Task ShouldIncludeSerialColumnWithCurrentValueWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int, val serial);",
                "INSERT INTO table1(id) VALUES(1);",
                "INSERT INTO table1(id) VALUES(2);"
            );
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var column = databaseDesc.Tables.Single().Columns.Last();
            column.IsIdentity.Should().BeFalse();
            column.IdentityInfo.Should().BeEquivalentTo(new IdentityDescriptor
            {
                IsCyclic = false,
                CurrentValue = 2,
            });
        }



        [Theory]
        [InlineData("integer", "0", null, 0)]
        [InlineData("integer", "123", null, 123)]
        [InlineData("boolean", "true", null, true)]
        [InlineData("TIMESTAMP(3)", "current_timestamp(3)", Function.Now, null)]
        [InlineData("uuid", "gen_random_uuid()", Function.Uuid, null)]
        public async Task ShouldIncludeDefaultValueWhenGetDatabaseDescription(string dataType, string defaultValue, Function? function, object constant)
        {
            _dbConnection.ExecuteNonQuery(
                   $"create table table1(id int,val {dataType} default {defaultValue});");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var column = databaseDesc.Tables.Single().Columns.Last();
            column.DefaultValue.Should().BeEquivalentTo(new SqlExpressionDescriptor
            {
                Function = function,
                Constant = constant
            });
            column.Values.Should().ContainKey(ColumnDescriptorExtensions.OriginDefaultValueKey);
        }


        #endregion


        #region MigrateMetaData
        [Fact]
        public async Task ShouldCreateSchemasAndTableWhenMigrateMetadata()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor {
                                Name="id",
                                DataType = DataTypeDescriptor.Int(),
                                Values = new Dictionary<string, object>
                                {
                                    [ColumnDescriptorExtensions.OriginStoreTypeKey]="integer"
                                }
                            }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
        }
        [Fact]
        public async Task ShouldCreateTableInDefaultSchemaWhenMigrateMetadataAndNoSchema()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor {
                                Name="id",
                                DataType = DataTypeDescriptor.Int(),
                                Values = new Dictionary<string, object>
                                {
                                    [ColumnDescriptorExtensions.OriginStoreTypeKey]="integer"
                                }
                            }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var exceptedDatabaseDesc = databaseDesc.DeepCloneAndSet(p => p.Tables.ForEach(t => t.Schema = "public"));
            actualDatabaseDesc.Should().BeEquivalentTo(exceptedDatabaseDesc);
        }

        [Fact]
        public async Task ShouldCreatePrimaryKeyWhenMigrateMetadata()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {

                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", StoreType="integer" }
                        },
                         PrimaryKey = new PrimaryKeyDescriptor { Name="table1_id_pkey", Columns = new List<string>{"id" } },
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
        }

        [Fact]
        public async Task ShouldCreatePrimaryKeyWhenMigrateMetadataAndPrimaryNameIsEmpty()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {

                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", StoreType="integer" }
                        },
                        PrimaryKey = new PrimaryKeyDescriptor { Columns = new List<string>{"id" } },
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var exceptedDatabaseDesc = databaseDesc.DeepCloneAndSet(p => p.Tables.ForEach(t => t.PrimaryKey.Name = "table1_pkey"));
            actualDatabaseDesc.Should().BeEquivalentTo(exceptedDatabaseDesc);
        }

        [Fact]
        public async Task ShouldCreateUniqueWhenMigrateMetadata()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", StoreType="integer" }
                        },
                        Uniques = new List<UniqueDescriptor>
                        {
                            new UniqueDescriptor{ Name="table1_id_key", Columns = new List<string>{"id" } }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
        }

        [Fact]
        public async Task ShouldCreateMutilColumnUniqueWhenMigrateMetadata()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType= DataTypeDescriptor.Int() },
                            new ColumnDescriptor { Name="nm", DataType= DataTypeDescriptor.Int() }
                        },
                        Uniques = new List<UniqueDescriptor>
                        {
                            new UniqueDescriptor{ Name="table1_id_key", Columns = new List<string>{"id","nm" } }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Tables.First().Uniques.Should()
                .BeEquivalentTo(new List<UniqueDescriptor>
                        {
                            new UniqueDescriptor{ Name="table1_id_key", Columns = new List<string>{"id","nm" } }
                        });
        }

        [Fact]
        public async Task ShouldCreateIndexWhenMigrateMetadata()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType= DataTypeDescriptor.Int() }
                        },
                        Indexes = new List<IndexDescriptor>
                        {
                            new IndexDescriptor{ Name="index_name", Columns = new List<string> {"id" } }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Tables.Single().Indexes.Should()
                .BeEquivalentTo(new List<IndexDescriptor>
                        {
                            new IndexDescriptor{ Name="index_name", Columns = new List<string> {"id" } }
                        });
        }

        [Fact]
        public async Task ShouldCreateMutilColumnIndexWhenMigrateMetadata()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType= DataTypeDescriptor.Int() },
                             new ColumnDescriptor { Name="nm",DataType= DataTypeDescriptor.Int() }
                        },
                        Indexes = new List<IndexDescriptor>
                        {
                            new IndexDescriptor{ Name="index_name", Columns = new List<string> {"id","nm" } }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Tables.Single().Indexes.Should()
                 .BeEquivalentTo(new List<IndexDescriptor>
                        {
                            new IndexDescriptor{ Name="index_name", Columns = new List<string> {"id","nm" } }
                        });
        }
        [Fact]
        public async Task ShouldCreateUniqueIndexWhenMigrateMetadata()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType= DataTypeDescriptor.Int() }
                        },
                        Indexes = new List<IndexDescriptor>
                        {
                            new IndexDescriptor{ Name="index_name", IsUnique=true, Columns = new List<string> {"id" } }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Tables.Single().Indexes.Should()
                .BeEquivalentTo(new List<IndexDescriptor>
                        {
                            new IndexDescriptor{ Name="index_name", IsUnique=true, Columns = new List<string> {"id" } }
                        });
        }
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldSetColumnNullableWhenMigrateMetadata(bool nullable)
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id",DataType = DataTypeDescriptor.Int(),IsNullable = nullable },

                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var column = actualDatabaseDesc.Tables.First().Columns.Single();
            column.IsNullable.Should().Be(nullable);

        }
        [Fact]
        public async Task ShouldCreatForgienKeysWhenMigrateMetadata()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType = DataTypeDescriptor.Int()},
                        },
                        Uniques = new List<UniqueDescriptor>
                        {
                            new UniqueDescriptor { Name="unique_table1_id", Columns = new List<string>{ "id"} }
                        }
                    },
                    new TableDescriptor
                    {
                        Schema="ts2",
                        Name="table2",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id2", DataType = DataTypeDescriptor.Int()},
                        },
                         ForeignKeys = new List<ForeignKeyDescriptor>
                         {
                             new ForeignKeyDescriptor
                             {
                                Name="foreign_key",
                                PrincipalSchema="ts",
                                PrincipalTable="table1",
                                PrincipalNames= new List<string> {"id" },
                                ColumnNames= new List<string> { "id2"},
                                OnDelete = ReferentialAction.NoAction
                             }
                         }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Tables.Last().ForeignKeys.Should()
                .BeEquivalentTo(new List<ForeignKeyDescriptor>
                         {
                             new ForeignKeyDescriptor
                             {
                                Name="foreign_key",
                                PrincipalSchema="ts",
                                PrincipalTable="table1",
                                PrincipalNames= new List<string> {"id" },
                                ColumnNames= new List<string> { "id2"},
                                OnDelete = ReferentialAction.NoAction
                             }
                         });
        }

        [Fact]
        public async Task ShouldCreatMutilColumnForgienKeysWhenMigrateMetadata()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType = DataTypeDescriptor.Int()},
                            new ColumnDescriptor { Name="nm", DataType = DataTypeDescriptor.Int()},
                        },
                        Uniques = new List<UniqueDescriptor>
                        {
                            new UniqueDescriptor { Name="unique_table1_id", Columns = new List<string>{ "id","nm"} }
                        }
                    },
                    new TableDescriptor
                    {
                        Schema="ts2",
                        Name="table2",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id2", DataType = DataTypeDescriptor.Int()},
                             new ColumnDescriptor { Name="nm2",DataType = DataTypeDescriptor.Int()},
                        },
                         ForeignKeys = new List<ForeignKeyDescriptor>
                         {
                             new ForeignKeyDescriptor
                             {
                                Name="foreign_key",
                                PrincipalSchema="ts",
                                PrincipalTable="table1",
                                PrincipalNames= new List<string> {"id","nm" },
                                ColumnNames= new List<string> { "id2","nm2"},
                                OnDelete = ReferentialAction.NoAction
                             }
                         }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Tables.Last().ForeignKeys.Should()
                .BeEquivalentTo(new List<ForeignKeyDescriptor>
                         {
                             new ForeignKeyDescriptor
                             {
                                Name="foreign_key",
                                PrincipalSchema="ts",
                                PrincipalTable="table1",
                                PrincipalNames= new List<string> {"id","nm" },
                                ColumnNames= new List<string> { "id2","nm2"},
                                OnDelete = ReferentialAction.NoAction
                             }
                         });
        }

        //[Theory]
        //[InlineData("integer", null, 1, "1")]
        //[InlineData("varchar(10)", null, "abc", "'abc'")]
        //[InlineData("boolean", null, true, "True")]
        //[InlineData("boolean", null, false, "false")]
        //[InlineData("uuid", Function.Uuid, null, "gen_random_uuid()")]
        //[InlineData("timestamp", Function.Now, null, "CURRENT_TIMESTAMP")]
        //public async Task ShouldSetColumnDefaultValueWhenMigrateMetadata(string storeType, Function? function, object value, string expectedDefaultValue)
        //{
        //    var databaseDesc = new DatabaseDescriptor()
        //    {
        //        Tables = new List<TableDescriptor>
        //        {
        //            new TableDescriptor
        //            {
        //                Schema="ts",
        //                Name="table1",
        //                Columns =new List<ColumnDescriptor>
        //                {
        //                    new ColumnDescriptor { Name="id", StoreType="integer", DefaultValueSql="1"},
        //                    new ColumnDescriptor { Name="nm", StoreType="character varying(10)", DefaultValueSql="'abc'::character varying"},
        //                    new ColumnDescriptor { Name="used", StoreType="boolean", DefaultValueSql="true"},
        //                    new ColumnDescriptor { Name="rid", StoreType="uuid", DefaultValueSql="gen_random_uuid()"},
        //                    new ColumnDescriptor { Name="createtime", StoreType="timestamp without time zone", DefaultValueSql="CURRENT_TIMESTAMP"},
        //                },
        //                PrimaryKey = new PrimaryKeyDescriptor{  Name="pk_ts_table1", Columns = new List<string>{ "id"} }
        //            }
        //        }
        //    };
        //    await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
        //    var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
        //    actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
        //}
        [Fact]
        public async Task ShouldMapByDefaultIdentityWhenMigrateMetadata()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                           new ColumnDescriptor
                           {
                                Name="id", DataType = DataTypeDescriptor.BigInt(), IsIdentity =true,
                                IdentityInfo = new IdentityDescriptor
                                {
                                    IsCyclic =false,
                                    Values = new Dictionary<string, object>
                                    {
                                        [PostgresUtils.IdentityType]="BY DEFAULT",
                                    }
                                }
                           }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var column = actualDatabaseDesc.Tables.First().Columns.Single();

            column.IsIdentity.Should().BeTrue();
            column.IdentityInfo.Should().BeEquivalentTo(new IdentityDescriptor
            {
                IsCyclic = false,
                Values = new Dictionary<string, object>
                {
                    [PostgresUtils.IdentityType] = "BY DEFAULT",
                }
            });

        }

        [Fact]
        public async Task ShouldMapAlwaysIdentityWhenMigrateMetadata()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                           new ColumnDescriptor
                           {
                                Name="id", DataType = DataTypeDescriptor.BigInt(), IsIdentity =true,
                                IdentityInfo = new IdentityDescriptor
                                {
                                    IsCyclic =false,
                                    Values = new Dictionary<string, object>
                                    {
                                        [PostgresUtils.IdentityType]="ALWAYS",
                                    }
                                }
                           }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var column = actualDatabaseDesc.Tables.First().Columns.Single();

            column.IsIdentity.Should().BeTrue();
            column.IdentityInfo.Should().BeEquivalentTo(new IdentityDescriptor
            {
                IsCyclic = false,
                Values = new Dictionary<string, object>
                {
                    [PostgresUtils.IdentityType] = "ALWAYS",
                }
            });
        }

        //[Fact] TODO

        public async Task ShouldMapAlwaysIdentityWhenMigrateMetadataAndStartValueLessThan1()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor
                            {
                                Name="id", StoreType = "bigint", IsIdentity =true,
                                IdentityInfo = new IdentityDescriptor
                                {
                                    IsCyclic =false,
                                    StartValue = -5,
                                    Values = new Dictionary<string, object>
                                    {
                                        [PostgresUtils.IdentityType]="ALWAYS",
                                    }
                                }
                            }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var expectDatabaseDesc = databaseDesc.DeepClone().DoIfNotNull(p => p.Tables.SelectMany(t => t.Columns).Each(
                c =>
                {

                    c.IdentityInfo.MinValue = -5;
                }));
            actualDatabaseDesc.Should().BeEquivalentTo(expectDatabaseDesc);
        }

        [Fact]
        public async Task ShouldMapIdentityWhenMigrateMetadataAndWithFullArguments()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                           new ColumnDescriptor
                           {
                                Name="id", DataType = DataTypeDescriptor.BigInt(), IsIdentity =true,
                                IdentityInfo = new IdentityDescriptor
                                {
                                    IsCyclic =true,
                                    MinValue=0,
                                    StartValue=1,
                                    IncrementBy=5,
                                    MaxValue=1000,
                                    Values = new Dictionary<string, object>
                                    {
                                        [PostgresUtils.IdentityType]="ALWAYS",
                                        [PostgresUtils.IdentityNumbersToCache]=50,
                                    }
                                }
                           }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var column = actualDatabaseDesc.Tables.First().Columns.Single();

            column.IsIdentity.Should().BeTrue();
            column.IdentityInfo.Should().BeEquivalentTo(new IdentityDescriptor
            {
                IsCyclic = true,
                MinValue = 0,
                StartValue = 1,
                IncrementBy = 5,
                MaxValue = 1000,
                Values = new Dictionary<string, object>
                {
                    [PostgresUtils.IdentityType] = "ALWAYS",
                    [PostgresUtils.IdentityNumbersToCache] = 50,
                }
            });
        }
        [Fact]
        public async Task ShouldMapToAlwaysIdentityWhenMigrateMetadataAndIdentityTypeIsEmpty()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                           new ColumnDescriptor
                           {
                                Name="id", StoreType = "smallint", IsIdentity =true,
                                IdentityInfo = new IdentityDescriptor
                                {
                                    IsCyclic =false,
                                    Values = new Dictionary<string, object>
                                    {

                                    }
                                }
                           }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var expectedDatabaseDesc = databaseDesc.DeepCloneAndSet(desc =>
            {
                desc.Tables.SelectMany(p => p.Columns).Select(p => p.IdentityInfo.Values).Each(dic => { dic[PostgresUtils.IdentityType] = "ALWAYS"; });
            });
            actualDatabaseDesc.Should().BeEquivalentTo(expectedDatabaseDesc);
        }

        [Fact]
        public async Task ShouldMapSerialColumnWhenMigrateMetadata()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema="ts",
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                           new ColumnDescriptor
                           {
                                Name="id", IsNullable=false, StoreType = "integer", IsIdentity = false,
                                IdentityInfo = new IdentityDescriptor
                                {
                                    IsCyclic = false,
                                    Values = new Dictionary<string, object>
                                    {

                                    }
                                }
                           }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
        }
        #endregion






    }
}
