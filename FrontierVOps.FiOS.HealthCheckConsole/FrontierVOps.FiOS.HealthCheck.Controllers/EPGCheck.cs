using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrontierVOps.Common.FiOS;
using FrontierVOps.Data;
using FrontierVOps.Data.Objects;
using FrontierVOps.FiOS.HealthCheck.DataObjects;
using FrontierVOps.FiOS.Servers.Objects;

namespace FrontierVOps.FiOS.HealthCheck.Controllers
{
    public class EPGCheck
    {
        public int Progress { get { return _progress; } set { _progress = value; } }
        private int _progress;

        private string beIMGAppConnectionStr;
        private string beIMGAdminConnectionStr;
        private bool isCancelRequested;

        public HealthRollupCollection hruCol { get; set; }

        public EPGCheck()
        {
            try
            {
                var beIMGDatasource = DBConfig.GetDBs().Where(x => x.Type == DSType.TSQL && x.Role == FiOSRole.AdminConsole).First();

                this.beIMGAppConnectionStr = beIMGDatasource.CreateConnectionString(beIMGDatasource.Databases.Where(x => x.Location == FiOSLocation.VHE && x.Function == DbFunction.Application).FirstOrDefault());
                this.beIMGAdminConnectionStr = beIMGDatasource.CreateConnectionString(beIMGDatasource.Databases.Where(x => x.Location == FiOSLocation.VHE && x.Function == DbFunction.Admin).FirstOrDefault());

                if (string.IsNullOrEmpty(this.beIMGAppConnectionStr))
                    throw new Exception("IMG Application connection string cannot be null.");

                if (string.IsNullOrEmpty(this.beIMGAdminConnectionStr))
                    throw new Exception("IMG Admin connection string cannot be null.");

                hruCol = new HealthRollupCollection(() => new HealthRollup());
                isCancelRequested = false;
            }
            catch(Exception ex)
            {
                throw new Exception("No SQL datasources could be found in the configuration for Admin Console. " + ex.Message);
            }
        }

        /// <summary>
        /// Cancels all running loops.
        /// </summary>
        public void Cancel()
        {
            this.isCancelRequested = true;
        }

        public async Task<HealthRollupCollection> CheckSliceFilesAsync()
        {
            return await CheckSliceFilesAsync(null);
        }

        public async Task<HealthRollupCollection> CheckSliceFilesAsync(FiOSServer[] Servers)
        {
            this._progress = 1;
            
            //var beIMGDatasources = DBConfig.GetDBs().Where(x => x.Type == DSType.TSQL && x.Role == FiOSRole.AdminConsole);

            //if (beIMGDatasources.Count() < 1)
            //{
            //    throw new Exception("No SQL datasources could be found in the configuration for Admin Console.");
            //}

            //var beAppDb = beIMGDatasources.SelectMany(x => x.Databases.Where(y => y.Location == FiOSLocation.VHE && y.Function == DbFunction.Application)).FirstOrDefault();
            //var beAdminDb = beIMGDatasources.SelectMany(x => x.Databases.Where(y => y.Location == FiOSLocation.VHE && y.Function == DbFunction.Admin)).FirstOrDefault();

            IEnumerable<FiOSWebServer> feIMGWebServers = null;
            if (Servers == null)
            {
                feIMGWebServers = (await getWebServers(this.beIMGAppConnectionStr)).Where(x => x.HostFunction == ServerFunction.Web && x.HostRole == FiOSRole.IMG);
            }
            else
            {
                feIMGWebServers = Servers.Where(x => x.HostRole == FiOSRole.IMG && x.HostFunction == ServerFunction.Web).Select(x => x as FiOSWebServer);
            }

            var drvLetter = await getDriveLetter(this.beIMGAdminConnectionStr);

            var epgRgnPaths = await getRegionPath(this.beIMGAppConnectionStr, feIMGWebServers.Select(x => x.HostLocationName).ToArray());

            if (feIMGWebServers.Count() == 0)
                throw new Exception("No front end web servers were found.");

            if (string.IsNullOrEmpty(drvLetter))
                throw new Exception("Could not find local drive letter.");

            if (epgRgnPaths.Count() == 0)
                throw new Exception("No EPG regions could be found.");

            var currentDate = DateTime.Today;

            //If the tool is run between midnight and 3am, then set the current date to yesterday
            if (currentDate.Hour < 4)
            {
                currentDate = currentDate.AddDays(-1);
            }

            int totalServers = feIMGWebServers.Count();
            int index = 0;


            foreach (var svr in feIMGWebServers.ToList())
            {
                        if (this.isCancelRequested)
                        {
                            continue;
                        }
                        var hru = new HealthRollup();
                        var hce = new HealthCheckError();
                        hce.HCType = HealthCheckType.EPG;
                        hce.Result = StatusResult.Ok;
                        hru.Server = svr as FiOSWebServer;

                        var epgRgns = epgRgnPaths.Where(x => x.VHOId == svr.HostLocationName);

                        string sliceDir = string.Empty, dbDir = string.Empty;

                        foreach (var region in epgRgns)
                        {

                            if (region.StbType == STBType.QIP)
                            {
                                string[] stbModelTypes = new string[] { "BigEndian", "LittleEndian" };

                                for (int i = 0; i < 2; i++)
                                {
                                    sliceDir = Path.Combine(region.ApplicationPath, stbModelTypes[i]);
                                    if (!(Directory.Exists(sliceDir)))
                                    {
                                        sliceDir = @"\\" + svr.HostName + "\\" + sliceDir.Replace(":", "$");
                                        if (!(Directory.Exists(sliceDir)))
                                            hce.Error.Add(string.Format("{0} - Could not find QIP directory at {1} for region {2}.", StatusResult.Warning, sliceDir, region.VirtualChannel));
                                    }
                                    sliceDir = Directory.EnumerateDirectories(sliceDir).Where(x => x.Contains(region.VHOId) && x.Contains(region.VirtualChannel)).FirstOrDefault();

                                    if (region.SlicerVersion == null)
                                        hce.Error.Add(string.Format("{0} - QIP slicer version not found for region {1}.", StatusResult.Warning, region.VirtualChannel));
                                    else if (string.IsNullOrEmpty(sliceDir))
                                    {
                                        hce.Error.Add(string.Format("{0} - Could not find QIP slice file directory for region {1}.", StatusResult.Warning, region.VirtualChannel));
                                        continue;
                                    }

                                    sliceDir = Path.Combine(sliceDir, region.SlicerVersion);
                                    sliceDir = @"\\" + svr.HostFullName + sliceDir.Replace(":", "$");

                                    if (!Directory.Exists(sliceDir))
                                    {
                                        hce.Result = GenericChecks.getCorrectStatusResult(hce.Result, StatusResult.Critical);
                                        hce.Error.Add(string.Format("{0} - Unable to find QIP slice file directory at path: {1} for region {2}.", StatusResult.Critical, sliceDir, region.RegionId));
                                        continue;
                                    }

                                    var files = Directory.EnumerateFiles(sliceDir, "*.bin", SearchOption.AllDirectories);

                                    //Check timestamps
                                    var tsResult = checkSliceFileTimeStamp(files);
                                    if (!string.IsNullOrEmpty(tsResult))
                                    {
                                        hce.Result = GenericChecks.getCorrectStatusResult(hce.Result, StatusResult.Error);
                                        hce.Error.Add(tsResult);
                                    }

                                    //Check guide data day count
                                    if (files.Count() < 15)
                                    {
                                        hce.Result = GenericChecks.getCorrectStatusResult(hce.Result, StatusResult.Critical);
                                        hce.Error.Add(string.Format("{0} - Expecting 15 slice files in {1} but only found {2}. Verify there are 14 days worth of guide data for region {3}.",
                                            StatusResult.Critical, sliceDir, files.Count(), region.RegionId));
                                    }
                                }
                            }
                            else if (region.StbType == STBType.VMS)
                            {
                                dbDir = Path.Combine(region.DbFilePath, region.VHOId, region.VirtualChannel, region.SlicerVersion);
                                sliceDir = Path.Combine(region.ApplicationPath, region.VirtualChannel, region.SlicerVersion);

                                dbDir = @"\\" + svr.HostName + "\\" + dbDir.Replace(":", "$");
                                sliceDir = @"\\" + svr.HostName + "\\" + sliceDir.Replace(":", "$");

                                bool skip = false;
                                if (!Directory.Exists(sliceDir))
                                {
                                    hce.Result = GenericChecks.getCorrectStatusResult(hce.Result, StatusResult.Critical);
                                    hce.Error.Add(string.Format("{0} - Unable to find VMS slice file directory at path: {1} for region {2}.", StatusResult.Critical, sliceDir, region.RegionId));
                                    skip = true;
                                }

                                if (!Directory.Exists(dbDir))
                                {
                                    hce.Result = GenericChecks.getCorrectStatusResult(hce.Result, StatusResult.Critical);
                                    hce.Error.Add(string.Format("{0} - Unable to find VMS db file directory at path: {1} for region {2}.", StatusResult.Critical, sliceDir, region.RegionId));
                                    skip = true;
                                }

                                if (skip)
                                    continue;

                                var sliceFiles = Directory.EnumerateFiles(sliceDir, "*.json", SearchOption.AllDirectories);
                                var dbFiles = Directory.EnumerateFiles(dbDir).Where(x => x.ToLower().EndsWith("json") || x.ToLower().EndsWith("gz"));

                                //Check timestamps
                                var tsResult = new List<string>() { checkSliceFileTimeStamp(sliceFiles), checkSliceFileTimeStamp(dbFiles) };

                                tsResult.ForEach(x =>
                                    {
                                        if (!string.IsNullOrEmpty(x))
                                        {
                                            hce.Result = GenericChecks.getCorrectStatusResult(hce.Result, StatusResult.Error);
                                            hce.Error.Add(x);
                                        }
                                    });

                                //Check guide data day count
                                //Extract all of the dates from the schedule files
                                var sliceSCHFiles = sliceFiles.Where(x => x.ToUpper().Contains("_SCH_")).Select(x => Path.GetFileNameWithoutExtension(x).Split('_')[2]).Distinct();
                                List<DateTime> sliceFileDates = new List<DateTime>();

                                foreach (var sliceSCHFile in sliceSCHFiles)
                                {
                                    DateTime dtSliceFile = DateTime.Today;
                                    if (!DateTime.TryParseExact(sliceSCHFile, "MMddyyyy", null, System.Globalization.DateTimeStyles.None, out dtSliceFile))
                                    {
                                        hce.Result = GenericChecks.getCorrectStatusResult(hce.Result, StatusResult.Warning);
                                        hce.Error.Add(string.Format("{0} - Unable to extract date from VMS epg schedule slice file {1} for region {2}.", StatusResult.Warning, sliceSCHFile, region.RegionId));
                                    }
                                    else
                                    {
                                        sliceFileDates.Add(dtSliceFile);
                                    }
                                }

                                sliceFileDates = sliceFileDates.Distinct().ToList();

                                if (sliceFileDates.Any(x => x.Date < currentDate.Date))
                                {
                                    var sfd = sliceFileDates.Min(x => x.Date);
                                    hce.Result = GenericChecks.getCorrectStatusResult(hce.Result, StatusResult.Error);
                                    hce.Error.Add(string.Format("{0} - Old EPG data exists at {1} for region {2}. Earliest guide data date is {3} but should be {4}.", StatusResult.Error, sliceDir, region.RegionId, sfd.Date.ToString("MM/dd/yyyy"), currentDate.Date.ToString("MM/dd/yyyy")));
                                }

                                if (sliceFileDates.Count < 15)
                                {
                                    hce.Result = GenericChecks.getCorrectStatusResult(hce.Result, StatusResult.Error);
                                    hce.Error.Add(string.Format("{0} - There are not 14 days worth of guide data in {1} for region {2}. Current number of days: {3}", StatusResult.Error, sliceDir, region.RegionId, sliceFileDates.Count));
                                }
                            }
                        }

                        hru.Errors.Add(hce);
                        this.hruCol.PutObject(hru);
                        this._progress = (int)(((decimal)++index / (decimal)totalServers) * 100);
                }

            return await Task.FromResult<HealthRollupCollection>(hruCol);
        }

        public async Task<string[]> GetIMGVersionsAsync(FiOSServer Server)
        {
            if (string.IsNullOrEmpty(this.beIMGAppConnectionStr))
            {
                throw new Exception("No connection string for the backend IMG application database could be found.");
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("SELECT DISTINCT c.strSlicerVersionId FROM tFiOSVHOServerMap a (NOLOCK)");
            sb.AppendLine("INNER JOIN tFIOSRegion b (NOLOCK) ON b.strVHOId = a.strVHOId");
            sb.AppendLine("INNER JOIN tFIOSSliceFileVersion c (NOLOCK) ON c.strFiosRegionID = b.strFIOSRegionId");
            sb.AppendLine("INNER JOIN tFIOSServers d (NOLOCK) ON d.strServerId = a.strServerId");
            sb.AppendLine("INNER JOIN tFIOSDataSlicerRegionsProcess e (NOLOCK) ON e.strFIOSRegionId = b.strFIOSRegionId AND e.strSlicerVersionID = c.strSlicerVersionId");
            sb.AppendFormat("WHERE strServerTypeId = 'LOADBAL' AND a.strServerId = {0}", Server.HostName);

            List<string> versions = new List<string>();

            await DBFactory.SQL_ExecuteReaderAsync(this.beIMGAppConnectionStr, sb.ToString(), System.Data.CommandType.Text, null, (dr) =>
                {
                    while (dr.Read())
                    {
                        versions.Add(dr.GetString(0));
                    }
                });

            return await Task.FromResult<string[]>(versions.ToArray());
        }

        private string checkSliceFileTimeStamp(IEnumerable<string> files)
        {
            var currentDate = DateTime.Today.Date;

            //If the tool is run between midnight and 3am, then set the current date to yesterday
            if (DateTime.Now.Hour < 4)
            {
                currentDate = currentDate.AddDays(-1);
            }

            foreach(var file in files)
            {
                FileInfo fi = new FileInfo(file);

                if (fi.LastWriteTime.Date < currentDate)
                {
                    return string.Format("{0} - Slice files out of date. Slice file date: {1}", StatusResult.Error, fi.LastWriteTime.ToString("MM/dd/yy"));
                }
            }
            return string.Empty;
        }

        private async Task<string> getDriveLetter(string connectionStr)
        {
            string query = "SELECT param_value FROM tFIOSParameters WHERE param_name = 'DRVLetter'";

            string result = string.Empty;
            await DBFactory.SQL_ExecuteReaderAsync(connectionStr, query, System.Data.CommandType.Text, null, (dr) =>
                {
                    if (dr.Read())
                        result = dr.IsDBNull(0) ? string.Empty : dr.GetString(0);
                });
            return await Task.FromResult<string>(result);
        }

        private async Task<IEnumerable<EPGRegionPath>> getRegionPath(string connectionStr, string[] vhoIds)
        {
            StringBuilder sbQIP = new StringBuilder();
            StringBuilder sbVMS = new StringBuilder();

            //QIP QUERY
            sbQIP.Append("SELECT DISTINCT a.strFIOSRegionId, a.strVHOId, b.strSlicerVersionId, ");
            sbQIP.AppendLine("c.strFiOSVersionAliasId, c.strDestBaseDirectory, a.strVirtualChannelPosition FROM tFIOSRegion a (NOLOCK)");
            sbQIP.AppendLine("INNER JOIN tFIOSSliceFileVersion b (NOLOCK) ON a.strFIOSRegionId = b.strFiosRegionID");
            sbQIP.AppendLine("INNER JOIN tFiOSVersionAlias c (NOLOCK) ON b.strSlicerVersionId = c.strSlicerVersionID ");
            sbQIP.AppendFormat("WHERE a.strVHOId IN ({0})", string.Join(",", vhoIds.Select(x => "'" + x + "'")));

            //VMS QUERY
            sbVMS.Append("SELECT DISTINCT a.strFIOSRegionId, a.strVHOId, b.strSlicerVersionId, ");
            sbVMS.AppendLine("d.strValue as strFiOSVersionAliasId, f.strValue as strDestBaseDirectory, e.strValue as strDbDirectory, a.strVirtualChannelPosition");
            sbVMS.AppendLine("FROM tFIOSRegion a (NOLOCK)");
            sbVMS.AppendLine("INNER JOIN tFIOSSliceFileVersion b (NOLOCK) ON a.strFIOSRegionId = b.strFiosRegionID");
            sbVMS.AppendLine("INNER JOIN tVMSepgProbeConfig c (NOLOCK) ON b.strSlicerVersionId = c.strSlicerVersion");
            sbVMS.AppendLine("INNER JOIN tVMSepgProbeConfig d (NOLOCK) ON c.strSlicerVersion = d.strSlicerVersion AND d.strKey = 'strVirtualPathAlias'");
            sbVMS.AppendLine("INNER JOIN tVMSepgProbeConfig e (NOLOCK) ON c.strSlicerVersion = e.strSlicerVersion AND e.strKey = 'SqliteDbPath'");
            sbVMS.AppendLine("INNER JOIN tVMSepgProbeConfig f (NOLOCK) ON c.strSlicerVersion = f.strSlicerVersion AND f.strKey = 'JsonPath' ");
            sbVMS.AppendFormat("WHERE a.strVHOId IN ({0})", string.Join(",", vhoIds.Select(x => "'" + x + "'")));

            var result = new List<EPGRegionPath>();
            await DBFactory.SQL_ExecuteReaderAsync(connectionStr, sbQIP.ToString(), System.Data.CommandType.Text, null, (dr) =>
                {
                    while (dr.Read())
                    {
                        var epgRP = new EPGRegionPath();
                        epgRP.RegionId = dr.GetString(0);
                        epgRP.VHOId = dr.GetString(1);
                        epgRP.SlicerVersion = dr.GetString(2);
                        epgRP.FiOSVersion = dr.GetString(3);
                        epgRP.ApplicationPath = dr.GetString(4);
                        epgRP.VirtualChannel = dr.GetString(5);
                        //epgRP.DbFilePath = dr.GetString(5);

                        if (!epgRP.ApplicationPath.ToUpper().Contains("SLICEFILES"))
                        {
                            epgRP.ApplicationPath = System.IO.Path.Combine(epgRP.ApplicationPath, "SliceFiles");
                        }

                        result.Add(epgRP);
                    }
                });

            await DBFactory.SQL_ExecuteReaderAsync(connectionStr, sbVMS.ToString(), System.Data.CommandType.Text, null, (dr) =>
                {
                    while (dr.Read())
                    {
                        var epgRP = new EPGRegionPath();
                        epgRP.RegionId = dr.GetString(0);
                        epgRP.VHOId = dr.GetString(1);
                        epgRP.SlicerVersion = dr.GetString(2);
                        epgRP.FiOSVersion = dr.GetString(3);
                        epgRP.ApplicationPath = dr.GetString(4);
                        epgRP.DbFilePath = dr.GetString(5);
                        epgRP.VirtualChannel = dr.GetString(6);

                        result.Add(epgRP);
                    }
                });


            return await Task.FromResult<IEnumerable<EPGRegionPath>>(result.Distinct());
        }

        private async Task<IEnumerable<FiOSWebServer>> getWebServers(string connectionStr)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("SELECT a.strServerId, a.strServerIP, a.strServerTypeId, b.strVHOId");
            sb.AppendLine("FROM tFiOSServers a (NOLOCK)");
            sb.AppendLine("LEFT OUTER JOIN tFiOSVHOServerMap b (NOLOCK) ON b.strServerId = a.strServerId");

            var result = new List<FiOSWebServer>();
            await DBFactory.SQL_ExecuteReaderAsync(connectionStr, sb.ToString(), System.Data.CommandType.Text, null, (dr) =>
                {
                    while (dr.Read())
                    {
                        var svr = new FiOSWebServer();
                        svr.HostName = dr.GetString(0);
                        svr.IPAddress = dr.GetString(1);
                        string svrType = dr.GetString(2).ToUpper();
                        svr.HostRole = svrType.Equals("LOADBAL") ? FiOSRole.IMG : svrType.Equals("DCSLICER") ? FiOSRole.AdminConsole : FiOSRole.Unknown;
                        if (svr.HostRole == FiOSRole.IMG)
                        {
                            svr.HostLocation = ServerLocation.VHO;
                            svr.HostLocationName = dr.IsDBNull(3) ? null : dr.GetString(3);
                        }
                        else if (svr.HostRole == FiOSRole.AdminConsole)
                        {
                            svr.HostLocation = ServerLocation.VHE;
                            svr.HostLocationName = "VHE";
                        }
                        svr.HostFunction = ServerFunction.Web;
                        result.Add(svr);
                    }
                });

            return await Task.FromResult<IEnumerable<FiOSWebServer>>(result);
        }

        private class EPGRegionPath
        {
            internal string RegionId { get; set; }
            internal string VHOId { get; set; }   
            internal string FiOSVersion { get; set; }
            internal string ApplicationPath { get; set; }
            internal string DbFilePath { get; set; }
            internal STBType StbType { get; private set; }
            internal string VirtualChannel { get; set; }
            internal string SlicerVersion 
            { 
                get
                {
                    return this._slicerVersion;
                }
                set
                {
                    this._slicerVersion = value;
                    switch(this._slicerVersion)
                    {
                        case "1_0_5":
                            this.StbType = STBType.QIP;
                            break;
                        case "1_0_10":
                            this.StbType = STBType.VMS;
                            break;
                        default:
                            this.StbType = STBType.QIP;
                            break;
                    }
                }
            }
            private string _slicerVersion;
        }
    }
}
