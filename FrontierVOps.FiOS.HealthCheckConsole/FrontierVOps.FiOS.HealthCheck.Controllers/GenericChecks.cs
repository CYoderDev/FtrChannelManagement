using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.Administration;
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
        public List<string> WindowsServicesToCheck { get; set; }

        public GenericChecks()
        {
            this.WindowsServicesToCheck = new List<string>();
        }

        /// <summary>
        /// Performs all server checks on the server
        /// </summary>
        /// <param name="Server">FiOS Server</param>
        /// <returns>A health rollup containing all general server checks</returns>
        public async Task<HealthRollup> PerformServerCheck(FiOSServer Server)
        {
            Server.IsOnline = getIsOnline(Server.HostFullName);
            var hru = new HealthRollup();
            var hce = new HealthCheckError();
            hru.Server = Server;
            hru.Result = StatusResult.Ok;

            if (!Server.IsOnline)
            {
                hru.Result = StatusResult.Critical;
                hce.Error.Add(string.Format("{0} - Server is offline or unreachable via it's FQDN.", StatusResult.Critical));
                hru.Errors.Add(hce);
                return await Task.FromResult<HealthRollup>(hru);
            }

            #region CheckHardDrives
            var hdds = await HardDrive.GetHardDriveAsync(Server.HostFullName);

            for (int i = 0; i < hdds.Length; i++)
            {
                var result = StatusResult.Ok;

                //Check Hard Drive Space
                try
                {
                    result = await checkHDDSpaceAsync(hdds[i]);

                    if (result != StatusResult.Ok)
                    {
                        hce.Error.Add(string.Format("{0} - Disk {1} currently only has {2}GB of {3}GB of space remaining. SN: {4}.", result, hdds[i].Name, (hdds[i].FreeSpace / 1024 / 1024 / 1024).ToString("N1"), hdds[i].SerialNumber));
                    }
                }
                catch (Exception ex)
                {
                    hce.Error.Add(string.Format("{0} - Failed to get available disk space. Exception: {1}", StatusResult.Error, ex.Message));
                    hru.Result = getCorrectStatusResult(hru.Result, StatusResult.Error);
                }

                //Check Hard Drive Status
                try
                {
                    result = await checkHDDStatusAsync(hdds[i]);

                    hru.Result = getCorrectStatusResult(hru.Result, result);

                    if (result != StatusResult.Ok)
                        hce.Error.Add(string.Format("{0} - Disk {1} is currently in a {2} state. Type: {3} - SN: {4}", result, hdds[i].Name, hdds[i].Status, hdds[i].DriveType, hdds[i].SerialNumber));

                    hru.Result = getCorrectStatusResult(hru.Result, result);
                }
                catch (Exception ex)
                {
                    hce.Error.Add(string.Format("{0} - Failed to get hard drive status. Exception: {1}", StatusResult.Error, ex.Message));
                    hru.Result = getCorrectStatusResult(hru.Result, StatusResult.Error);
                }
            }
            #endregion CheckHardDrives

            #region CheckWindowsServices
            try
            {
                var tplWinServicesRes = await checkWindowsServices(Server.HostFullName, this.WindowsServicesToCheck.ToArray());

                hru.Result = getCorrectStatusResult(hru.Result, tplWinServicesRes.Item1);
                if (tplWinServicesRes.Item2.Count > 0)
                {
                    hce.Error.AddRange(tplWinServicesRes.Item2);
                }
            }
            catch (Exception ex)
            {
                hce.Error.Add(string.Format("{0} - Failed to get windows services. Exception: {1}", hru.Result, ex.Message));
                hru.Result = getCorrectStatusResult(hru.Result, StatusResult.Error);
            }
            #endregion CheckWindowsServices

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
            var percRemaining = (hdd.FreeSpace / hdd.Capacity) * 100;
            var freeInGB = hdd.FreeSpace / 1024 / 1024 / 1024;

            //If less than one percent remaining, or less than 1 GB remaining, return critical.
            if (percRemaining < 1 || freeInGB < 1)
                return Task.FromResult<StatusResult>(StatusResult.Critical);
            //If less than 5 percent remaining, or less than 10 GB remaining, return error.
            else if (percRemaining < 5 || freeInGB < 10)
                return Task.FromResult<StatusResult>(StatusResult.Error);
            //If less than 10 percent remaining, or less than 25 GB remaining, return warning.
            else if (percRemaining < 10 || freeInGB < 25)
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

        /// <summary>
        /// Checks windows services.
        /// </summary>
        /// <param name="serverName">Name of the server being checked</param>
        /// <param name="servicesToCheck">Array of services to check</param>
        /// <returns>A tuple containing the highest level of status result, and a list of errors.</returns>
        private Task<Tuple<StatusResult, List<string>>> checkWindowsServices(string serverName, string[] servicesToCheck)
        {
            var services = ServiceController.GetServices(serverName).Where(x => servicesToCheck.Contains(x.ServiceName)).ToArray();
            var errors = new List<string>();
            var result = StatusResult.Ok;
            
            for (int i = 0; i < services.Length; i++)
            {
                try
                {
                    if (services[i].Status != ServiceControllerStatus.Running)
                    {
                        if (services[i].Status.ToString().ToUpper().Contains("PENDING"))
                        {
                            result = getCorrectStatusResult(result, StatusResult.Warning);
                            errors.Add(string.Format("{0} - Windows service \"{1}\" is in a {2} state.", StatusResult.Warning, services[i].ServiceName, services[i].Status));
                        }
                        else
                        {
                            result = getCorrectStatusResult(result, StatusResult.Error);
                            errors.Add(string.Format("{0} - Windows service \"{1}\" is in a {2} state.", StatusResult.Error, services[i].ServiceName, services[i].Status));
                        }

                    }
                }
                finally
                {
                    services[i].Close();
                    services[i].Dispose();
                }
            }

            return Task.FromResult<Tuple<StatusResult, List<string>>>(new Tuple<StatusResult, List<string>>(result, errors));
        }

        /// <summary>
        /// Returns whether the server is currently online or not
        /// </summary>
        /// <param name="serverFullName">FQDN of the server</param>
        /// <returns>True if server is online, False otherwise</returns>
        private static bool getIsOnline(string serverFullName)
        {
            PingReply pingReply;
            using (var ping = new Ping())
            {
                try
                {
                    pingReply = ping.Send(serverFullName);
                }
                catch
                {
                    return false;
                }
            }

            return pingReply.Status == IPStatus.Success;
        }
    }
}
