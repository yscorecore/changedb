using System.Threading.Tasks;

namespace ChangeDB.Migration
{
    public interface IMetadataMigrator
    {
        Task<DatabaseDescriptor> GetDatabaseDescriptor(AgentContext agentContext);

        Task PreMigrateMetadata(DatabaseDescriptor databaseDescriptor, AgentContext agentContext);

        Task PostMigrateMetadata(DatabaseDescriptor databaseDescriptor, AgentContext agentContext);
        public async Task MigrateAllMetaData(DatabaseDescriptor databaseDescriptor, AgentContext agentContext)
        {
            await PreMigrateMetadata(databaseDescriptor, agentContext);
            await PostMigrateMetadata(databaseDescriptor, agentContext);
        }
    }
}
