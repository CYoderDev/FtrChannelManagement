using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;
using FrontierVOps.FiOS.HealthCheck.DataObjects;
using FrontierVOps.FiOS.Servers.Controllers;
using FrontierVOps.FiOS.Servers.Objects;

namespace FrontierVOps.FiOS.HealthCheck.Controllers
{
    public class IISChecks
    {
        public async Task<HealthRollup> CheckWebServer(FiOSWebServer Server)
        {
            HealthRollup hru = new HealthRollup();
            HealthCheckError hce = new HealthCheckError();
            hru.Server = Server;
            hce.Result = StatusResult.Ok;
            hce.HCType = HealthCheckType.IIS;

            if (!Server.IsOnline)
            {
                hce.Result = StatusResult.Ok;
                hce.Error.Add(string.Format("{0} - Cannot communicate with web services due to the server being unreachable.", StatusResult.Critical));
            }

            try
            {
                using (var iisManager = new IISServerMgr(Server))
                {
                    foreach (var site in iisManager.GetSiteCollection())
                    {
                        switch (site.State)
                        {
                            case ObjectState.Started:
                                break;
                            case ObjectState.Stopped:
                                hce.Result = StatusResult.Critical;
                                hce.Error.Add(string.Format("{0} - IIS Site {1} is in a stopped state.", StatusResult.Critical, site.Name));
                                break;
                            case ObjectState.Stopping:
                                hce.Result = StatusResult.Critical;
                                hce.Error.Add(string.Format("{0} - IIS Site {1} is currently in a stopping state.", StatusResult.Error, site.Name));
                                break;
                            case ObjectState.Starting:
                                hce.Result = StatusResult.Critical;
                                hce.Error.Add(string.Format("{0} - IIS Site {1} is currently starting.", StatusResult.Warning, site.Name));
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                hce.Result = StatusResult.Error;
                hce.Error.Add(string.Format("{0} - Error connecting to IIS. {1}", StatusResult.Error, ex.Message));
            }

            hru.Errors.Add(hce);
            return await Task.FromResult<HealthRollup>(hru);
        }

        public async Task<HealthRollupCollection> CompareIISConfigs(IEnumerable<FiOSWebServer> WebServers)
        {
            return await Task.FromResult<HealthRollupCollection>(new HealthRollupCollection());
        }

        private Task<Tuple<StatusResult, string>> checkServicesAsync(string serverName, string serviceName, string serviceDesc)
        {
            var error = string.Empty;
            var result = StatusResult.Ok;

            using (var svcCtrl = new ServiceController(serviceName, serverName))
            {
                if (svcCtrl.Status != ServiceControllerStatus.Running)
                {
                    if (svcCtrl.Status == ServiceControllerStatus.Stopped)
                        result = GenericChecks.getCorrectStatusResult(result, StatusResult.Critical);

                    error = string.Format("{0} - {1} service is in a {2} state. {3}", result, serviceName, svcCtrl.Status, serviceDesc);
                }
            }

            return Task.FromResult<Tuple<StatusResult, string>>(new Tuple<StatusResult, string>(result, error));
        }

        private async Task<Tuple<StatusResult, List<string>>> compareAppPoolsAsync(ApplicationPoolCollection appPools1, ApplicationPoolCollection appPools2, string server1, string server2)
        {
            var result = StatusResult.Ok;
            var errors = new List<string>();

            if (appPools1.Count < appPools2.Count)
            {
                result = GenericChecks.getCorrectStatusResult(result, StatusResult.Error);
                appPools2.Except(appPools1, new AppPoolComparer()).Select(x => x.Name).ToList().ForEach((ap) =>
                {
                    errors.Add(string.Format("{0} - {1} application pool exists on {2} but does not exist on {3}", result, ap, server2, server1));
                });
            }

            foreach(var ap in appPools1)
            {
                var diffAp = appPools2.Where(x => x.Name == ap.Name).FirstOrDefault();

                if (diffAp == null)
                    continue;

                switch (ap.State)
                {
                    case ObjectState.Started:
                    case ObjectState.Starting:
                        break;
                    case ObjectState.Stopped:
                        errors.Add(string.Format("{0} - {1} application pool is in a stopped state.", StatusResult.Critical, ap.Name));
                        result = GenericChecks.getCorrectStatusResult(result, StatusResult.Critical);
                        break;
                    case ObjectState.Stopping:
                    case ObjectState.Unknown:
                        errors.Add(string.Format("{0} - {1} application pool is in a {2} state", StatusResult.Warning, ap.Name, ap.State));
                        result = GenericChecks.getCorrectStatusResult(result, StatusResult.Warning);
                        break;
                }

                if (!ap.AutoStart.Equals(diffAp.AutoStart))
                {
                    errors.Add(string.Format("{0} - {1} application pool auto start config does not match with {2}. Currently set to {3}.", StatusResult.Warning, ap.Name, server2, ap.AutoStart));
                    result = GenericChecks.getCorrectStatusResult(result, StatusResult.Warning);
                }

                if (!ap.Enable32BitAppOnWin64.Equals(diffAp.Enable32BitAppOnWin64))
                {
                    errors.Add(string.Format("{0} - 32 bit applications is {1} on application pool {2}, but is {2} on {3}", StatusResult.Error,
                        ap.Enable32BitAppOnWin64 ? "enabled" : "disabled",
                        diffAp.Enable32BitAppOnWin64 ? "enabled" : "disabled",
                        server2
                        ));
                    result = GenericChecks.getCorrectStatusResult(result, StatusResult.Error);
                }

                if (!ap.QueueLength.Equals(diffAp.QueueLength))
                {
                    errors.Add(string.Format("{0} - {1} application pool queue length does not match with {2}. Currently set to {3}.", StatusResult.Warning, ap.Name, server2, ap.QueueLength));
                    result = GenericChecks.getCorrectStatusResult(result, StatusResult.Warning);
                }

                var processModelResults = await compareProcessModelsAsync(ap.ProcessModel, diffAp.ProcessModel);
            }

            return await Task.FromResult<Tuple<StatusResult, List<string>>>(new Tuple<StatusResult, List<string>>(result, errors));
        }

        private Task<Tuple<StatusResult, List<string>>> compareProcessModelsAsync(ApplicationPoolProcessModel pm1, ApplicationPoolProcessModel pm2)
        {
            var errors = new List<string>();
            var result = StatusResult.Ok;

            if (pm1.IdentityType != pm2.IdentityType)
                errors.Add(string.Format("{0} - "));

            return Task.FromResult<Tuple<StatusResult, List<string>>>(new Tuple<StatusResult, List<string>>(result, errors));
        }
    }
}
