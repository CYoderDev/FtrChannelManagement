using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.Data.Objects
{
    public interface iDatabase
    {
        string DatabaseName { get; set; }
        int ConnectionTimeout { get; set; }
        string Username { get; set; }
        SecureString Password { get; set; }
        bool IntegratedSecurity { get; set; }
        string CreateConnectionString(Datasource DS);
        string CreateConnectionString(Datasource DS, bool useMARs);
        dynamic Location { get; set; }
        DbFunction Function { get; set; }
    }
}
