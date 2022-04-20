using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ChangeDB.Descriptors;
using ChangeDB.Migration;
using FluentAssertions;
using TestDB;
using Xunit;

namespace ChangeDB.Agent.MySql
{

    public class MySqlMetadataMigratorTest : BaseTest, IDisposable
    {
        private readonly IMetadataMigrator _metadataMigrator = MySqlMetadataMigrator.Default;
        private readonly MigrationContext _migrationContext;
        private readonly DbConnection _dbConnection;
        private readonly IDatabase _database;

        [Obsolete]
        public MySqlMetadataMigratorTest()
        {
            _database = CreateDatabase(false);
            _dbConnection = _database.Connection;
            _migrationContext = new MigrationContext
            {
                TargetConnection = _dbConnection,
                SourceConnection = _dbConnection,
                Source = new AgentRunTimeInfo { Agent = new MySqlAgent() },
                SourceDatabase = new DatabaseInfo() { ConnectionString = _dbConnection.ConnectionString }
            };
        }

        public void Dispose()
        {
            _database.Dispose();
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
               "create table table1(id int ,nm varchar(64));");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Should().HaveCount(1);
            databaseDesc.Tables.First().Should().Match<TableDescriptor>(p => p.Schema == null && p.Name == "table1");
        }

        [Fact]
        public async Task ShouldIncludeNullableColumnInfoWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
               "create table table1(id int, nm int NOT NULL);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Should().HaveCount(1);
            databaseDesc.Tables.Should().ContainSingle()
                .And.ContainEquivalentOf(
                new TableDescriptor
                {
                    Name = "table1",
                    Schema = null,
                    Columns = new List<ColumnDescriptor>
                    {
                        new ColumnDescriptor { Name="id", IsNullable=true, DataType = DataTypeDescriptor.Int(), IsStored = false},
                        new ColumnDescriptor { Name="nm", IsNullable=false, DataType = DataTypeDescriptor.Int(), IsStored = false}
                    }
                });
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
            databaseDesc.Tables.Single(p => p.Name == "table2").ForeignKeys.Should().HaveCount(1);
            databaseDesc.Tables.SelectMany(p => p.ForeignKeys).First().Should().BeEquivalentTo(new ForeignKeyDescriptor
            {
                Name = "table2_id1_fkey",
                OnDelete = ReferentialAction.NoAction,
                PrincipalTable = "table1",
                PrincipalSchema = null,
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
                    PrincipalSchema = null,
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
                "create table table1(id int primary key AUTO_INCREMENT);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema = null,
                    PrimaryKey = new PrimaryKeyDescriptor
                    {
                        Columns = new List<string> { "id" },
                        Name = "PRIMARY"
                    },
                    Columns = new List<ColumnDescriptor>
                    {
                       new ColumnDescriptor
                       {
                            Name="id", DataType = DataTypeDescriptor.Int(), IsIdentity =true,IsStored= false,
                            IdentityInfo = new IdentityDescriptor
                            {
                                IsCyclic =false,
                                StartValue=1,
                                IncrementBy=1
                            }
                       }
                    }
                });
        }
        [Fact]
        public async Task ShouldIncludeIdentityDescriptorWithStartValueWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key AUTO_INCREMENT,val int);",
                "insert into table1(val) VALUES(234)"
            );
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema = null,
                    PrimaryKey = new PrimaryKeyDescriptor
                    {
                        Columns = new List<string> { "id" },
                        Name = "PRIMARY"
                    },
                    Columns = new List<ColumnDescriptor>
                    {
                        new ColumnDescriptor
                        {
                            Name="id", DataType = DataTypeDescriptor.Int(), IsIdentity =true,IsStored= false,IsNullable= false,
                            IdentityInfo = new IdentityDescriptor
                            {
                                IsCyclic =false,
                                StartValue=1,
                                IncrementBy=1,
                                CurrentValue =1
                            }
                        },
                        new ColumnDescriptor
                        {
                            Name="val", DataType = DataTypeDescriptor.Int(), IsIdentity =false,IsStored= false,IsNullable =true
                        }
                    }
                });
        }
        [Fact]
        public async Task ShouldIncludeIdentityDescriptorWithCurrentValueWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int primary key auto_increment ,val int);",
                   "insert into table1(val) VALUES(123)",
                   "insert into table1(val) VALUES(123)"
                   );
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema = null,
                    PrimaryKey = new PrimaryKeyDescriptor
                    {
                        Columns = new List<string> { "id" },
                        Name = "PRIMARY"
                    },
                    Columns = new List<ColumnDescriptor>
                    {
                       new ColumnDescriptor
                       {
                            Name="id", DataType = DataTypeDescriptor.Int(), IsIdentity =true,IsStored= false,IsNullable= false,
                            IdentityInfo = new IdentityDescriptor
                            {
                                IsCyclic =false,
                                StartValue=1,
                                IncrementBy=1,
                                CurrentValue =2
                            }
                       },
                       new ColumnDescriptor
                       {
                            Name="val", DataType = DataTypeDescriptor.Int(), IsIdentity =false,IsStored= false,IsNullable =true
                       }
                    }
                });
        }

        [Fact]
        public async Task ShouldIncludeUuidColumnWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                   "create table table1(abc binary(16) default (UUID_TO_BIN(UUID())));");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema = null,
                    Columns = new List<ColumnDescriptor>
                    {
                       new ColumnDescriptor{ Name="abc", IsNullable=true, DataType = DataTypeDescriptor.Binary(16),DefaultValue=SqlExpressionDescriptor.FromFunction(Function.Uuid) }
                    }
                });
        }
        [Fact]
        public async Task ShouldIncludeTimestampColumnWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                   "create table table1(id datetime default now());");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema = null,
                    Columns = new List<ColumnDescriptor>
                    {
                       new ColumnDescriptor{ Name="id", IsNullable=true, DataType =DataTypeDescriptor.DateTime(3),DefaultValue = SqlExpressionDescriptor.FromFunction(Function.Now)}
                    }
                });
        }
        [Fact]
        public async Task ShouldIncludeDefaultValueWhenGetDatabaseDescription()
        {
            _dbConnection.ExecuteNonQuery(
                "create table table1(id int NOT NULL default 0,nm varchar(10) default 'abc', val decimal default 1);");
            var databaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            databaseDesc.Tables.Where(p => p.Name == "table1").Single().Should()
                .BeEquivalentTo(new TableDescriptor
                {
                    Name = "table1",
                    Schema = null,
                    Columns = new List<ColumnDescriptor>
                    {
                        new ColumnDescriptor{ Name="id", IsNullable=false, DataType = DataTypeDescriptor.Int(), DefaultValue=SqlExpressionDescriptor.FromConstant(0)},
                        new ColumnDescriptor{ Name="nm", IsNullable=true, DataType=DataTypeDescriptor.Varchar(10), DefaultValue = SqlExpressionDescriptor.FromConstant("abc")},
                        new ColumnDescriptor{ Name="val", IsNullable=true, DataType = DataTypeDescriptor.Decimal(10,0), DefaultValue = SqlExpressionDescriptor.FromConstant(1)}
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
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType = DataTypeDescriptor.Int(), IsNullable = true}
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
                            new ColumnDescriptor { Name="id", DataType = DataTypeDescriptor.Int() }
                        }
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Should().BeEquivalentTo(databaseDesc);
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
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType = DataTypeDescriptor.Int() }
                        },
                         PrimaryKey = new PrimaryKeyDescriptor { Name="PRIMARY", Columns = new List<string>{"id" } },
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
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType = DataTypeDescriptor.Int() }
                        },
                        PrimaryKey = new PrimaryKeyDescriptor { Columns = new List<string>{"id" } },
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            actualDatabaseDesc.Tables.Single().PrimaryKey.Name.Should().Be("PRIMARY");
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
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType = DataTypeDescriptor.Int() }
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
                        Schema=null,
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType = DataTypeDescriptor.Int() },
                            new ColumnDescriptor { Name="nm", DataType = DataTypeDescriptor.Int() }
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
                        Schema=null,
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType = DataTypeDescriptor.Int() }
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
                        Schema= null,
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType = DataTypeDescriptor.Int() },
                             new ColumnDescriptor { Name="nm", DataType = DataTypeDescriptor.Int() }
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
        public async Task ShouldSetColumnNullableWhenMigrateMetadata()
        {
            var databaseDesc = new DatabaseDescriptor()
            {
                Tables = new List<TableDescriptor>
                {
                    new TableDescriptor
                    {
                        Schema=null,
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType = DataTypeDescriptor.Int(),IsNullable = false },
                            new ColumnDescriptor { Name="id2", DataType = DataTypeDescriptor.Int(),IsNullable = true }
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
                        Schema=null,
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
                        Schema=null,
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
                                PrincipalSchema=null,
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
                        Schema=null,
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
                        Schema=null,
                        Name="table2",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id2", DataType = DataTypeDescriptor.Int()},
                             new ColumnDescriptor { Name="nm2", DataType = DataTypeDescriptor.Int()},
                        },
                         ForeignKeys = new List<ForeignKeyDescriptor>
                         {
                             new ForeignKeyDescriptor
                             {
                                Name="foreign_key",
                                PrincipalSchema=null,
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
                        Schema=null,
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                            new ColumnDescriptor { Name="id", DataType = DataTypeDescriptor.Int(), DefaultValue=SqlExpressionDescriptor.FromConstant(1)},
                             new ColumnDescriptor { Name="nm",DataType  = DataTypeDescriptor.Varchar(10),DefaultValue =SqlExpressionDescriptor.FromConstant("abc") },
                             new ColumnDescriptor { Name="used", DataType=DataTypeDescriptor.Boolean(),DefaultValue = SqlExpressionDescriptor.FromConstant(true)},
                             new ColumnDescriptor {Name="rid", DataType = DataTypeDescriptor.Binary(16), DefaultValue = SqlExpressionDescriptor.FromConstant(Function.Uuid)},
                             new ColumnDescriptor { Name="createtime", DataType = DataTypeDescriptor.DateTime(3), DefaultValue = SqlExpressionDescriptor.FromConstant(Function.Now)},
                        },
                        PrimaryKey = new PrimaryKeyDescriptor{ Name="PRIMARY", Columns = new List<string>{"id"}}
                    }
                }
            };
            await _metadataMigrator.MigrateAllTargetMetaData(databaseDesc, _migrationContext);
            var actualDatabaseDesc = await _metadataMigrator.GetSourceDatabaseDescriptor(_migrationContext);
            // var expectedDatabaseDesc = databaseDesc.DeepClone();
            // expectedDatabaseDesc.Tables.SelectMany(p => p.Columns)
            //     .Where(p => !p.DefaultValueSql.StartsWith('(')).Each(c => c.DefaultValueSql = $"({c.DefaultValueSql})");
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
                        Schema=null,
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                           new ColumnDescriptor
                           {
                                Name="id", DataType = DataTypeDescriptor.Int(), IsIdentity =true,
                                IdentityInfo = new IdentityDescriptor
                                {

                                }
                           }
                        },
                        PrimaryKey = new PrimaryKeyDescriptor{ Columns = new List<string>{"id"}, Name = "PRIMARY"}
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
                        Schema=null,
                        Name="table1",
                        Columns =new List<ColumnDescriptor>
                        {
                           new ColumnDescriptor
                           {
                                Name="id", DataType = DataTypeDescriptor.BigInt(), IsIdentity =true,
                                IdentityInfo = new IdentityDescriptor
                                {
                                    StartValue=1,
                                    IncrementBy=1,
                                }
                           }
                        },
                        Uniques = new List<UniqueDescriptor>
                        {
                            new UniqueDescriptor{ Name = "abc", Columns = new List<string>{"id"}}
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
