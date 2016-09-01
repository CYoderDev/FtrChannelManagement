using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;

namespace FrontierVOps.FiOS.Servers.Objects
{
    public class FiOSIISServer : FiOSServer
    {
        public WebAppType Type { get; set; }

        public ServerManager OpenConnection()
        {
            if (string.IsNullOrEmpty(this.HostFullName))
                throw new ArgumentNullException("HostFullName cannot be null to run open a connection");

            return OpenConnection(this.HostFullName);
        }

        public ServerManager OpenConnection(string FullServerName)
        {
            return ServerManager.OpenRemote(FullServerName);
        }
    }

    public enum WebAppType { Web, Application}
}
