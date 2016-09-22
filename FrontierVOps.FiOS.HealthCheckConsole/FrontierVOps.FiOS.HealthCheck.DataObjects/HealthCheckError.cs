using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.FiOS.HealthCheck.DataObjects
{
    public class HealthCheckError
    {
        /// <summary>
        /// Type of health check being performed
        /// </summary>
        public HealthCheckType HCType { get; set; }

        /// <summary>
        /// List of errors during the health check
        /// </summary>
        public List<string> Error { get; set; }

        /// <summary>
        /// Result of the health checks
        /// </summary>
        public StatusResult Result { get; set; }

        public HealthCheckError()
        {
            this.Error = new List<string>();
        }
    }
}
