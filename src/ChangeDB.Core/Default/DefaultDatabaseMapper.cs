using System.Collections.Generic;
using System.Threading.Tasks;
using ChangeDB.Mapper;
using ChangeDB.Migration;
using ChangeDB.Migration.Mapper;

namespace ChangeDB.Default
{
    public class DefaultDatabaseMapper : IDatabaseMapper
    {
        public Task<DatabaseDescriptorMapper> MapDatabase(DatabaseDescriptor sourceDatabase, AgentSetting agentSetting)
        {
            var mapper = CreateDefaultMapper(sourceDatabase);
            return Task.FromResult(mapper);
        }

        private DatabaseDescriptorMapper CreateDefaultMapper(DatabaseDescriptor sourceDatabase)
        {
            var databaseMapper = new DatabaseDescriptorMapper {Source = sourceDatabase, Target = new DatabaseDescriptor()};
            // sequence
            foreach (var seq in sourceDatabase.Sequences)
            {
                var seqMapper = new SequenceDescriptorMapper {Source = seq, Target = seq.DeepClone()};
                databaseMapper.SequenceMappers.Add(seqMapper);
                databaseMapper.Target.Sequences.Add(seqMapper.Target);
            }
            // table
            foreach (var table in sourceDatabase.Tables)
            {
                var tableMapper = new TableDescriptorMapper
                {
                    Source = table,
                    Target = new TableDescriptor {Name = table.Name, Comment = table.Comment, Schema = table.Schema}
                };
                foreach (var column in table.Columns)
                {
                    var columnMapper = new ColumnDescriptorMapper {Source = column, Target = column.DeepClone()};
                    tableMapper.ColumnMappers.Add(columnMapper);
                    tableMapper.Target.Columns.Add(columnMapper.Target);
                }
                
                databaseMapper.TableMappers.Add(tableMapper);
                databaseMapper.Target.Tables.Add(tableMapper.Target);
            }
            
            return databaseMapper;
        }
    }
}
