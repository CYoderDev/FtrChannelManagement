using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FrontierVOps.Config.FiOS
{
    public class CfgHelper
    {
        /// <summary>
        /// Get or set the full path to the config file
        /// </summary>
        public string ConfigFile { get { return this.ConfigFile; } set { this._configFile = value; } }
        private string _configFile;

        public CfgHelper()
        {

        }

        /// <summary>
        /// Get the xml configuration.
        /// </summary>
        /// <returns>The configuration as a loaded xml document.</returns>
        public XDocument GetConfig()
        {
            if (string.IsNullOrEmpty(this._configFile))
                throw new ArgumentNullException("Config file cannot be null.");

            if (!File.Exists(this._configFile))
                throw new FileNotFoundException("Cannot locate config file to load.", this._configFile);

            if (!this._configFile.EndsWith(".xml"))
                throw new ArgumentException("File must be in xml format.");
            
            return XDocument.Load(this._configFile);
        }

        /// <summary>
        /// Gets the xml configuration.
        /// </summary>
        /// <param name="FileName">The name of the xml file to be loaded.</param>
        /// <returns>The configuration as a loaded xml document.</returns>
        public XDocument GetConfig(string FileName)
        {
            string configDir = GetConfigDirectory();
            this._configFile = Path.Combine(configDir, FileName);

            return GetConfig();
        }

        /// <summary>
        /// Attempts to locate the config directory within the executing application's folder structure
        /// </summary>
        /// <returns>Config directory path</returns>
        public string GetConfigDirectory()
        {
            string strAppPath = Path.GetDirectoryName(this.GetType().Assembly.Location);

#if DEBUG
            System.Diagnostics.Trace.WriteLine(string.Format("runPath => {0}", strAppPath));
#endif

            while (!Directory.EnumerateDirectories(strAppPath).Any(x => x.ToLower().Contains("config")))
            {
                try
                {
                    strAppPath = Directory.GetParent(strAppPath).FullName;
                }
                catch
                {
                    throw new DirectoryNotFoundException("Unable to locate config directory. " + strAppPath);
                }
            }

#if DEBUG
            System.Diagnostics.Trace.WriteLine(string.Format("strAppPath1 => {0}", strAppPath));
#endif

            try
            {
                strAppPath = Path.Combine(strAppPath, "Config");
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting config directory. " + ex.Message, ex);
            }

#if DEBUG
            System.Diagnostics.Trace.WriteLine(string.Format("strAppPath2 => {0}", strAppPath));
#endif

            if (Directory.Exists(strAppPath))
                return strAppPath;
            else
                throw new DirectoryNotFoundException("Unable to find config directory." + strAppPath);
        }
    }
}
