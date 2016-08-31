using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.FiOS.Servers.Objects;

namespace FrontierVOps.FiOS.Servers.Enumerators
{
    public class FiOSServers : IEnumerable<FiOSServer>
    {
        List<FiOSServer> servers;

        public IEnumerator<FiOSServer> GetEnumerator()
        {
            return servers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
