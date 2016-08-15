using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FrontierVOps.Config.FiOS;
using FrontierVOps.Data.Objects;

namespace FrontierVOps.FiOS.NGVODPoster
{
    public class NGVodPosterConfig
    {
        #region Properties
        public IDictionary<string, NGVodVHO> Vhos { get { return this._vhos; } }
        private IDictionary<string, NGVodVHO> _vhos;

        public string DestinationDir 
        { 
            get 
            { 
                return this._destinationDir; 
            } 
            internal set 
            {
                this._destinationDir = value;
            } 
        }
        private string _destinationDir;

        public string SourceDir
        {
            get
            {
                return this._sourceDir;
            }
            internal set
            {
                if (Directory.Exists(value))
                    this._sourceDir = value;
                else
                    throw new DirectoryNotFoundException("Directory does not exist. " + value);
            }
        }
        private string _sourceDir;

        public string SMTPServer 
        { 
            get 
            { 
                return this._smtpServer; 
            } 
            internal set
            {
                this._smtpServer = value;
            }
        }
        private string _smtpServer;

        public int SMTPPort 
        { 
            get 
            { 
                return this._smtpPort; 
            } 
            internal set
            {
                this._smtpPort = value;
            }
        }
        private int _smtpPort;

        public string EmailFrom
        {
            get
            {
                return this._emailFrom;
            }
            internal set
            {
                this._emailFrom = value;
            }
        }
        private string _emailFrom;

        public List<string> EmailTo
        {
            get
            {
                return this._emailTo;
            }
        }
        private List<string> _emailTo;

        public int ImgWidth 
        { 
            get 
            { 
                return this._imgWidth; 
            } 
            internal set
            {
                this._imgWidth = value;
            }
        }
        private int _imgWidth;

        public int ImgHeight 
        { 
            get 
            { 
                return this._imgHeight; 
            } 
            internal set
            {
                this._imgHeight = value;
            }
        }
        private int _imgHeight;

        public string LogErrorDir 
        { 
            get 
            { 
                return this._logErrorDir; 
            } 
            internal set
            {
                if (Directory.Exists(value))
                    this._logErrorDir = value;
                else
                    throw new DirectoryNotFoundException("Directory does not exist. " + value);
            }
        }
        private string _logErrorDir;

        public string LogMissPosterDir 
        { 
            get 
            { 
                return this._logMissPosterDir; 
            } 
            internal set
            {
                if (Directory.Exists(value))
                    this._logMissPosterDir = value;
                else
                    throw new DirectoryNotFoundException("Directory does not exist. " + value);
            }
        }
        private string _logMissPosterDir;

        public int MaxThreads
        {
            get
            {
                return this._maxThreads;
            }
            internal set
            {
                this._maxThreads = value;
            }
        }
        private int _maxThreads;
        #endregion Properties

        #region Constructor
        /// <summary>
        /// NG Vod Poster configuration parameters
        /// </summary>
        private NGVodPosterConfig()
        {
            this._vhos = new Dictionary<string, NGVodVHO>();
            this._emailTo = new List<string>();
        }
        #endregion Constructor

        #region Public Methods
        public static NGVodPosterConfig GetConfig()
        {

            var cfgHelper = new CfgHelper();

            //Load xml config
            var config = cfgHelper.GetConfig("NGVODPoster.xml");

            if (config == null)
            {
                Trace.TraceError("Failed to gete configuration");
                throw new Exception("Failed to load configuration xml file.");
            }
            
            //Get namespace
            var ns = config.Root.GetDefaultNamespace();

            var ngVodConfig = new NGVodPosterConfig();

            List<Task> tskList = new List<Task>()
            {
                ngVodConfig.setVHO(config, ns),
                ngVodConfig.setDirs(config, ns),
                ngVodConfig.setImage(config, ns),
                ngVodConfig.setLogDir(config, ns),
                ngVodConfig.setSMTP(config, ns),
                ngVodConfig.setMaxThreads(config, ns),
            };

            try
            {
                Task.WaitAll(tskList.ToArray());
                ngVodConfig.setVhoDirs();
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.InnerExceptions)
                {
                    Trace.TraceError("Error setting config properties. {0}", ex.Message);
                }
                throw aex.Flatten();
            }

            return ngVodConfig;
        }

        public void AddEmailTo(string address)
        {
            Trace.WriteLine("addEmailTo called");
            if (System.Text.RegularExpressions.Regex.IsMatch(address, @"^([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,5})$", System.Text.RegularExpressions.RegexOptions.Singleline))
                this._emailTo.Add(address);
            else
                throw new Exception("Invalid send to email address provided");
        }
        #endregion Public Methods

        #region Private Methods
        /// <summary>
        /// Sets the poster destination directory for each VHO using
        /// the UNC path admin share
        /// </summary>
        private void setVhoDirs()
        {
            Trace.WriteLine("setVhoDirs called");
            foreach(var vho in this.Vhos)
            {
                string dir = this.DestinationDir;
                dir = dir.Replace(':', '$');

                string svr = @"\\" + vho.Value.WebServerName;

                dir = Path.Combine(svr, dir);

                vho.Value.PosterDir = dir;
            }
        }

        /// <summary>
        /// Sets the VHO dictionary by getting the VHO details from the xml
        /// </summary>
        /// <param name="config">XML config file</param>
        /// <param name="ns">Namespace of the config file</param>
        /// <returns></returns>
        private Task setVHO(XDocument config, XNamespace ns)
        {
            Trace.WriteLine("setVHO called");
            var vhoElements = config.Root.Descendants(ns + "VHO");
            var exceptions = new ConcurrentQueue<Exception>();
            var vhoDict = new Dictionary<string, NGVodVHO>();

            Parallel.ForEach(vhoElements, (el) =>
            {
                try
                {
                    var vho = new NGVodVHO();

                    vho.Name = el.Attribute("Name").Value;
                    vho.WebServerName = el.Element(ns + "PrimaryWebServer").Value;
                    vho.IMGDb.DatabaseName = el.Element(ns + "IMGDb").Attribute("Name").Value;
                    vho.IMGDb.DataSource = el.Element(ns + "IMGDb").Attribute("InstanceName").Value;

#if DEBUG
                    Trace.WriteLine(string.Format("Name: {0} | WS: {1} | DBName: {2} | DBSource: {3}", vho.Name, vho.WebServerName, vho.IMGDb.DatabaseName, vho.IMGDb.DataSource));
#endif
                    vhoDict.Add(vho.Name, vho);
                }
                catch (NullReferenceException nre)
                {
                    Trace.TraceError("Error in setVHO. {0}", nre.Message);
                    exceptions.Enqueue(nre);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error in setVHO. {0}", ex.Message);
                    exceptions.Enqueue(ex);
                }
            });

            this._vhos = vhoDict;

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// Sets the height and width parameters for an image to be resized to when saved
        /// </summary>
        /// <param name="config">XML config file</param>
        /// <param name="ns">Namespace of the config file</param>
        /// <returns></returns>
        private Task setImage(XDocument config, XNamespace ns)
        {
            Trace.WriteLine("setImage called");
            var exceptions = new ConcurrentQueue<Exception>();
            var paramEle = config.Root.Element(ns + "Parameters");

            try
            {
                this.ImgHeight = int.Parse(paramEle.Element(ns + "ImageHeightPx").Value);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in setImage while setting height. {0}", ex.Message);
                exceptions.Enqueue(new Exception("Failed to set image height in config. " + ex.Message));
            }

            try
            {
                this.ImgWidth = int.Parse(paramEle.Element(ns + "ImageWidthPx").Value);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in setImage while setting width. {0}", ex.Message);
                exceptions.Enqueue(new Exception("Failed to set image width in config. " + ex.Message));
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions).Flatten();

            return Task.FromResult(0);
        }

        /// <summary>
        /// Sets the error, and missing poster log directory
        /// </summary>
        /// <param name="config">XML config file</param>
        /// <param name="ns">Namespace of the config file</param>
        /// <returns></returns>
        private Task setLogDir (XDocument config, XNamespace ns)
        {
            Trace.WriteLine("setLogDir called");
            if (config.Root.Elements().Any(x => x.Name.Equals(ns + "Log")) 
                && config.Root.Element(ns + "Log").Elements().Any(x => x.Name.Equals(ns + "ErrorLogDir")))
            {
                var exceptions = new ConcurrentQueue<Exception>();
                var logEle = config.Root.Element(ns + "Log");

                try
                {
                    this.LogErrorDir = logEle.Elements().Any(x => x.Name.LocalName.Contains("ErrorLogDir")) ? logEle.Element(ns + "ErrorLogDir").Value : Directory.GetCurrentDirectory();
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error in setLogDir. Could not find ErrorLogDir element. {0}", ex.Message);
                    this.LogErrorDir = Directory.GetCurrentDirectory();
                    exceptions.Enqueue(new Exception("Failed to set error log directory in config. " + ex.Message));
                }

                try
                {
                    this.LogMissPosterDir = logEle.Elements().Any(x => x.Name.LocalName.Contains("MissingPosterLogDir")) ? logEle.Element(ns + "MissingPosterLogDir").Value : Directory.GetCurrentDirectory();
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error in sestLogDir. Could not find MissingPosterLogDir element. {0}", ex.Message);
                    this.LogMissPosterDir = Directory.GetCurrentDirectory();
                    exceptions.Enqueue(new Exception("Failed to set missing poster log directory in config. " + ex.Message));
                }

                if (exceptions.Count > 0)
                    throw new AggregateException(exceptions).Flatten();
            }
            else
            {
                this.LogErrorDir = Directory.GetCurrentDirectory();
                this.LogMissPosterDir = Directory.GetCurrentDirectory();
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// Set the email parameter values
        /// </summary>
        /// <param name="config">XML config file</param>
        /// <param name="ns">Namespace of the config file</param>
        /// <returns></returns>
        private Task setSMTP (XDocument config, XNamespace ns)
        {
            Trace.WriteLine("setSMTP called");
            if (config.Root.Elements().Any(x => x.Name.Equals(ns + "Email")))
            {
                var exceptions = new ConcurrentQueue<Exception>();
                var emailEle = config.Root.Element(ns + "Email");

                try
                {
                    this.SMTPServer = emailEle.Element(ns + "Server").Value;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error in setSMTP. Could not find Server element. {0}", ex.Message);
                    exceptions.Enqueue(new Exception("Failed to set SMTP server value in config. " + ex.Message));
                }

                try
                {
                    this.SMTPPort = emailEle.Elements().Any(x => x.Name.LocalName.Contains("Port")) ? int.Parse(emailEle.Element(ns + "Port").Value) : 25;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error in setSMTP. Could not find Port element. {0}", ex.Message);
                    exceptions.Enqueue(new Exception("Failed to set SMTP port value in config. " + ex.Message));
                }

                try
                {
                    this.EmailFrom = emailEle.Element(ns + "SendFrom").Value;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error in setSMTP. Could not find Email element. {0}", ex.Message);
                    exceptions.Enqueue(new Exception("Failed to set send from email address. " + ex.Message));
                }

                try
                {
                    this.EmailTo.AddRange(emailEle.Elements(ns + "SendTo").Select(x => x.Value));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error in setSMTP. Could not find SendTo element. {0}", ex.Message);
                    exceptions.Enqueue(new Exception("Failed to set send to email addresses. " + ex.Message));
                }

                if (exceptions.Count > 0)
                    throw new AggregateException(exceptions);
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// Sets the (local)poster destination and poster source directory
        /// </summary>
        /// <param name="config">XML config file</param>
        /// <param name="ns">Namespace of the config file</param>
        /// <returns></returns>
        private Task setDirs (XDocument config, XNamespace ns)
        {
            Trace.WriteLine("setDirs called");
            var exceptions = new ConcurrentQueue<Exception>();

            try
            {
                this.DestinationDir = config.Root.Element(ns + "PosterDestDir").Value;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in setDirs. Could not find PosterDestDir element. {0}", ex.Message);
                exceptions.Enqueue(new Exception("Failed to set destination directory in config. " + ex.Message));
            }

            try
            {
                this.SourceDir = config.Root.Element(ns + "PosterSourceDir").Value;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in setDirs. Could not find PosterSourceDir element. {0}", ex.Message);
                exceptions.Enqueue(new Exception("Failed to set source directory in config. " + ex.Message));
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);

            return Task.FromResult(0);
        }

        /// <summary>
        /// Sets the maximum amount of threads to be opened during asyncronous operations
        /// </summary>
        /// <param name="config">XML config file</param>
        /// <param name="ns">Namespace of the config file</param>
        /// <returns></returns>
        private Task setMaxThreads (XDocument config, XNamespace ns)
        {
            Trace.WriteLine("setMaxThreads called");
            this.MaxThreads = config.Root.Elements().Any(x => x.Name.LocalName.Contains("MaxThreads")) ? int.Parse(config.Root.Element(ns + "MaxThreads").Value) : System.Environment.ProcessorCount;

            return Task.FromResult(0);
        }
        #endregion Private Methods
    }
}
