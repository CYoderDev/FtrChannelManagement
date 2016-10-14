using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.Common;
using FrontierVOps.Common.FiOS;
using FrontierVOps.FiOS.Servers.Objects;

namespace FrontierVOps.Data.Objects
{
    public class SqlDb : FiOSDbServer, iDatabase
    {
        /// <summary>
        /// Get or set the name of the SQL Database
        /// </summary>
        public string DatabaseName { get { return this._databaseName; } set { this._databaseName = value; } }
        private string _databaseName;
        /// <summary>
        /// Get or set the length of time (in seconds) before the connection times out
        /// </summary>
        public int ConnectionTimeout { get { return this._connectionTimeout; } set { this._connectionTimeout = value; } }
        private int _connectionTimeout;
        /// <summary>
        /// Get or set the username used for the connection
        /// </summary>
        /// <remarks>Ignored if IntegratedSecurity is set to true</remarks>
        public string Username { get { return this._userName; } set { this._userName = value; this.IntegratedSecurity = false; } }
        private string _userName;
        /// <summary>
        /// Get or set a password for the connecting user
        /// </summary>
        /// <remarks>Ignored if IntegratedSecurity is set to true</remarks>
        public SecureString Password { get { return this._password; } set { this._password = value; this.IntegratedSecurity = false; } }
        private SecureString _password;
        /// <summary>
        /// Get or set whether or not the connection will use the user's windows credentials for the connection
        /// </summary>
        public bool IntegratedSecurity { get { return _integratedSecurity; } set { _integratedSecurity = value; } }
        private bool _integratedSecurity;

        /// <summary>
        /// Get or set the location of the database within the FiOS environment
        /// </summary>
        public FiOSLocation FiosLocation { get { return this._fiosLocation; } set { this._fiosLocation = value; } }
        private FiOSLocation _fiosLocation;

        dynamic iDatabase.Location { get { return this._fiosLocation; } set { this._fiosLocation = (FiOSLocation)value; } }

        /// <summary>
        /// Get or set the database core function.
        /// </summary>
        public DbFunction Function { get { return this._function; } set { this._function = value; } }
        private DbFunction _function;

        public SqlDb()
        {
            this.IntegratedSecurity = true;
            this.ConnectionTimeout = 30;
            this.HostFunction = ServerFunction.Database;
            this.DatabaseType = FiOS.Servers.Objects.DbType.TSQL;
        }

        public string CreateConnectionString(Datasource DS, bool useMARs)
        {
            SqlConnectionStringBuilder strBuilder = new SqlConnectionStringBuilder();
            strBuilder.DataSource = DS.Name;
            strBuilder.InitialCatalog = this._databaseName;
            strBuilder.ConnectTimeout = this._connectionTimeout;
            strBuilder.IntegratedSecurity = this._integratedSecurity;
            strBuilder.MultipleActiveResultSets = useMARs;

            if (!this._integratedSecurity)
            {
                strBuilder.UserID = this._userName ?? string.Empty;
                strBuilder.Password = Toolset.ConvertToInsecureString(this._password) ?? string.Empty;
            }

            return strBuilder.ConnectionString;
        }

        public string CreateConnectionString(Datasource DS)
        {
            return CreateConnectionString(DS, false);
        }

        private void validateArgs()
        {
            if (this._databaseName == null)
                throw new ArgumentNullException("DatabaseName cannot be null");
        }
    }
}
