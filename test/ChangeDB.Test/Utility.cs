using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace ChangeDB
{
    public static class Utility
    {
        public static int GetRandomTcpPort(int start = 1024, int stop = IPEndPoint.MaxPort)
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var endPoints = ipGlobalProperties.GetActiveTcpListeners();
            var allPorts = endPoints.Select(p => p.Port).ToHashSet();
            var random = new Random();
            while (true)
            {
                var val = random.Next(start, stop);
                if (!allPorts.Contains(val))
                {
                    return val;
                }
            }
        }

        public static string RandomDatabaseName()
        {
            var random = new Random((int)DateTime.Now.Ticks);
            return $"testdb_{random.Next(100000):d6}";
        }
    }
}
