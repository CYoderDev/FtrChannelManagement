using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.Administration;
using System.Management;
using FrontierVOps.Common;
using FrontierVOps.FiOS.HealthCheck.DataObjects;
using FrontierVOps.FiOS.Servers.Controllers;
using FrontierVOps.FiOS.Servers.Components;
using FrontierVOps.FiOS.Servers.Objects;

namespace FrontierVOps.FiOS.HealthCheck.Controllers
{
    public class GenericChecks
    {
        /// <summary>
        /// List of all windows services to check
        /// </summary>
        public List<HCWinService> WindowsServicesToCheck { get; set; }

        public GenericChecks()
        {
            this.WindowsServicesToCheck = new List<HCWinService>();
        }

        /// <summary>
        /// Performs all server checks on the server
        /// </summary>
        /// <param name="Server">FiOS Server</param>
        /// <returns>A health rollup containing all general server checks</returns>
        public async Task<HealthRollup> PerformServerCheck(FiOSServer Server)
        {
            var hru = new HealthRollup();
            var hce = new HealthCheckError();
            hru.Server = Server;

            if (!Server.IsActive)
            {
                hce.Result = StatusResult.Skipped;
                hce.HCType = HealthCheckType.GeneralServer;
                if (Server.IsOnline)
                    hce.Error.Add("Server is marked as inactive but is currently online.");
                else
                    hce.Error.Add("Server is offline");

                hru.Errors.Add(hce);
                return await Task.FromResult<HealthRollup>(hru);
            }

            if (!Server.IsOnline)
            {
                hce.Error.Add(string.Format("{0} - Server is offline or unreachable via it's FQDN.", StatusResult.Critical));
                hce.Result = StatusResult.Critical;
                hru.Errors.Add(hce);
                return await Task.FromResult<HealthRollup>(hru);
            }

            #region CheckHardDrives
            try
            {
                var hdds = await HardDrive.GetHardDriveAsync(Server.HostFullName);

                for (int i = 0; i < hdds.Length; i++)
                {
                    var result = StatusResult.Ok;

                    //If server is exempt, skip
                    if (ServerHealthCheckConfigMgr.IsExempt(Server, ExemptionType.HardDrive, hdds[i].DriveLetter))
                    {
                        hce.Error.Add(string.Format("Skipped hard drive checks for drive letter {0}. {1}GB Remaining.", hdds[i].DriveLetter, ((decimal)hdds[i].FreeSpace / 1024M / 1024M / 1024M).ToString("N1")));
                        continue;
                    }

                    //Check Hard Drive Space
                    try
                    {
                        result = await checkHDDSpaceAsync(hdds[i]);

                        if (result != StatusResult.Ok)
                        {
                            hce.Error.Add(string.Format("{0} - Disk {1} currently only has {2}GB of {3}GB of space remaining. SN: {4}.", 
                                result, 
                                hdds[i].Name, 
                                ((decimal)hdds[i].FreeSpace / 1024M / 1024M / 1024M).ToString("N1"), 
                                ((decimal)hdds[i].Capacity / 1024M / 1024M / 1024M).ToString("N1"), 
                                hdds[i].SerialNumber));

                            hce.Result = getCorrectStatusResult(hce.Result, result);
                        }
                    }
                    catch (Exception ex)
                    {
                        hce.Error.Add(string.Format("{0} - Failed to get available disk space for {2}. Exception: {1}", StatusResult.Error, ex.Message, hdds[i].DriveLetter));
                        hce.Result = getCorrectStatusResult(hce.Result, StatusResult.Error);
                    }

                    //Check Hard Drive Status
                    try
                    {
                        result = await checkHDDStatusAsync(hdds[i]);

                        hce.Result = getCorrectStatusResult(hce.Result, result);

                        if (result != StatusResult.Ok)
                            hce.Error.Add(string.Format("{0} - Disk {1} is currently in a {2} state. Type: {3} - SN: {4}", result, hdds[i].Name, hdds[i].Status, hdds[i].DriveType, hdds[i].SerialNumber));

                        hce.Result = getCorrectStatusResult(hce.Result, result);
                    }
                    catch (Exception ex)
                    {
                        hce.Error.Add(string.Format("{0} - Failed to get hard drive status. Exception: {1}", StatusResult.Error, ex.Message));
                        hce.Result = getCorrectStatusResult(hce.Result, StatusResult.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                hce.Error.Add(string.Format("{0} - Failed to perform hard drive check. {1}", StatusResult.Error, ex.Message));
                hce.Result = getCorrectStatusResult(hce.Result, StatusResult.Error);
            }
            #endregion CheckHardDrives

            hru.Errors.Add(hce);

            return await Task.FromResult<HealthRollup>(hru);
        }


        /// <summary>
        /// Provides the highest level of importance as the status result. Critical being the highest.
        /// </summary>
        /// <param name="oldResult">The current result</param>
        /// <param name="newResult">The new result</param>
        /// <returns>The highest level of status result</returns>
        internal static StatusResult getCorrectStatusResult(StatusResult oldResult, StatusResult newResult)
        {
            if (newResult == StatusResult.Critical || oldResult == StatusResult.Critical)
                return StatusResult.Critical;
            else if (newResult == StatusResult.Error || oldResult == StatusResult.Error)
                return StatusResult.Error;
            else if (newResult == StatusResult.Warning || oldResult == StatusResult.Warning)
                return StatusResult.Warning;
            else
                return StatusResult.Ok;
        }

        /// <summary>
        /// Check the hard drive space async using the percentage remaining and physical space remaining.
        /// </summary>
        /// <param name="hdd">Server hard drive</param>
        /// <returns>Status result of the drive space.</returns>
        private Task<StatusResult> checkHDDSpaceAsync(HardDrive hdd)
        {
            var percRemaining = ((double)hdd.FreeSpace / (double)hdd.Capacity) * 100;
            var freeInGB = (decimal)hdd.FreeSpace / 1024M / 1024M / 1024M;

            //If less than one percent remaining, or less than 1 GB remaining, return critical.
            if (percRemaining < 1 || (hdd.Capacity > 1000000000 && freeInGB < 1))
                return Task.FromResult<StatusResult>(StatusResult.Critical);
            //If less than 5 percent remaining, or less than 10 GB remaining, return error.
            else if (percRemaining < 5 || (hdd.Capacity > 10000000000 && freeInGB < 10))
                return Task.FromResult<StatusResult>(StatusResult.Error);
            //If less than 10 percent remaining, or less than 25 GB remaining, return warning.
            else if (percRemaining < 10 || (hdd.Capacity > 250000000000 && freeInGB < 25))
                return Task.FromResult<StatusResult>(StatusResult.Warning);
            else
                return Task.FromResult<StatusResult>(StatusResult.Ok);
        }

        /// <summary>
        /// Checks the hard drive status async. 
        /// </summary>
        /// <param name="hdd">Server hard drive.</param>
        /// <returns>Status result of the hard drive status.</returns>
        private Task<StatusResult> checkHDDStatusAsync(HardDrive hdd)
        {
            switch (hdd.Status)
            {
                case HardDrive.DriveStatus.Unknown:
                case HardDrive.DriveStatus.OK:
                case HardDrive.DriveStatus.Starting:
                    break;
                case HardDrive.DriveStatus.LostComm:
                case HardDrive.DriveStatus.NoContact:
                case HardDrive.DriveStatus.NonRecover:
                    return Task.FromResult<StatusResult>(StatusResult.Critical);
                case HardDrive.DriveStatus.Degraded:
                case HardDrive.DriveStatus.Error:
                    return Task.FromResult<StatusResult>(StatusResult.Error);
                case HardDrive.DriveStatus.PredFail:
                case HardDrive.DriveStatus.Service:
                case HardDrive.DriveStatus.Stressed:
                case HardDrive.DriveStatus.Stopping:
                    return Task.FromResult<StatusResult>(StatusResult.Warning);
            }

            return Task.FromResult<StatusResult>(StatusResult.Ok);
        }

        public static async Task<IEnumerable<HealthRollup>> CheckWindowsServices(FiOSServer Server, HCWinService[] ServicesToCheck)
        {
            var hruList = new List<HealthRollup>();

            var hcErrors = new List<HealthCheckError>();

            if (!Server.IsOnline || !Server.IsActive)
            {             
                var svcFuncts = ServicesToCheck.GroupBy(x => x.Function)
                    .Select(x => new 
                        {
                            x.Key, 
                            RoleFunctions = x.Where(y => y.Function.Equals(x.Key)).SelectMany(y => y.Roles.Select(z => z.Item3)).ToList(),
                            ServerCount = x.Where(y => y.Function.Equals(x.Key)).SelectMany(y => y.Servers).Count(),
                        }).ToList();

                svcFuncts.ForEach(x =>
                    {
                        var hcErr = new HealthCheckError();

                        if (Server.IsActive)
                            hcErr.Result = StatusResult.Critical;
                        else
                            hcErr.Result = StatusResult.Skipped;

                        if (x.Key == ServerFunction.Database || (x.RoleFunctions.Count > 0 && x.RoleFunctions.Any(y => y.Equals(ServerFunction.Database))))
                            hcErr.HCType = HealthCheckType.Database;
                        else if (x.Key == ServerFunction.Web || (x.RoleFunctions.Count > 0 && x.RoleFunctions.Any(y => y.Equals(ServerFunction.Web))))
                            hcErr.HCType = HealthCheckType.IIS;

                        if (!(hcErr.HCType == HealthCheckType.GeneralServer) && Server.IsActive)
                            hcErr.Error.Add(string.Format("Cannot check windows services because the server is offline."));
                        hcErrors.Add(hcErr);
                    });
                var hru = new HealthRollup() { Server = Server, Errors = hcErrors };
                hruList.Add(hru);
                return await Task.FromResult<IEnumerable<HealthRollup>>(hruList);
            }


            hcErrors = await checkWindowsServices(Server.HostFullName, ServicesToCheck.Where(x => !x.OnePerGroup).ToArray());

            foreach (var result in hcErrors.GroupBy(x => x.HCType).Select(x => new HealthCheckError()
                {
                    HCType = x.Key,
                    Result = x.Where(y => y.HCType.Equals(x.Key)).Select(y => y.Result).Max(),
                    Error = x.Where(y => y.HCType.Equals(x.Key)).SelectMany(y => y.Error).ToList()
                }))
            {
                var hru = new HealthRollup();
                hru.Server = Server;
                hru.Errors.Add(result);
                hruList.Add(hru);
            }

            return await Task.FromResult<IEnumerable<HealthRollup>>(hruList);
        }

        /// <summary>
        /// Check if a windows service meets expected parameters on at least one of an array of servers
        /// </summary>
        /// <param name="ServerNames">The group of server names to check services</param>
        /// <param name="ServiceToCheck">The services that only one of the servers in the group must meet the parameters</param>
        /// <returns></returns>
        public static async Task<List<HealthCheckError>> CheckWindowsServices(string[] ServerNames, HCWinService ServiceToCheck)
        {
            List<HealthCheckError> lstHce = new List<HealthCheckError>();
            if (!ServiceToCheck.OnePerGroup)
                return lstHce;

            for (int i = 0; i < ServerNames.Length; i++)
            {
                foreach (var result in await checkWindowsServices(ServerNames[i], new HCWinService[1] { ServiceToCheck }))
                {
                    if (result.Error.Count < 1)
                    {
                        lstHce.Clear();
                        lstHce.Add(result);
                        return lstHce;
                    }
                    else
                    {
                        var errs = new List<string>();
                        result.Error.ForEach(x => errs.Add(string.Format("{0}. At least one server within the same \"cluster\" as {1} requires this to be corrected.", x, ServerNames[i])));
                        result.Error.Clear();
                        result.Error.AddRange(errs);
                        lstHce.Add(result);
                    }
                }
            }
            return lstHce;
        }

        /// <summary>
        /// Checks windows services.
        /// </summary>
        /// <param name="ServerName">Name of the server being checked</param>
        /// <param name="ServicesToCheck">Array of services to check</param>
        /// <returns>A tuple containing the highest level of status result, and a list of errors.</returns>
        private static Task<List<HealthCheckError>> checkWindowsServices(string ServerName, HCWinService[] ServicesToCheck)
        {
            var errors = new List<HealthCheckError>();
            var exceptions = new List<Exception>();
            

            for (int i = 0; i < ServicesToCheck.Length; i++ )
            {
                try
                {
                    var hcErr = new HealthCheckError();
                    hcErr.Result = StatusResult.Ok;
                    using (var svc = new ServiceController(ServicesToCheck[i].Name, ServerName))
                    {
                        StringBuilder sb = new StringBuilder();

                        if (ServicesToCheck[i].Function == ServerFunction.Database || (ServicesToCheck[i].Roles.Count > 0 && ServicesToCheck[i].Roles.Any(x => x.Item3.Equals(ServerFunction.Database))))
                            hcErr.HCType = HealthCheckType.Database;
                        else if (ServicesToCheck[i].Function == ServerFunction.Web || (ServicesToCheck[i].Roles.Count > 0 && ServicesToCheck[i].Roles.Any(x => x.Item3.Equals(ServerFunction.Web))))
                            hcErr.HCType = HealthCheckType.IIS;

                        //Check Status
                        try
                        {
                            if (ServicesToCheck[i].CheckStatus.Count > 0 && !ServicesToCheck[i].CheckStatus.Contains(svc.Status))
                            {
                                hcErr.Result = getCorrectStatusResult(hcErr.Result, StatusResult.Error);
                                sb.AppendFormat("{0} - Windows service \"{1}\" is in a {2} state.", StatusResult.Error, svc.ServiceName, svc.Status);
                                sb.AppendFormat(" Expected state(s): {0}.", string.Join(",", ServicesToCheck[i].CheckStatus));
                                hcErr.Error.Add(sb.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            hcErr.Result = getCorrectStatusResult(hcErr.Result, StatusResult.Critical);
                            hcErr.Error.Add(string.Format("{0} - Failed to check windows service status for \"{1}\" on \"{2}\". {3}", StatusResult.Critical, ServicesToCheck[i].Name, ServerName, ex.Message));
                            if (ex.Message.ToUpper().Contains("WAS NOT FOUND"))
                            {
                                errors.Add(hcErr);
                                continue;
                            }
                        }

                        //Check StartupType. Can be deprecated after .net upgrade, and instead use the ServiceController.
                        try
                        {
                            if (ServicesToCheck[i].CheckStartupType.Count > 0)
                            {
                                var stType = GetWindowsServiceStartMode(ServerName, svc.ServiceName);
                                if (!ServicesToCheck[i].CheckStartupType.Contains(stType))
                                {
                                    hcErr.Result = getCorrectStatusResult(hcErr.Result, StatusResult.Warning);
                                    sb.Clear();
                                    sb.AppendFormat("{0} - Windows service \"{1}\" startup type is set to {2}.", StatusResult.Warning, svc.ServiceName, stType);
                                    sb.AppendFormat(" Expected startup type(s): {0}", string.Join(",", ServicesToCheck[i].CheckStartupType));
                                    hcErr.Error.Add(sb.ToString());
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            hcErr.Result = getCorrectStatusResult(hcErr.Result, StatusResult.Critical);
                            hcErr.Error.Add(string.Format("{0} - Failed to check windows service \"{1}\" on \"{2}\". {3}", StatusResult.Critical, ServicesToCheck[i].Name, ServerName, ex.Message));
                            errors.Add(hcErr);
                            continue;
                        }

                        //Check LogonAs Account.
                        try
                        {
                            if (ServicesToCheck[i].CheckLogonAs.Count > 0)
                            {
                                var logonAcct = Toolset.RemoveWhitespace(GetWindowsServiceLogonAccount(ServerName, svc.ServiceName)).ToUpper();
                                if (string.IsNullOrEmpty(logonAcct))
                                    logonAcct = "Unknown";

                                if (!ServicesToCheck[i].CheckLogonAs.Contains(logonAcct))
                                {
                                    hcErr.Result = getCorrectStatusResult(hcErr.Result, StatusResult.Warning);
                                    sb.Clear();
                                    sb.AppendFormat("{0} - Windows service \"{1}\" logon account is {2}.", StatusResult.Warning, svc.ServiceName, logonAcct);
                                    sb.AppendFormat(" Expected logon account(s): {0}", string.Join(",", ServicesToCheck[i].CheckLogonAs));
                                    hcErr.Error.Add(sb.ToString());
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            hcErr.Result = getCorrectStatusResult(hcErr.Result, StatusResult.Critical);
                            hcErr.Error.Add(string.Format("{0} - Failed to check windows service logon account for \"{1}\" on \"{2}\". {3}", StatusResult.Critical, ServicesToCheck[i].Name, ServerName, ex.Message));
                        }
                    }
                    errors.Add(hcErr);
                }
                catch (Exception ex)
                {
                    exceptions.Add(new Exception(string.Format("{0} - Failed to get windows service \"{1}\" on {2}. {3}", StatusResult.Critical, ServicesToCheck[i].Name, ServerName, ex.Message), ex.InnerException));
                }
            }

            if (exceptions.Count > 0)
            {
                var hce = new HealthCheckError();
                exceptions.ForEach(x => hce.Error.Add(x.Message));
                hce.HCType = HealthCheckType.GeneralServer;
                hce.Result = StatusResult.Critical;
                errors.Add(hce);
            }

            return Task.FromResult<List<HealthCheckError>>(errors);
        }

        public static ServiceStartMode GetWindowsServiceStartMode(string ServerName, string ServiceName)
        {
            SelectQuery query = new SelectQuery(string.Format("SELECT StartMode FROM Win32_Service WHERE Name = '{0}'", ServiceName));
            string scopeStr = string.Format(@"\\{0}\root\cimv2", ServerName);

            ManagementScope scope = new ManagementScope(scopeStr);
            scope.Options.Timeout = new TimeSpan(0, 0, 5);

            try
            {
                scope.Connect();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("{0} - Unable to connect to WMI on {1} to perform windows service checks. {2}.", StatusResult.Critical, ServerName, ex.Message));
            }

            var sm = string.Empty;

            using (var svc = new ManagementObjectSearcher(scope, query))
            {
                svc.Options.Timeout = new TimeSpan(0, 0, 5);
                svc.Options.ReturnImmediately = false;
                using (var watcher = svc.Get())
                {
                    foreach(var obj in watcher)
                    {
                        using (obj)
                        {
                            switch (obj.GetPropertyValue("StartMode").ToString())
                            {
                                case "Auto":
                                case "Automatic":
                                    return ServiceStartMode.Automatic;
                                case "Manual":
                                    return ServiceStartMode.Manual;
                                case "Disabled":
                                    return ServiceStartMode.Disabled;
                                default:
                                    sm = obj.GetPropertyValue("StartMode").ToString();
                                    break;
                            }
                        }
                    }
                }
            }
            

            throw new Exception(string.Format("Unable to get or unable to recognize windows service start mode value {0} for service {1} on {2}.", sm, ServiceName, ServerName));
        }

        public static string GetWindowsServiceLogonAccount(string ServerName, string ServiceName)
        {
            SelectQuery query = new SelectQuery(string.Format("SELECT StartName FROM Win32_Service WHERE Name = '{0}'", ServiceName));

            string scopeStr = string.Format(@"\\{0}\root\cimv2", ServerName);

            ManagementScope scope = new ManagementScope(scopeStr);
            scope.Options.Timeout = new TimeSpan(0, 0, 5);

            try
            {
                scope.Connect();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("{0} - Unable to connect to WMI on {1} to perform windows service checks. {2}.", StatusResult.Critical, ServerName, ex.Message));
            }

            using (var svc = new ManagementObjectSearcher(scope, query))
            {
                svc.Options.Timeout = new TimeSpan(0, 0, 5);
                svc.Options.ReturnImmediately = false;
                using (var objCol = svc.Get())
                {
                    foreach (var obj in objCol)
                    {
                        using (obj)
                        {
                            return obj.GetPropertyValue("StartName").ToString();
                        }
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Returns whether the server is currently online or not
        /// </summary>
        /// <param name="serverFullName">FQDN of the server</param>
        /// <returns>True if server is online, False otherwise</returns>
        public static bool getIsOnline(FiOSServer server)
        {
            PingReply pingReply;
            using (var ping = new Ping())
            {
                try
                {
                    pingReply = ping.Send(server.HostFullName);
                }
                catch
                {
                    return false;
                }
            }

            return pingReply.Status == IPStatus.Success;
        }

        public static bool WindowsServiceExists(string ServiceName, string ServerName)
        {
            try
            {
                using (ServiceController ctrl = new ServiceController(ServiceName, ServerName))
                {
                    return ctrl.Status == ServiceControllerStatus.Running;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
