using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.FiOS.Servers.Objects
{
    public class FiOSServer
    {
        public string HostName { get; set; }
        public string HostFullName { get; set; }
        public string DomainName { get; set; }
        public string HostLocationName { get; set; }
        public bool IsOnline { get; set; }
        public ServerRole HostRole { get; set; }
        public ServerLocation HostLocation { get; set; }
        public ServerFunction HostFunction { get; set; }
    }
}
