using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChangeDB
{
    public record DependencyNode<TItem, TKey>
    {
        public TItem Value { get; init; }
        public TKey Key { get; set; }
        public List<TKey> Dependencies { get; set; } = new List<TKey>();

    }

    public static class DependencyNodeExtensions
    {
        public static Task RunDependency<TItem, TKey>(this IEnumerable<TItem> sources, Func<TItem, TKey> keyFunc, Func<TItem, IEnumerable<TKey>> dependencyKeys, Action<TItem> action, int maxTaskCount = 5, int millisecondsDelay = 1000)
        {
            var dependencyNodes = sources.Select(p => new DependencyNode<TItem, TKey>
            {
                Value = p,
                Key = keyFunc(p),
                Dependencies = dependencyKeys(p)?.ToList() ?? new List<TKey>()
            }).OrderBy(p => p.Dependencies.Count);
            return RunDependency(dependencyNodes, action, maxTaskCount, millisecondsDelay);
        }

        public static Task RunDependency<TItem, TKey>(IEnumerable<DependencyNode<TItem, TKey>> nodes, Action<TItem> action, int maxTaskCount,
            int millisecondsDelay = 1000)
        {
            var context = new TaskContext<TItem, TKey>(nodes);
            var allTasks = Enumerable.Range(1, maxTaskCount)
                .Select(p => Task.Run(() => { RunTask(context, action, millisecondsDelay); })).ToArray();
            Task.WaitAll(allTasks.ToArray());
            return Task.CompletedTask;

        }

        private class TaskContext<TItem, TKey>
        {
            public TaskContext(IEnumerable<DependencyNode<TItem, TKey>> nodes)
            {
                foreach (var node in nodes)
                {
                    Queue.Enqueue(node);
                }
            }
            public ConcurrentQueue<DependencyNode<TItem, TKey>> Queue { get; } = new ConcurrentQueue<DependencyNode<TItem, TKey>>();

            public ConcurrentBag<TKey> Done { get; } = new ConcurrentBag<TKey>();
        }



        private static void RunTask<TItem, TKey>(TaskContext<TItem, TKey> context, Action<TItem> action, int millisecondsDelay)
        {
            while (context.Queue.TryDequeue(out var item))
            {
                if (CanRun(item))
                {
                    //Console.WriteLine($"[{Task.CurrentId}] starting {item.Key}--{string.Join(",",item.Dependencies)}");
                    action(item.Value);
                    context.Done.Add(item.Key);
                    //Console.WriteLine($"[{Task.CurrentId}] done {item.Key}");
                }
                else
                {
                    //Console.WriteLine($"[{Task.CurrentId}] waiting {item.Key}--{string.Join(",",item.Dependencies)}");
                    context.Queue.Enqueue(item);
                    Task.Delay(millisecondsDelay).Wait();
                }
            }


            bool CanRun(DependencyNode<TItem, TKey> item)
            {
                if (item.Dependencies == null || item.Dependencies.Count == 0)
                {
                    return true;
                }
                return item.Dependencies.All(p => p.Equals(item.Key) || context.Done.Contains(p));
            }
        }
    }

}
