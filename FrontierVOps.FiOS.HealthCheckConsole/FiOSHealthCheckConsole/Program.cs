using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.FiOS.Servers.Controllers;
using FrontierVOps.FiOS.Servers.Objects;

namespace FiOSHealthCheckConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var server in ServerMgr.GetServers().AsParallel().Where(x => x.HostRole == ServerRole.NSP))
            {
                if (server is FiOSDbServer)
                {
                    Console.WriteLine("\nDATABASE:\n");
                    Console.WriteLine((server as FiOSDbServer).DatabaseType);                
                }
                else if (server is FiOSIISServer)
                {
                    Console.WriteLine("\nIIS SERVER:\n");
                }
                else
                {
                    Console.Write("\nFiOS Server:\n");
                }

                Console.WriteLine(server.HostName);
                Console.WriteLine(server.HostFullName);
                Console.WriteLine(server.HostFunction);
                Console.WriteLine(server.HostLocation);
                Console.WriteLine(server.HostLocationName);
                Console.WriteLine(server.HostName);
                Console.WriteLine(server.HostRole);
                Console.WriteLine(server.IsOnline);
            }

            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadLine();
        }
    }
}
