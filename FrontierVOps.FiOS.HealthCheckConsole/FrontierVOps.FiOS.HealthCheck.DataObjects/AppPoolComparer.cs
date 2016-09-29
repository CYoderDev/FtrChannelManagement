using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;

namespace FrontierVOps.FiOS.HealthCheck.DataObjects
{
    public class AppPoolComparer : IEqualityComparer<ApplicationPool>
    {
        public bool Equals(ApplicationPool apc1, ApplicationPool apc2)
        {
            if (apc1 == null && apc2 == null)
                return true;

            if (apc1.Name != apc2.Name)
                return false;

            return false;
        }

        public int GetHashCode(ApplicationPool apc)
        {
            return apc.GetHashCode();
        }
    }
}
