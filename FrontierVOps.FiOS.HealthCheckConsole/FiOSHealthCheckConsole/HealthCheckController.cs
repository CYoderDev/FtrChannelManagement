using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Management.Instrumentation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.Administration;
using FrontierVOps.FiOS.Servers.Controllers;
using FrontierVOps.FiOS.Servers.Objects;

namespace FiOSHealthCheckConsole
{
    class HealthCheckController
    {
        //public async Task CheckServers()
        //{
        //    var servers = ServerConfigMgr.GetServers();

        //    HTMLFormatter htmlFormatter = new HTMLFormatter();

        //    Parallel.ForEach(servers, async (server) =>
        //        {
        //            if (server is FiOSWebServer)
        //            {
        //                var hrColl = new HealthRollupCollection();
        //                hrColl.Add(await CheckWebServer(server as FiOSWebServer));
        //            }
        //        });
        //}

        //#region WebServers
        //public async Task<HealthRollup> CheckWebServer(FiOSWebServer Server)
        //{
        //    HealthRollup hru = new HealthRollup();
        //    hru.ServerName = Server.HostName;
        //    hru.Result = StatusResult.Ok;

        //    if (!Server.IsOnline)
        //    {
        //        hru.Result = StatusResult.Critical;
        //        hru.Errors.Add("Cannot communicate with web services due to the server being unreachable.");
        //    }

        //    try
        //    {
        //        using (var iisManager = new IISServerMgr(Server))
        //        {
        //            foreach (var site in iisManager.GetSiteCollection())
        //            {
        //                switch (site.State)
        //                {
        //                    case ObjectState.Started:
        //                        break;
        //                    case ObjectState.Stopped:
        //                        hru.Result = StatusResult.Critical;
        //                        hru.Errors.Add(string.Format("IIS Site {0} is in a stopped state.", site.Name));
        //                        break;
        //                    case ObjectState.Stopping:
        //                        hru.Result = StatusResult.Error;
        //                        hru.Errors.Add(string.Format("IIS Site {0} is currently in a stopping state."));
        //                        break;
        //                    case ObjectState.Starting:
        //                        hru.Result = StatusResult.Warning;
        //                        hru.Errors.Add(string.Format("IIS Site {0} is currently starting."));
        //                        break;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        hru.Result = StatusResult.Error;
        //        hru.Errors.Add(string.Format("Error connecting to IIS. {0}", ex.Message));
        //    }

        //    return await Task.FromResult<HealthRollup>(hru);
        //}

        //public async Task<HealthRollup> GeneralServerCheck(FiOSServer Server)
        //{
        //    var hru = new HealthRollup();
        //    hru.Result = StatusResult.Ok;

        //    if (!Server.IsOnline)
        //    {
        //        hru.Result = StatusResult.Critical;
        //        hru.Errors.Add(string.Format("Server is offline or unreachable via it's FQDN."));
        //    }
            

        //}

        //public async Task CheckWebServersAsync()
        //{
        //    var webServers = ServerConfigMgr.GetServers().Where(x => x.HostFunction == ServerFunction.Web);

        //    //Site Name, Server Name, 
        //    List<Tuple<string, string, string>> siteStatus = new List<Tuple<string, string, string>>();
        //    IDictionary<ServerRole, FiOSWebServer[]> roleDict = new Dictionary<ServerRole, FiOSWebServer[]>();

        //    var serverRoles = webServers.GroupBy(x => x.HostRole).Select(y => y.Key);

        //    foreach(var serverRole in serverRoles)
        //    {
        //        roleDict.Add(serverRole, webServers.Cast<FiOSWebServer>().Where(x => x.HostRole == serverRole).ToArray());
        //    }

        //    foreach (var server in webServers)
        //    {
        //        var dictSiteStatuses = await checkIISSiteStatus(server as FiOSWebServer);
        //    }
        //}

        //private async Task<IDictionary<string, ObjectState>> checkIISSiteStatus(FiOSWebServer server)
        //{
        //    var ret = new Dictionary<string, ObjectState>();
        //    using (var iisManager = new IISServerMgr(server))
        //    {
        //        foreach (var site in iisManager.GetSiteCollection())
        //        {
        //            ret.Add(site.Name, site.State);
        //        }
        //    }

        //    return await Task.FromResult<IDictionary<string, ObjectState>>(ret);
        //}
        //#endregion
    }
}
