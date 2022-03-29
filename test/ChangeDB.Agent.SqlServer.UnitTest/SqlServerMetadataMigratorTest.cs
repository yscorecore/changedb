using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Xunit;

namespace ChangeDB.Agent.SqlServer
{
    [Collection(nameof(DatabaseEnvironment))]
    public class SqlServerMetadataMigratorTest : IDisposable
    {
        private readonly IMetadataMigrator _metadataMigrator = SqlServerMetadataMigrator.Default;
        private readonly MigrationContext _migrationContext;
        private readonly DbConnection _dbConnection;

        public SqlServerMetadataMigratorTest(DatabaseEnvironment databaseEnvironment)
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

        public async Task ShouldIncludeNullableColumnInfoWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
               "create schema ts",
               "create table ts.table1(id int, nm int NOT NULL);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var columns = databaseDesc.Tables.Single().Columns;
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
            databaseDesc.Tables.Single().Indexes.Should()
                .BeEquivalentTo(new List<IndexDescriptor>
                {   
                    new IndexDescriptor
                    {
                        Name = "nm_index",
                        Columns = new List<string> { "nm1" }
                    }
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
        public async Task ShouldIncludeForeignKeyWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key,nm varchar(64));",
                "create table table2(id int, id1 int);",
                "ALTER TABLE table2 add constraint table2_id1_fkey foreign key(id1) references table1(id);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Single(p => p.Name == "table2").ForeignKeys.Should()
                .ContainSingle().And.BeEquivalentTo(new ForeignKeyDescriptor
                {
                    Name = "table2_id1_fkey",
                    OnDelete = ReferentialAction.NoAction,
                    PrincipalTable = "table1",
                    PrincipalSchema = "dbo",
                    ColumnNames = new List<string> { "id1" },
                    PrincipalNames = new List<string> { "id" }
                });
        }

        [Fact]
        public async Task ShouldIncludeMutilpleColumnForeignKeyWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int,nm int,primary key(id,nm));",
                "create table table2(id2 int, nm2 int);",
                "ALTER TABLE table2 add constraint table2_id2_nm2_fkey foreign key(id2, nm2) references table1(id, nm);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table2").Single().ForeignKeys.Should()
                .ContainSingle().And.ContainEquivalentOf(new ForeignKeyDescriptor
                {
                    Name = "table2_id2_nm2_fkey",
                    OnDelete = ReferentialAction.NoAction,
                    PrincipalTable = "table1",
                    PrincipalSchema = "dbo",
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
                .ContainSingle().Which.Columns.Should().BeEquivalentTo(new List<string> { "nm" });
        }

        [Fact]
        public async Task ShouldIncludeMutilpleColumnUniqueWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int,nm int,unique(id,nm));");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Single().Uniques.Should()
                .ContainSingle().Which.Columns.Should().BeEquivalentTo(new List<string> { "id", "nm" });
        }
        [Fact]
        public async Task ShouldIncludeIdentityDescriptorWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                   "create table table1(id int identity(2,5));");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema = "dbo",
                    Columns = new List<ColumnDescriptor>
                    {
                       new ColumnDescriptor
                       {
                            Name="id",DataType=DataTypeDescriptor.Int(), IsIdentity =true,IsStored= false,
                            IdentityInfo = new IdentityDescriptor
                            {
                                IsCyclic =false,
                                StartValue=2,
                                IncrementBy=5
                            }
                       }
                    }
                });
        }
        [Fact]
        public async Task ShouldIncludeIdentityDescriptorWithStartValueWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int identity(2,5),val int);",
                "insert into table1(val) VALUES(123)"
            );
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema = "dbo",
                    Columns = new List<ColumnDescriptor>
                    {
                        new ColumnDescriptor
                        {
                            Name="id",DataType=DataTypeDescriptor.Int(), IsIdentity =true,IsStored= false,IsNullable= false,
                            IdentityInfo = new IdentityDescriptor
                            {
                                IsCyclic =false,
                                StartValue=2,
                                IncrementBy=5,
                                CurrentValue =2
                            }
                        },
                        new ColumnDescriptor
                        {
                            Name="val",DataType=DataTypeDescriptor.Int(), IsIdentity =false,IsStored= false,IsNullable =true
                        }
                    }
                });
        }
        [Fact]
        public async Task ShouldIncludeIdentityDescriptorWithCurrentValueWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                   "create table table1(id int identity(2,5),val int);",
                   "insert into table1(val) VALUES(123)",
                   "insert into table1(val) VALUES(123)"
                   );
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema = "dbo",
                    Columns = new List<ColumnDescriptor>
                    {
                       new ColumnDescriptor
                       {
                            Name="id",DataType=DataTypeDescriptor.Int(), IsIdentity =true,IsStored= false,IsNullable= false,
                            IdentityInfo = new IdentityDescriptor
                            {
                                IsCyclic =false,
                                StartValue=2,
                                IncrementBy=5,
                                CurrentValue =7
                            }
                       },
                       new ColumnDescriptor
                       {
                            Name="val",DataType=DataTypeDescriptor.Int(), IsIdentity =false,IsStored= false,IsNullable =true
                       }
                    }
                });
        }

        [Fact]
        public async Task ShouldIncludeUuidColumnWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                   "create table table1(abc uniqueidentifier default newid());");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema = "dbo",
                    Columns = new List<ColumnDescriptor>
                    {
                       new ColumnDescriptor{ Name="abc", IsNullable=true, StoreType = "uniqueidentifier",DefaultValueSql="(newid())" }
                    }
                });
        }
        [Fact]
        public async Task ShouldIncludeTimestampColumnWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                   "create table table1(id datetime default getdate());");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema = "dbo",
                    Columns = new List<ColumnDescriptor>
                    {
                       new ColumnDescriptor{ Name="id", IsNullable=true, StoreType = "datetime", DefaultValueSql="(getdate())"}
                    }
                });
        }
        [Fact]
        public async Task ShouldIncludeDefaultValueWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int NOT NULL default 0,nm varchar(10) default 'abc', val money default 0);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema = "dbo",
                    Columns = new List<ColumnDescriptor>
                    {
                        new ColumnDescriptor{ Name="id", IsNullable=false,DataType=DataTypeDescriptor.Int(), DefaultValueSql="((0))"},
                        new ColumnDescriptor{ Name="nm", IsNullable=true, StoreType = "varchar(10)", DefaultValueSql="('abc')"},
                        new ColumnDescriptor{ Name="val", IsNullable=true, StoreType = "money", DefaultValueSql="((0))"}
                    }
                });
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
                            new ColumnDescriptor { Name="id",DataType=DataTypeDescriptor.Int(), IsNullable = true}
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
                            new ColumnDescriptor { Name="id",DataType=DataTypeDescriptor.Int() }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            var exceptedDatabaseDesc = databaseDesc.DeepCloneAndSet(p => p.Tables.ForEach(t => t.Schema = "dbo"));
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
                            new ColumnDescriptor { Name="id",DataType=DataTypeDescriptor.Int() }
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
                            new ColumnDescriptor { Name="id",DataType=DataTypeDescriptor.Int() }
                        },
                        PrimaryKey = new PrimaryKeyDescriptor { Columns = new List<string>{"id" } },
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Tables.Single().PrimaryKey.Name.Should().StartWith("PK__table1");
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
                            new ColumnDescriptor { Name="id",DataType=DataTypeDescriptor.Int() }
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

                            new ColumnDescriptor { Name="id",DataType=DataTypeDescriptor.Int() },
                            new ColumnDescriptor { Name="nm",DataType=DataTypeDescriptor.Int() }
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
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
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
                            new ColumnDescriptor { Name="id",DataType=DataTypeDescriptor.Int() }
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
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
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
                            new ColumnDescriptor { Name="id",DataType=DataTypeDescriptor.Int() },
                             new ColumnDescriptor { Name="nm",DataType=DataTypeDescriptor.Int() }
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
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
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
                            new ColumnDescriptor { Name="id",DataType=DataTypeDescriptor.Int() }
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
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
        }
        [Fact]
        public async Task ShouldSetColumnNullableWhenMigrateMetadata()
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
                            new ColumnDescriptor { Name="id",DataType=DataTypeDescriptor.Int(),IsNullable = false },
                            new ColumnDescriptor { Name="id2",DataType=DataTypeDescriptor.Int(),IsNullable = true }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
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
                            new ColumnDescriptor { Name="id",DataType=DataTypeDescriptor.Int()},
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
                            new ColumnDescriptor { Name="id2",DataType=DataTypeDescriptor.Int()},
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
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
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
                            new ColumnDescriptor { Name="id",DataType=DataTypeDescriptor.Int()},
                            new ColumnDescriptor { Name="nm",DataType=DataTypeDescriptor.Int()},
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
                            new ColumnDescriptor { Name="id2",DataType=DataTypeDescriptor.Int()},
                             new ColumnDescriptor { Name="nm2",DataType=DataTypeDescriptor.Int()},
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
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
        }

        [Fact]
        public async Task ShouldSetColumnDefaultValueWhenMigrateMetadata()
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
                            new ColumnDescriptor { Name="id",DataType=DataTypeDescriptor.Int(), DefaultValueSql="((1))"},
                             new ColumnDescriptor { Name="nm", StoreType="nvarchar(10)", DefaultValueSql="('abc')"},
                             new ColumnDescriptor { Name="used", StoreType="bit", DefaultValueSql="((1))"},
                             new ColumnDescriptor {Name="rid", StoreType="uniqueidentifier", DefaultValueSql="(newid())"},
                             new ColumnDescriptor { Name="createtime", StoreType="datetime2", DefaultValueSql="(getdate())"},
                        },
                        PrimaryKey = new PrimaryKeyDescriptor{ Name="pk_table1_id", Columns = new List<string>{"id"}}
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
        }


        [Fact]
        public async Task ShouldMapIdentityWhenMigrateMetadata()
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
                                Name="id",DataType=DataTypeDescriptor.Int(), IsIdentity =true,
                                IdentityInfo = new IdentityDescriptor
                                {

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
                                Name="id", StoreType = "bigint", IsIdentity =true,
                                IdentityInfo = new IdentityDescriptor
                                {
                                    StartValue=2,
                                    IncrementBy=5,
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
