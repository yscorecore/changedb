using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChangeDB.Migration;

namespace ChangeDB
{
    public record AgentContext : IDisposable, IAsyncDisposable
    {
        public IAgent Agent { get; set; }
        public string ConnectionString { get; init; }
        public DbConnection Connection { get; init; }
        public IEventReporter EventReporter { get; set; }

        public void Dispose()
        {
            Connection?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (Connection != null)
            {
                await Connection.DisposeAsync();
            }
        }
    }



    public static class AgentContextExtensions
    {
        public static AgentContext Fork(this AgentContext context)
        {
            return context with { Connection = context.Agent.ConnectionProvider.CreateConnection(context.ConnectionString) };
        }

        public static void CreateTargetObject(this AgentContext context, string sql, ObjectType objectType, string fullName, string ownerName = null)
        {
            _ = context.Connection.ExecuteNonQuery(sql);

            context.EventReporter?.RaiseObjectCreated(new ObjectInfo { ObjectType = objectType, FullName = fullName, OwnerName = ownerName });

        }

        public static void RaiseObjectCreated(this AgentContext context, ObjectType objectType, string objectName, string ownerName = null)
        {
            // TODO
            //context.EventReporter?.RaiseObjectCreated(objectType, objectName, ownerName);
        }
        public static void RaiseTableDataMigrated(this AgentContext context, TableDataInfo tableDataInfo)
        {
            // TODO
            //context.EventReporter?.RaiseTableDataMigrated(tableDataInfo);
        }

        public static void RaiseTableDataMigrated(this AgentContext context, string table, long totalCount,
            long migratedCount, bool completed)
        {
            //TODO 
            //context.EventReporter?.RaiseTableDataMigrated(table, totalCount, migratedCount, completed);

        }
        public static void RaiseStageChanged(this AgentContext context, StageKind stage)
        {

        }



        public static void RaiseWarning(this AgentContext context, string message)
        {

        }


    }
}
