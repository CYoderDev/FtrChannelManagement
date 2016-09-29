using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;

namespace FrontierVOps.FiOS.HealthCheck.DataObjects
{
    public class IISConfiguration
    {
        public ApplicationPoolCollection AppPools { get; set; }
        public VirtualDirectoryCollection VirtualDirectories { get; set; }
        public ApplicationCollection Applications { get; set; }
        public BindingCollection Bindings { get; set; }
    }
}
