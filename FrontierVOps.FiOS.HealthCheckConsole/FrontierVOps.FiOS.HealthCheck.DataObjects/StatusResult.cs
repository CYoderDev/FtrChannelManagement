using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.FiOS.HealthCheck.DataObjects
{
    public enum StatusResult
    {
        Skipped = 0,
        Ok = 1,
        Warning = 2,
        Error = 3,
        Critical = 4,
    }
}
