using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;
using FluentAssertions;
using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace ChangeDB.Agent.Postgres
{
    public class PostgresMetadataMigratorTest : IDisposable

    {
        private readonly IMetadataMigrator _metadataMigrator = PostgresMetadataMigrator.Default;
        private readonly MigrationSetting _migrationSetting = new MigrationSetting { DropTargetDatabaseIfExists = true };
        private readonly DbConnection _dbConnection;

        public PostgresMetadataMigratorTest()
        {
            _dbConnection = new NpgsqlConnection($"Server=127.0.0.1;Port=5432;Database={TestUtils.RandomDatabaseName()};User Id=postgres;Password=mypassword;");
            _dbConnection.CreateDatabase();
        }
        #region DropAndCreate
        [Fact]
        public async Task ShouldDropCurrentDatabase()
        {
            await _metadataMigrator.DropDatabaseIfExists(_dbConnection, _migrationSetting);
            Action action = () =>
            {
                _dbConnection.ExecuteScalar<string>("select current_database()");
            };
            action.Should().Throw<Npgsql.PostgresException>()
                .WithMessage("3D000: database \"*\" does not exist");

        }
        [Fact]
        public async Task ShouldCreateNewDatabase()
        {
            await _metadataMigrator.DropDatabaseIfExists(_dbConnection, _migrationSetting);
            await _metadataMigrator.CreateDatabase(_dbConnection, _migrationSetting);
            var currentDatabase = _dbConnection.ExecuteScalar<string>("select current_database()");
            currentDatabase.Should().NotBeEmpty();
        }
        #endregion

        #region GetDescription
        [Fact]
        public async Task ShouldReturnEmptyDescriptorWhenGetDatabaseDescriptionAndGivenEmptyDatabase()
        {

            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            databaseDesc.Tables.Should().HaveCount(1);
            databaseDesc.Tables.First().Should().Match<TableDescriptor>(p => p.Schema == "ts" && p.Name == "table1");
        }

        [Fact]
        public async Task ShouldIncludeNullableColumnInfoWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
               "create schema ts",
               "create table ts.table1(id integer ,nm integer not null);");
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            databaseDesc.Tables.Should().HaveCount(1);
            databaseDesc.Tables.Should().ContainSingle()
                .And.ContainEquivalentOf(
                new TableDescriptor
                {
                    Name = "table1",
                    Schema = "ts",
                    Columns = new List<ColumnDescriptor>
                    {
                        new ColumnDescriptor { Name="id", IsNullable=true, StoreType="integer"},
                        new ColumnDescriptor { Name="nm", IsNullable=false, StoreType="integer"}
                    }
                });
        }

        [Fact]
        public async Task ShouldIncludePrimaryKeyWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key,nm varchar(64));");

            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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

            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
            _dbConnection.ReCreateDatabase();
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key,nm varchar(64));",
                "create table table2(id int, id1 int references table1(id));");
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
            _dbConnection.ReCreateDatabase();
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key,nm varchar(64));",
                "create table table2(id int, id1 int references table1(id));");
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
            _dbConnection.ReCreateDatabase();
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int,nm int,unique(id,nm));");
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            databaseDesc.Tables.Single().Uniques.Should()
                .ContainSingle().And.ContainEquivalentOf(new UniqueDescriptor
                {
                    Name = "table1_id_nm_key",
                    Columns = new List<string> { "id", "nm" }
                });
        }
        [Fact]
        public async Task ShouldIncludeIdentityColumnWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                   "create table table1(id int generated always as identity);");
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema="public",
                    Columns = new List<ColumnDescriptor>
                    {
                       new ColumnDescriptor{ Name="id", StoreType = "integer", ValueGenerated = ValueGenerated.OnAdd }
                    }
                });
        }
        [Fact]
        public async Task ShouldIncludeSerialColumnWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                   "create table table1(id serial);");
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema = "public",
                    Columns = new List<ColumnDescriptor>
                    {
                       new ColumnDescriptor{ Name="id", StoreType = "integer", ValueGenerated = ValueGenerated.OnAdd }
                    }
                });
        }
        #endregion


        #region MigrateData
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
                            new ColumnDescriptor { Name="id", StoreType="integer" }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllMetaData(databaseDesc, _dbConnection, _migrationSetting);
            var actualDatabaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
                            new ColumnDescriptor { Name="id", StoreType="integer" }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllMetaData(databaseDesc, _dbConnection, _migrationSetting);
            var actualDatabaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
            await _metadataMigrator.MigrateAllMetaData(databaseDesc, _dbConnection, _migrationSetting);
            var actualDatabaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
            await _metadataMigrator.MigrateAllMetaData(databaseDesc, _dbConnection, _migrationSetting);
            var actualDatabaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
            await _metadataMigrator.MigrateAllMetaData(databaseDesc, _dbConnection, _migrationSetting);
            var actualDatabaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
                            new ColumnDescriptor { Name="id", StoreType="integer" },
                            new ColumnDescriptor { Name="nm", StoreType="integer" }
                        },
                        Uniques = new List<UniqueDescriptor>
                        {
                            new UniqueDescriptor{ Name="table1_id_key", Columns = new List<string>{"id","nm" } }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllMetaData(databaseDesc, _dbConnection, _migrationSetting);
            var actualDatabaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
                            new ColumnDescriptor { Name="id", StoreType="integer" }
                        },
                        Indexes = new List<IndexDescriptor>
                        {
                            new IndexDescriptor{ Name="index_name", Columns = new List<string> {"id" } }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllMetaData(databaseDesc, _dbConnection, _migrationSetting);
            var actualDatabaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
                            new ColumnDescriptor { Name="id", StoreType="integer" },
                             new ColumnDescriptor { Name="nm", StoreType="integer" }
                        },
                        Indexes = new List<IndexDescriptor>
                        {
                            new IndexDescriptor{ Name="index_name", Columns = new List<string> {"id","nm" } }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllMetaData(databaseDesc, _dbConnection, _migrationSetting);
            var actualDatabaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
                            new ColumnDescriptor { Name="id", StoreType="integer" }
                        },
                        Indexes = new List<IndexDescriptor>
                        {
                            new IndexDescriptor{ Name="index_name", IsUnique=true, Columns = new List<string> {"id" } }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllMetaData(databaseDesc, _dbConnection, _migrationSetting);
            var actualDatabaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
                            new ColumnDescriptor { Name="id", StoreType="integer",IsNullable = false },
                            new ColumnDescriptor { Name="id2", StoreType="integer",IsNullable = true }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllMetaData(databaseDesc, _dbConnection, _migrationSetting);
            var actualDatabaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
                            new ColumnDescriptor { Name="id", StoreType="integer"},
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
                            new ColumnDescriptor { Name="id2", StoreType="integer"},
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
            await _metadataMigrator.MigrateAllMetaData(databaseDesc, _dbConnection, _migrationSetting);
            var actualDatabaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
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
                            new ColumnDescriptor { Name="id", StoreType="integer"},
                            new ColumnDescriptor { Name="nm", StoreType="integer"},
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
                            new ColumnDescriptor { Name="id2", StoreType="integer"},
                             new ColumnDescriptor { Name="nm2", StoreType="integer"},
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
            await _metadataMigrator.MigrateAllMetaData(databaseDesc, _dbConnection, _migrationSetting);
            var actualDatabaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
        }
        #endregion


        //[Fact]
        //public async Task ShouldCreateSchemasWhenPreMigrate()
        //{
        //    var databaseDesc = new DatabaseDescriptor()
        //    { Schemas = new List<string> { "public", "abc", "Bcd" } };
        //    await _metadataMigrator.PreMigrate(databaseDesc, _dbConnection, _migrationSetting);
        //    var schemas = _dbConnection.ExecuteReaderAsList<string>("select schema_name from information_schema.schemata s ");
        //    schemas.Should().Contain(databaseDesc.GetAllSchemas());
        //}
        [Fact]
        public async Task ShouldCreateTableWhenMigrate()
        {
            _dbConnection.ReCreateDatabase();
            var databaseDesc = new DatabaseDescriptor
            {
                Tables = new List<TableDescriptor>
                   {
                        new TableDescriptor
                        {
                            Name="table1",
                            Schema="ts",
                            Columns = new List<ColumnDescriptor>
                            {
                              new ColumnDescriptor{ Name ="id",IsNullable = true, StoreType="int"},
                              new ColumnDescriptor{ Name ="nm",IsNullable =true, StoreType="varchar(64)"}
                            }
                        }
                   }
            };
            await _metadataMigrator.MigrateAllMetaData(databaseDesc, _dbConnection, _migrationSetting);
            var databaseDesc2 = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            // databaseDesc2.DefaultSchema.Should().Be("public");
            // databaseDesc2.Collation.Should().NotBeNull();
            databaseDesc2.Tables.Should().HaveCount(1);
            databaseDesc2.Tables.First().Should().BeEquivalentTo(
                 new TableDescriptor
                 {
                     Name = "table1",
                     Schema = "ts",
                     Columns = new List<ColumnDescriptor>
                            {
                              new ColumnDescriptor{ Name ="id",IsNullable = true, StoreType="integer"},
                              new ColumnDescriptor{ Name ="nm",IsNullable =true, StoreType="character varying(64)"}
                            }

                 });
        }
        [Theory]

        //[InlineData("varchar(12)", CommonDatabaseType.NVarchar, 12, null)]
        //[InlineData("character varying(12)", CommonDatabaseType.NVarchar, 12, null)]
        //[InlineData("char(12)", CommonDatabaseType.NChar, 12, null)]
        //[InlineData("character(12)", CommonDatabaseType.NChar, 12, null)]
        //[InlineData("text", CommonDatabaseType.NText, null, null)]
        //[InlineData("varchar", CommonDatabaseType.NText, null, null)]
        //[InlineData("char", CommonDatabaseType.NChar, 1, null)]
        //[InlineData("int", CommonDatabaseType.Int, null, null)]
        //[InlineData("integer", CommonDatabaseType.Int, null, null)]
        //[InlineData("smallint", CommonDatabaseType.SmallInt, null, null)]
        //[InlineData("bigint", CommonDatabaseType.BigInt, null, null)]

        [InlineData("serial", CommonDatabaseType.Int, null, null)]
        // [InlineData("bigserial", DBType.BigInt, null, null)]

        //[InlineData("decimal", CommonDatabaseType.Decimal, null, null)]
        //[InlineData("decimal(3)", CommonDatabaseType.Decimal, 3, 0)]
        //[InlineData("decimal(12,3)", CommonDatabaseType.Decimal, 12, 3)]
        //[InlineData("dec", CommonDatabaseType.Decimal, null, null)]
        //[InlineData("dec(3)", CommonDatabaseType.Decimal, 3, 0)]
        //[InlineData("dec(12,3)", CommonDatabaseType.Decimal, 12, 3)]

        //[InlineData("numeric", CommonDatabaseType.Decimal, null, null)]
        //[InlineData("numeric(3)", CommonDatabaseType.Decimal, 3, 0)]
        //[InlineData("numeric(12,3)", CommonDatabaseType.Decimal, 12, 3)]
        //[InlineData("money", CommonDatabaseType.Decimal, 19, 2)]
        //[InlineData("double precision", CommonDatabaseType.Double, null, null)]
        //[InlineData("float", CommonDatabaseType.Double, null, null)]
        //[InlineData("float(1)", CommonDatabaseType.Float, null, null)]
        //[InlineData("float(24)", CommonDatabaseType.Float, null, null)]
        //[InlineData("float(25)", CommonDatabaseType.Double, null, null)]
        //[InlineData("real", CommonDatabaseType.Float, null, null)]
        //[InlineData("uuid", CommonDatabaseType.Uuid, null, null)]
        //[InlineData("date", CommonDatabaseType.Date, null, null)]
        //[InlineData("time", CommonDatabaseType.Time, 6, null)]
        //[InlineData("time(4)", CommonDatabaseType.Time, 4, null)]
        //[InlineData("time(4) without time zone", CommonDatabaseType.Time, 4, null)]
        //[InlineData("timestamp", CommonDatabaseType.DateTime, 6, null)]
        //[InlineData("timestamp(3)", CommonDatabaseType.DateTime, 3, null)]
        //[InlineData("timestamp without time zone", CommonDatabaseType.DateTime, 6, null)]
        //[InlineData("timestamp with time zone", CommonDatabaseType.DateTimeOffset, 6, null)]
        //[InlineData("timestamp(1) without time zone", CommonDatabaseType.DateTime, 1, null)]
        //[InlineData("timestamp(1) with time zone", CommonDatabaseType.DateTimeOffset, 1, null)]

        public async Task ShouldMapDataTypeTo(string postgresDataType, CommonDatabaseType commonDbType, int? length, int? accuracy)
        {
            _dbConnection.ReCreateDatabase();
            _dbConnection.ExecuteNonQuery($"create table table1(col1 {postgresDataType})");
            var databaseDesc = await _metadataMigrator.GetDatabaseDescriptor(_dbConnection, _migrationSetting);
            var firstColumn = databaseDesc.Tables.First().Columns.First();
            firstColumn.Should().BeEquivalentTo(
                new ColumnDescriptor { Name = "col1", StoreType = postgresDataType, IsNullable = true });


        }

        public void Dispose()
        {
            //_dbConnection.DropDatabaseIfExists();
            _dbConnection?.Dispose();
        }
    }
}
