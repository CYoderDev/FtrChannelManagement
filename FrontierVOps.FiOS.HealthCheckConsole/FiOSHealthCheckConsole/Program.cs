using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.FiOS.Servers.Controllers;
using FrontierVOps.FiOS.Servers.Objects;
using FrontierVOps.FiOS.Servers.Components;
using FrontierVOps.Common;
using FrontierVOps.FiOS.HealthCheck.DataObjects;
using FrontierVOps.FiOS.HealthCheck.Controllers;
using Microsoft.Web.Administration;

namespace FiOSHealthCheckConsole
{
    class Program
    {
        static void Main(string[] args)
        {


            var server = ServerConfigMgr.GetServers().Where(x => x.HostName.ToUpper().Equals("CTXV01PIMGD01")).FirstOrDefault();

            bool isExempt = ServerHealthCheckConfigMgr.IsExempt(server, ExemptionType.HardDrive, "O");


             var winServicesToCheck = ServerHealthCheckConfigMgr.GetWindowsServicesToCheck()
                        .Where
                            (
                                x => x.Servers.Where(y => y.Item1 && y.Item2.HostName == server.HostName).Count() > 0 && x.Roles.Where(y => !y.Item1 && y.Item2 == server.HostRole && y.Item3 == server.HostFunction).Count() == 0
                                    || x.Roles.Where(y => y.Item1 && y.Item2 == server.HostRole && y.Item3 == server.HostFunction).Count() > 0 && x.Servers.Where(y => !y.Item1 && y.Item2.HostName == server.HostName).Count() == 0
                                    || !x.Function.Equals(ServerFunction.Unknown) && (x.Function.Equals(server.HostFunction) && x.Servers.Where(y => !y.Item1 && y.Item2.HostName == server.HostName).Count() + x.Roles.Where(y => !y.Item1 && y.Item2 == server.HostRole && y.Item3 == server.HostFunction).Count() == 0)
                                    || ((x.Roles.Count + x.Servers.Count == 0) && (x.Function.Equals(ServerFunction.Unknown)))
                            ).ToArray();


            //hcWServices = ServerHealthCheckConfigMgr.GetWindowsServicesToCheck().Where(x => x.OnePerGroup).ToArray();

            foreach (var svc in ServerHealthCheckConfigMgr.GetWindowsServicesToCheck().Where(x => x.Name == "WAS"))
            {
                var count = svc.Servers.Where(y => !y.Item1 && y.Item2.HostName == server.HostName).Count();
                var count2 = svc.Roles.Where(y => !y.Item1 && y.Item2 == server.HostRole && y.Item3 == server.HostFunction).Count();
           
                //if (svc.Roles.Count >= 1)
                //svc.Roles.ForEach(x => Console.WriteLine("{3}  - {0} | {1} | {2}", x.Item1, x.Item2, x.Item3, svc.Name));

                bool bol1 = svc.Servers.Where(y => y.Item1 && y.Item2.HostName == server.HostName).Count() > 0 && svc.Roles.Where(y => !y.Item1 && y.Item2 == server.HostRole && y.Item3 == server.HostFunction).Count() == 0;
                bool bol2 = svc.Roles.Where(y => y.Item1 && y.Item2 == server.HostRole && y.Item3 == server.HostFunction).Count() > 0 && svc.Servers.Where(y => !y.Item1 && y.Item2.HostName == server.HostName).Count() == 0;
                bool bol3 = (svc.Function.Equals(server.HostFunction) && svc.Servers.Where(y => !y.Item1 && y.Item2.HostName == server.HostName).Count() + svc.Roles.Where(y => !y.Item1 && y.Item2 == server.HostRole && y.Item3 == server.HostFunction).Count() == 0);
                bool bol4 = ((svc.Roles.Count + svc.Servers.Count == 0) && (svc.Function.Equals(ServerFunction.Unknown)));
                bool bol5 = svc.Servers.Where(y => y.Item1 && y.Item2.HostName == server.HostName).Count() > 0 && svc.Roles.Where(y => !y.Item1 && y.Item2 == server.HostRole && y.Item3 == server.HostFunction).Count() == 0
                                    || svc.Roles.Where(y => y.Item1 && y.Item2 == server.HostRole && y.Item3 == server.HostFunction).Count() > 0 && svc.Servers.Where(y => !y.Item1 && y.Item2.HostName == server.HostName).Count() == 0
                                    || (svc.Function.Equals(server.HostFunction) && svc.Servers.Where(y => !y.Item1 && y.Item2.HostName == server.HostName).Count() + svc.Roles.Where(y => !y.Item1 && y.Item2 == server.HostRole && y.Item3 == server.HostFunction).Count() == 0)
                                    || ((svc.Roles.Count + svc.Servers.Count == 0) && (svc.Function.Equals(ServerFunction.Unknown)));

                Console.WriteLine("{0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8}", svc.Name, count, server.HostFunction, server.HostRole, count2, bol1, bol2, bol3, bol4, bol5);
                
            }

            //var roleCheck = ServerHealthCheckConfigMgr.GetWindowsServicesToCheck().Where(x => x.Roles.Where(y => y.Item1 && y.Item2 == svr.HostRole).Count() > 0);

            foreach (var service in winServicesToCheck)
            {
                Console.WriteLine("Name:\t\t{0}", service.Name);
                Console.WriteLine("Display:\t{0}", service.DisplayName);
                if (service.CheckStatus.Count > 0)
                    service.CheckStatus.ForEach(x => Console.WriteLine("Status:\t\t{0}", x.ToString()));
                if (service.CheckStartupType.Count > 0)
                    service.CheckStartupType.ForEach(x => Console.WriteLine("StartupType:\t{0}", x.ToString()));
                if (service.CheckLogonAs.Count > 0)
                {
                    service.CheckLogonAs.ForEach(x => Console.WriteLine("LogonAs:\t{0} - {1}", x, GenericChecks.GetWindowsServiceLogonAccount("B1ZHN32", service.Name)));
                    
                }
                if (service.Roles.Count > 0)
                {
                    service.Roles.ForEach((x) =>
                    {
                        if (x.Item1)
                            Console.WriteLine("INCLUDE ROLES");
                        else
                            Console.WriteLine("EXCLUDE ROLES");
                        Console.WriteLine("Role:\t\t{0}", x.Item2.ToString());
                        Console.WriteLine("Function:\t{0}", x.Item3.ToString());
                    });
                }
                if (service.Servers.Count > 0)
                {
                    service.Servers.ForEach((x) =>
                        {
                            if (x.Item1)
                                Console.WriteLine("INCLUDE SERVERS");
                            else
                                Console.WriteLine("EXCLUDE SERVERS");

                            Console.WriteLine("Server Name:\t{0}", x.Item2.HostName);
                            Console.WriteLine("Server IP:\t{0}", x.Item2.IPAddress);
                            Console.WriteLine("Server Location:\t{0}", x.Item2.HostLocation.ToString());
                        });
                }
                Console.WriteLine("OnePerGroup:\t{0}", service.OnePerGroup);
                Console.WriteLine();
            }
            Console.WriteLine("Press Enter to Continue...");
            Console.ReadLine();

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
            foreach (var svr in servers.Where(x => x.HostRole == ServerRole.IMG))
            {
                if (svr is FiOSDbServer)
                {
                    Console.WriteLine("\nDATABASE:\n");
                    Console.WriteLine((svr as FiOSDbServer).DatabaseType);                
                }
                else if (svr is FiOSWebServer)
                {
                    Console.WriteLine("\nIIS SERVER:\n");
                }
                else
                {
                    Console.Write("\nFiOS Server:\n");
                }

                Console.WriteLine(svr.HostName);
                Console.WriteLine(svr.HostFullName);
                Console.WriteLine(svr.HostFunction);
                Console.WriteLine(svr.HostLocation);
                Console.WriteLine(svr.HostLocationName);
                Console.WriteLine(svr.HostName);
                Console.WriteLine(svr.HostRole);
                Console.WriteLine(svr.IsOnline);

                try
                {
                    if (svr is FiOSWebServer)
                    {
                        using (var iisMgr = new IISServerMgr(svr))
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
                                Console.WriteLine("Failed to get Sites for {0}. {1}", svr.HostName, ex.Message);
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
