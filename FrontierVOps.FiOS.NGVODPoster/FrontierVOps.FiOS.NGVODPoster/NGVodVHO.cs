using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.Data.Objects;

namespace FrontierVOps.FiOS.NGVODPoster
{
    public class NGVodVHO
    {
        /// <summary>
        /// Name of the VHO
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// FQDN of the VHO's primary IMG web server
        /// </summary>
        public string WebServerName { get; set; }

        /// <summary>
        /// The vho front end IMG database
        /// </summary>
        public SqlDb IMGDb { get; set; }

        /// <summary>
        /// The admin share path to the poster directory
        /// </summary>
        public string PosterDir 
        { 
            get
            {
                return this._posterDir;
            }
            set
            {
                if (Directory.Exists(value))
                    this._posterDir = value;
                else
                {
#if DEBUG
                    Directory.CreateDirectory(value);
                    this._posterDir = value;
#else
                    throw new DirectoryNotFoundException("Directory does not exist. " + value);
#endif
                }
            }
        }
        private string _posterDir;

        /// <summary>
        /// Individual VHO's where posters need to be stored
        /// </summary>
        public NGVodVHO()
        {
            this.IMGDb = new SqlDb();
        }
    }
}
