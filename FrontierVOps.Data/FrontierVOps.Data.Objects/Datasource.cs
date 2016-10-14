using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.Common.FiOS;

namespace FrontierVOps.Data.Objects
{
    public class Datasource
    {
        /// <summary>
        /// Datasource name used to connect to the instance
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// IP of the datasource instance
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// Type of data source
        /// </summary>
        public DSType Type { get; set; }

        /// <summary>
        /// FiOS Role to where the instance belongs
        /// </summary>
        public FiOSRole Role { get; set; }

        /// <summary>
        /// All of the databases that belong to the datasource
        /// </summary>
        public List<iDatabase> Databases { get; set; }

        public Datasource(string Name)
        {
            if (string.IsNullOrEmpty(Name))
            {
                throw new ArgumentNullException("Name cannot be null or empty.");
            }
            this.Name = Name;
            this.Databases = new List<iDatabase>();
            this.Role = FiOSRole.Unknown;
            this.Type = DSType.TSQL;
        }

        /// <summary>
        /// Get the connection string for a database
        /// </summary>
        /// <param name="useMARs">Use multiple active result sets?</param>
        /// <param name="database">Database being connected</param>
        /// <returns>The connection string used to connect to the database</returns>
        public string CreateConnectionString(bool useMARs, iDatabase database)
        {
            return database.CreateConnectionString(this, useMARs);
        }

        /// <summary>
        /// Get the connection string for a database
        /// </summary>
        /// <param name="database">Database being connected</param>
        /// <returns>The connection string used to connect to the database</returns>
        public string CreateConnectionString(iDatabase database)
        {
            return database.CreateConnectionString(this);
        }

        /// <summary>
        /// Get the connection string for the database at the specified index
        /// of the Databases property.
        /// </summary>
        /// <param name="index">Index of the database within the Databases property</param>
        /// <param name="useMARs">Use multiple active result sets?</param>
        /// <returns>The connection string used to connect to the database</returns>
        public string CreateConnectionString(int index, bool useMARs = false)
        {
            if (this.Databases.Count == 0 || index > (this.Databases.Count - 1))
                throw new IndexOutOfRangeException();

            var db = this.Databases[index];

            return db.CreateConnectionString(this, useMARs);
        }
    }

    public enum DSType { TSQL, Mongo, Cassandra }
}
