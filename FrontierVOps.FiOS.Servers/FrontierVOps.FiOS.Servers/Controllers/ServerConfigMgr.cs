using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FrontierVOps.Config.FiOS;
using FrontierVOps.FiOS.Servers.Enumerators;
using FrontierVOps.FiOS.Servers.Objects;

namespace FrontierVOps.FiOS.Servers.Controllers
{
    public class ServerConfigMgr
    {
        static XDocument _config { get; set; }
        static XNamespace _ns { get; set; }

        static ServerConfigMgr()
        {
            if (ServerConfigMgr._config == null)
            {
                CfgHelper cfgHelper = new CfgHelper();
                ServerConfigMgr._config = cfgHelper.GetConfig("ServerLayout.xml");
                ServerConfigMgr._ns = _config.Root.GetDefaultNamespace();
            }
        }

        public static IEnumerable<FiOSServer> GetServers()
        {
            var serverElems = _config.Root.Descendants(_ns + "Server");

            foreach(var serverElem in serverElems)
            {
                if (serverElem.HasAttributes)
                {
                    if (serverElem.FirstAttribute.Value.ToUpper().Equals("DATABASE"))
                    {
                        string vhoName = null;
                        var dbServer = new FiOSDbServer();
                        dbServer.HostName = serverElem.Value;
                        dbServer.HostFullName = getFullName(dbServer.HostName);
                        dbServer.HostLocation = getLocation(serverElem, out vhoName);
                        dbServer.HostLocationName = vhoName;
                        dbServer.HostRole = getRole(serverElem, dbServer.HostLocation);
                        dbServer.HostFunction = ServerFunction.Database;

                        yield return dbServer;
                    }
                    else if (serverElem.FirstAttribute.Value.ToUpper().Equals("WEB"))
                    {
                        string vhoName = null;
                        var webServer = new FiOSWebServer();
                        webServer.HostFunction = ServerFunction.Web;
                        webServer.HostName = serverElem.Value;
                        webServer.HostFullName = getFullName(webServer.HostName);
                        webServer.HostLocation = getLocation(serverElem, out vhoName);
                        webServer.HostLocationName = vhoName;
                        webServer.HostRole = getRole(serverElem, webServer.HostLocation);
                        webServer.HostFunction = ServerFunction.Web;

                        yield return webServer;
                    }
                }
                else
                {
                    string vho = null;
                    var server = new FiOSServer();
                    server.HostName = serverElem.Value;
                    server.HostFullName = getFullName(server.HostName);
                    server.HostLocation = getLocation(serverElem, out vho);
                    server.HostLocationName = vho;
                    server.HostRole = getRole(serverElem, server.HostLocation);
                    server.HostFunction = serverElem.HasAttributes && serverElem.FirstAttribute.Value.ToUpper().Equals("APP") ? ServerFunction.Application :
                        serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("INFRASTRUCTURE")) ? ServerFunction.Infrastructure : ServerFunction.Unknown;

                    yield return server;
                }
            }
        }

        private static ServerLocation getLocation(XElement serverElem, out string VHOName)
        {
            if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("VHO")))
            {
                VHOName = serverElem.Ancestors(_ns + "VHO").FirstOrDefault().FirstAttribute.Value;
                return ServerLocation.VHO;
            }
            else if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("VHE")))
            {
                VHOName = "VHE";
                return ServerLocation.VHE;
            }

            VHOName = string.Empty;
            return ServerLocation.Unknown;
        }

        private static ServerRole getRole(XElement serverElem, ServerLocation location)
        {
            switch(location)
            {
                case ServerLocation.VHE:
                    {
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("AES")))
                            return ServerRole.AES;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("ADMINCONSOLE")))
                            return ServerRole.AdminConsole;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("AUTOPROVISIONING")))
                            return ServerRole.AutoProvisioning;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("DOMAINCONTROLLERS")))
                            return ServerRole.DomainController;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("FIOSADVANCED")))
                            return ServerRole.FiOSAdvanced;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("FOTG")))
                            return ServerRole.FiOSOnTheGo;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("HYDRA")))
                            return ServerRole.Hydra;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("KMS-MDT")))
                            return ServerRole.KMSorMDT;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("MEDIAMGR")))
                            return ServerRole.MediaManager;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("MSV")))
                            return ServerRole.MSV;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("NSP")))
                            return ServerRole.NSP;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("PLAYREADY")))
                            return ServerRole.Playready;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("RATINGSRECOM")))
                            return ServerRole.RatingsAndRecomm;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("SCCM")))
                            return ServerRole.SCCM;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("SCOM")))
                            return ServerRole.SCOM;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("SEARCH")))
                            return ServerRole.Search;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("SFTP")))
                            return ServerRole.SFTP;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("VODENCRYPTION")))
                            return ServerRole.VOD;
                        break;
                    }
                case ServerLocation.VHO:
                    {
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("DOMAINCONTROLLERS")))
                            return ServerRole.DomainController;          
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("GATEWAY")))
                            return ServerRole.Gateway;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("IMG")))
                            return ServerRole.IMG;        
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("MES")))
                            return ServerRole.MES;
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("MGS")))
                            return ServerRole.MGS;        
                        if (serverElem.Ancestors().Any(x => x.Name.LocalName.ToUpper().Equals("THUMBNAIL")))
                            return ServerRole.Thumbnail;
                        break;
                    }
            }
            return ServerRole.Unknown;
        }

        private static string getFullName(string serverName)
        {
            var serverElem = _config.Root.Descendants(_ns + "Server").Where(x => x.Value.ToUpper().Equals(serverName.ToUpper())).FirstOrDefault();

            if (serverElem == null)
                throw new Exception(string.Format("No element with value of {0} was found.", serverName));

            var parentEle = serverElem.Ancestors().Where(x => x.HasAttributes && x.FirstAttribute.Name == "DomainName").FirstOrDefault();

            if (parentEle == null)
                throw new Exception(string.Format("No parent element with the DomainName attribute was found."));

            return string.Format("{0}.{1}", serverName, parentEle.FirstAttribute.Value);
        }
    }
}
