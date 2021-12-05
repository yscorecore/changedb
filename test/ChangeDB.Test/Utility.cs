using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

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

        public static string NewPassword(int length = 16)
        {
            Random random = new Random((int)DateTime.Now.Ticks);
            StringBuilder passwordBuilder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                passwordBuilder.Append(FullCode[random.Next(FullCode.Length)]);
            }
            return passwordBuilder.ToString();
        }
        public static IDisposable WithDotnetEnv(string envName)
        {
            return new EnvnameStore(envName);
        }
        class EnvnameStore : IDisposable
        {
            const string ASPNETCORE_ENVKEY = "DOTNET_ENVIRONMENT";
            public EnvnameStore(string envName)
            {
                this.backupEnvName = Environment.GetEnvironmentVariable(ASPNETCORE_ENVKEY);
                Environment.SetEnvironmentVariable(ASPNETCORE_ENVKEY, envName);
            }
            private readonly string backupEnvName;
            public void Dispose()
            {
                Environment.SetEnvironmentVariable(ASPNETCORE_ENVKEY, backupEnvName);
            }
        }
    }
}
