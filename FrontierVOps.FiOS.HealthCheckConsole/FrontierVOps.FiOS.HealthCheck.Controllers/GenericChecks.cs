using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<string> WindowsServicesToCheck { get; set; }
        private enum CurrentCheck { HardDriveSpace, HardDriveAvailability, }

        public GenericChecks()
        {
            this.WindowsServicesToCheck = new List<string>();
        }

        public async Task<HealthRollup> PerformServerCheck(FiOSServer Server)
        {
            var hru = new HealthRollup();
            hru.ServerName = Server.HostName;
            hru.Result = StatusResult.Ok;

            if (!Server.IsOnline)
            {
                hru.Result = StatusResult.Critical;
                hru.Errors.Add(string.Format("Server is offline or unreachable via it's FQDN."));
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
                        hru.Errors.Add(string.Format("{0} - Disk {1} currently only has {2}GB of {3}GB of space remaining. SN: {4}.", result, hdds[i].Name, (hdds[i].FreeSpace / 1024 / 1024 / 1024).ToString("N1"), hdds[i].SerialNumber));
                    }
                }
                catch (Exception ex)
                {
                    hru.Errors.Add(string.Format("{0} - Failed to get available disk space. Exception: {1}", StatusResult.Error, ex.Message));
                    hru.Result = getCorrectStatusResult(hru.Result, StatusResult.Error);
                }

                //Check Hard Drive Status
                try
                {
                    result = await checkHDDStatusAsync(hdds[i]);

                    hru.Result = getCorrectStatusResult(hru.Result, result);

                    if (result != StatusResult.Ok)
                        hru.Errors.Add(string.Format("{0} - Disk {1} is currently in a {2} state. Type: {3} - SN: {4}", result, hdds[i].Name, hdds[i].Status, hdds[i].DriveType, hdds[i].SerialNumber));

                    hru.Result = getCorrectStatusResult(hru.Result, result);
                }
                catch (Exception ex)
                {
                    hru.Errors.Add(string.Format("{0} - Failed to get hard drive status. Exception: {1}", StatusResult.Error, ex.Message));
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
                    hru.Errors.AddRange(tplWinServicesRes.Item2);
            }
            catch (Exception ex)
            {
                hru.Errors.Add(string.Format("{0} - Failed to get windows services. Exception: {1}", hru.Result, ex.Message));
                hru.Result = getCorrectStatusResult(hru.Result, StatusResult.Error);
            }
            #endregion CheckWindowsServices



            return await Task.FromResult<HealthRollup>(hru);
        }

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

        private Task<StatusResult> checkHDDSpaceAsync(HardDrive hdd)
        {
            var percRemaining = (hdd.FreeSpace / hdd.Capacity) * 100;
            var freeInGB = hdd.FreeSpace / 1024 / 1024 / 1024;

            if (percRemaining < 1 || freeInGB < 1)
                return Task.FromResult<StatusResult>(StatusResult.Critical);
            else if (percRemaining < 5 || freeInGB < 10)
                return Task.FromResult<StatusResult>(StatusResult.Error);
            else if (percRemaining < 10 || freeInGB < 25)
                return Task.FromResult<StatusResult>(StatusResult.Warning);
            else
                return Task.FromResult<StatusResult>(StatusResult.Ok);
        }

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
                        }
                        else
                        {
                            result = getCorrectStatusResult(result, StatusResult.Error);
                        }

                        errors.Add(string.Format("Windows service \"{0}\" is in a {1} state.", services[i].ServiceName, services[i].Status));
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
    }
}
