using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.FiOS.Servers.Objects;

namespace FrontierVOps.FiOS.HealthCheck.DataObjects
{
    public class HCWinService
    {
        /// <summary>
        /// Get or set the name of the service.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get or set the display name for the service.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Get or set the default status for the service.
        /// </summary>
        public List<ServiceControllerStatus> CheckStatus { get; set; }

        /// <summary>
        /// Get or set the default LogonAs account for the service.
        /// </summary>
        public List<string> CheckLogonAs { get; set; }

        /// <summary>
        /// Get or set the default startup type for the service.
        /// </summary>
        public List<ServiceStartMode> CheckStartupType { get; set; }

        /// <summary>
        /// Get or set whether the service default settings only need to 
        /// pass on a single service within the cluster.
        /// </summary>
        public bool OnePerGroup { get; set; }

        /// <summary>
        /// Get or set server roles to include/exclude from the service check. 
        /// Set item1 true if include, false if exclude.
        /// </summary>
        public List<Tuple<bool, ServerRole, ServerFunction>> Roles { get; set; }

        /// <summary>
        /// Get or set the servers to include/exclude from this service check. 
        /// Set item 1 true if include, false if exclude.
        /// </summary>
        public List<Tuple<bool, FiOSServer>> Servers { get; set; }

        /// <summary>
        /// Get or set the server function that should be checked. If roles list is empty,
        /// all servers that fall under this function will have this service checked.
        /// </summary>
        public ServerFunction Function { get; set; }

        public HCWinService()
        {
            this.CheckStatus = new List<ServiceControllerStatus>();
            this.CheckStartupType = new List<ServiceStartMode>();
            this.CheckLogonAs = new List<string>();
            this.Roles = new List<Tuple<bool, ServerRole, ServerFunction>>();
            this.Servers = new List<Tuple<bool, FiOSServer>>();
        }

        public override bool Equals(object obj)
        {
            HCWinService compObj = obj as HCWinService;

            if (compObj == null)
                return false;

            if (this.CheckLogonAs.Count + this.CheckStartupType.Count + this.CheckStatus.Count + this.Roles.Count + this.Servers.Count !=
                    compObj.CheckLogonAs.Count + compObj.CheckStartupType.Count + compObj.CheckStatus.Count + compObj.Roles.Count + compObj.Servers.Count)
            {
                return false;
            }

            return this.Name.ToUpper().Equals(compObj.Name.ToUpper()) && this.DisplayName.ToUpper().Equals(compObj.DisplayName.ToUpper()) && this.OnePerGroup.Equals(compObj.OnePerGroup)
                && this.Function.Equals(compObj.Function);
        }
    }
}
