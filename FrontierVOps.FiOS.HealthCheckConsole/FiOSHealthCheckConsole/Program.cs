using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.FiOS.Servers.Controllers;
using FrontierVOps.FiOS.Servers.Objects;
using FrontierVOps.Common;
using FrontierVOps.FiOS.HealthCheck.DataObjects;
using Microsoft.Web.Administration;

namespace FiOSHealthCheckConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            HTMLFormatter formatter = new HTMLFormatter();

            formatter.SetRole(ServerRole.NSP.ToString());

            formatter.BeginTable("NSPTXCAWAPV01");
            formatter.AddStatusRow("Server Status", StatusResult.Ok);
            formatter.AddStatusRow("Web Services", StatusResult.Error);
            List<string> errors = new List<string>();
            errors.Add("Config file mismatch with NSPTXCAWAPV02");
            errors.Add("Web API returned error");
            formatter.AddErrorDescriptionRows(errors);

            formatter.AddStatusRow("Database", StatusResult.Error);
            errors.Clear();
            errors.Add("Missing data in table");
            formatter.AddErrorDescriptionRows(errors);
            formatter.EndTable();

            formatter.BeginTable("NSPTXCAWAPV02");
            formatter.AddStatusRow("Server Status", StatusResult.Ok);
            formatter.AddStatusRow("Web Services", StatusResult.Error);
            errors.Clear();
            errors.Add("Config file mismatch with NSPTXCAWAPV01");
            formatter.AddErrorDescriptionRows(errors);
            formatter.AddStatusRow("Database", StatusResult.Ok);
            formatter.EndTable();

            formatter.BeginTable("NSPTXCAWAPV03");
            formatter.AddStatusRow("Server Status", StatusResult.Critical);
            errors.Clear();
            errors.Add("Server is unreachable.");
            formatter.AddErrorDescriptionRows(errors);
            formatter.AddStatusRow("Web Services", StatusResult.Ok);
            formatter.AddStatusRow("Database", StatusResult.Ok);
            formatter.EndTable();


            Toolset.SendEmail("mailrelay.corp.pvt", null, 25, false, "HealthCheck Test Email", formatter.ToString(), "FiOSHealthCheck@ftr.com", new string[1] { "cyy132@ftr.com" }, null);

            var servers = ServerConfigMgr.GetServers().ToList();
            foreach (var server in servers.Where(x => x.HostRole == ServerRole.IMG))
            {
                if (server is FiOSDbServer)
                {
                    Console.WriteLine("\nDATABASE:\n");
                    Console.WriteLine((server as FiOSDbServer).DatabaseType);                
                }
                else if (server is FiOSWebServer)
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

                try
                {
                    if (server is FiOSWebServer)
                    {
                        using (var iisMgr = new IISServerMgr(server))
                        {
                            try
                            {
                                var Sites = iisMgr.GetSiteCollection();
                                foreach (var site in Sites)
                                {
                                    Console.WriteLine("Site: {0} - {1} - {2}", site.Name, site.ServerAutoStart, site.State);

                                    foreach (var app in site.Applications)
                                    {
                                        Console.WriteLine("App Pool: {0} - {1}", app.ApplicationPoolName, app.Path);

                                        foreach (var vd in app.VirtualDirectories)
                                        {
                                            Console.WriteLine("Virtual Dir: {0} - {1}", vd.Path, vd.PhysicalPath);
                                            foreach (var attr in vd.Attributes)
                                            {
                                                Console.WriteLine("Attribute: {0} - {1}", attr.Name, attr.Value);
                                            }

                                            foreach (var ce in vd.ChildElements)
                                            {
                                                foreach(var attr in ce.Attributes)
                                                {
                                                    Console.WriteLine("Child Attribute: {0} - {1}", attr.Name, attr.Value);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Failed to get Sites for {0}. {1}", server.HostName, ex.Message);
                                Console.ResetColor();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("General Error: {0}", ex.Message);
                    Console.ResetColor();
                }
            }

            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadLine();
        }
    }
}
