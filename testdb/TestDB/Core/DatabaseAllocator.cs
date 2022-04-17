using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TestDB.Core;

namespace TestDB
{
    public class DatabaseAllocator : IDisposable, IAsyncDisposable, IDatabaseAllocator
    {
        private readonly TemplateDatabaseAllocator templateDatabaseManager;
        private readonly CachedDatabaseAllocator cachedDatabaseManager;
        private readonly DefaultDatabaseAllocator defaultDatabaseManager;
        private readonly IDatabaseServiceProvider serviceProvider;
        private readonly bool cachedDatabase = false;

        public DatabaseAllocator(string connectionTemplate, IDatabaseServiceProvider serviceProvider, bool cachedDatabase = true)
        {
            this.templateDatabaseManager = new TemplateDatabaseAllocator( connectionTemplate, serviceProvider);
            this.cachedDatabaseManager = new CachedDatabaseAllocator( connectionTemplate, serviceProvider);
            this.defaultDatabaseManager = new DefaultDatabaseAllocator( connectionTemplate, serviceProvider);
            this.cachedDatabase = cachedDatabase;
            this.serviceProvider = serviceProvider;
        }




        public void Dispose()
        {
            Parallel.ForEach(
                new IDisposable[]
                {
                    templateDatabaseManager,
                    cachedDatabaseManager,
                    defaultDatabaseManager
                },
                p => p.Dispose());
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Run(Dispose);
        }

        public IDatabase FromScriptFile(string sqlScriptFile, bool readOnly = true)
        {
            if (cachedDatabase)
            {
                if (readOnly)
                {
                    return templateDatabaseManager.GetOrCreateTemplateFromScriptFile(sqlScriptFile);
                }
                else
                {
                    if (serviceProvider.SupportFastClone && CalcTotalSqlSize(sqlScriptFile) > serviceProvider.FastClonedMinimalSqlSize)
                    {
                        return templateDatabaseManager.CloneTemplateFromScriptFile(sqlScriptFile);
                    }
                    else
                    {
                        return cachedDatabaseManager.CreateFromScriptFile(sqlScriptFile);
                    }
                }
            }
            else
            {
                return defaultDatabaseManager.FromScriptFile(sqlScriptFile, readOnly);
            }
        }

        public IDatabase FromSqls(IEnumerable<string> initSqls, bool readOnly = true)
        {
            if (cachedDatabase)
            {
                if (readOnly)
                {
                    return templateDatabaseManager.GetOrCreateTemplateFromSqls(initSqls);
                }
                else
                {
                    if (serviceProvider.SupportFastClone && CalcTotalSqlSize(initSqls) > serviceProvider.FastClonedMinimalSqlSize)
                    {
                        return templateDatabaseManager.CloneTemplateFromSqls(initSqls);
                    }
                    else
                    {
                        return cachedDatabaseManager.CreateFromSqlScripts(initSqls);
                    }
                }
            }
            else
            {
                return defaultDatabaseManager.FromSqls(initSqls, readOnly);
            }
        }

        public IDatabase RequestDatabase()
        {
            return defaultDatabaseManager.RequestDatabase();
        }
        private long CalcTotalSqlSize(IEnumerable<string> initSqls)
        {
            if (initSqls == null) return 0;
            return initSqls.Where(p => p != null).Select(p => p.Length).Sum();
        }
        private long CalcTotalSqlSize(string sqlScriptFile)
        {
            return new FileInfo(sqlScriptFile).Length;
        }
    }
}
