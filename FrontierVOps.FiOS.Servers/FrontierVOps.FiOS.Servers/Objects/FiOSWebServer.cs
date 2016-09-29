using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;

namespace FrontierVOps.FiOS.Servers.Objects
{
    public class FiOSWebServer : FiOSServer
    {
        public string IISVersion { get; set; }
    }
}
