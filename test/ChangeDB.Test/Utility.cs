using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace ChangeDB
{
    public static class Utility
    {
        public static uint GetAvailableTcpPort(uint start = 1024, uint stop = IPEndPoint.MaxPort)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] endPoints = ipGlobalProperties.GetActiveTcpListeners();
            for (uint i = start; i <= stop; i++)
            {
                if (endPoints.All(p => p.Port != i))
                {
                    return i;
                }
            }
            throw new ApplicationException("Not able to find a free TCP port.");
        }
        const string FullCode = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";


        public static string RandomDatabaseName()
        {
            var random = new Random((int)DateTime.Now.Ticks);
            return $"testdb_{random.Next(100000):d6}";
        }


    }
}
