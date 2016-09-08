using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.FiOS.Servers.Components
{
    public class HardDrive
    {
        public string DriveLetter { get; set; }
        public string Label { get; set; }
        public long Capacity { get { return this._capacity; } set { this._capacity = value; } }
        public long FreeSpace { get { return this._freeSpace; } set { this._freeSpace = value; } }
        public DriveAvailabilities Availability { get; set; }
        public DriveTypes DriveType { get; set; }
        public string FileSystem { get; set; }
        public string Name { get; set; }
        public string SerialNumber { get; set; }
        public DriveStatus Status { get; set; }

        private long _capacity;
        private long _freeSpace;

        /// <summary>
        /// Gets physical and network hard drive information from a remote computer
        /// </summary>
        /// <param name="ServerName">Name of the remote server</param>
        /// <returns>Hard drive information</returns>
        public static HardDrive GetHardDrive(string ServerName)
        {
            var hdd = new HardDrive();

            string scopeStr = string.Format(@"\\{0}\root\cimv2", ServerName);

            ManagementScope scope = new ManagementScope(scopeStr);
            scope.Connect();

            //Drive is niether a RAM disk (6) or a Compact disk (5) or a Removeable Disk (2)
            SelectQuery query = new SelectQuery("SELECT * FROM Win32_Volume WHERE DriveType != 6 AND DriveType != 5 AND DriveType != 2");

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
            {
                foreach (ManagementObject disk in searcher.Get())
                {
                    if (disk["DriveLetter"] != null)
                        hdd.DriveLetter = disk["DriveLetter"].ToString();
                    if (disk["Label"] != null)
                        hdd.Label = disk["Label"].ToString();
                    if (disk["FreeSpace"] != null)
                        long.TryParse(disk["FreeSpace"].ToString(), out hdd._freeSpace);
                    if (disk["Capacity"] != null)
                        long.TryParse(disk["Capacity"].ToString(), out hdd._capacity);
                    if (disk["DriveType"] != null)
                        hdd.DriveType = (DriveTypes)int.Parse(disk["DriveType"].ToString());
                    if (disk["SerialNumber"] != null)
                        hdd.SerialNumber = disk["SerialNumber"].ToString();
                    if (disk["Availability"] != null)
                        hdd.Availability = (DriveAvailabilities)int.Parse(disk["Availability"].ToString());
                    if (disk["Name"] != null)
                        hdd.Name = disk["Name"].ToString();
                    if (disk["Status"] != null)
                        hdd.Status = (DriveStatus)int.Parse(disk["Status"].ToString());
                }
            }

            return hdd;
        }

        /// <summary>
        /// Gets physical and network hard drive information from a remote computer asyncronously
        /// </summary>
        /// <param name="ServerName">Name of the remote server</param>
        /// <returns>Hard drive information</returns>
        public static async Task<HardDrive[]> GetHardDriveAsync(string ServerName)
        {
            List<HardDrive> hddList = new List<HardDrive>();

            string scopeStr = string.Format(@"\\{0}\root\cimv2", ServerName);

            ManagementScope scope = new ManagementScope(scopeStr);
            scope.Connect();

            //Drive is niether a RAM disk (6) a Compact disk (5), or a Removeable Disk (2)
            SelectQuery query = new SelectQuery("SELECT * FROM Win32_Volume WHERE DriveType != 6 AND DriveType != 5 AND DriveType != 2");

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
            {
                foreach (ManagementObject disk in searcher.Get())
                {
                    var hdd = new HardDrive();

                    if (disk["DriveLetter"] != null)
                        hdd.DriveLetter = disk["DriveLetter"].ToString();
                    if (disk["Label"] != null)
                        hdd.Label = disk["Label"].ToString();
                    if (disk["FreeSpace"] != null)
                        long.TryParse(disk["FreeSpace"].ToString(), out hdd._freeSpace);
                    if (disk["Capacity"] != null)
                        long.TryParse(disk["Capacity"].ToString(), out hdd._capacity);
                    if (disk["DriveType"] != null)
                        hdd.DriveType = (DriveTypes)int.Parse(disk["DriveType"].ToString());
                    if (disk["SerialNumber"] != null)
                        hdd.SerialNumber = disk["SerialNumber"].ToString();
                    if (disk["Availability"] != null)
                        hdd.Availability = (DriveAvailabilities)int.Parse(disk["Availability"].ToString());
                    if (disk["Name"] != null)
                        hdd.Name = disk["Name"].ToString();
                    if (disk["Status"] != null)
                        hdd.Status = (DriveStatus)int.Parse(disk["Status"].ToString());

                    hddList.Add(hdd);
                }
            }

            return await Task.FromResult<HardDrive[]>(hddList.ToArray());
        }

        public enum DriveAvailabilities
        {
            Other = 1,
            Unknown = 2,
            Running = 3,
            Warning = 4,
            Testing = 5,
            NotApplicable = 6,
            PowerOff = 7,
            Offline = 8,
            OffDuty = 9,
            Degraded = 10,
            NotInstalled = 11,
            InstallError = 12,
            PowerCycle = 16,
            Paused = 18,
            NotReady = 19,
            NotConfigured = 20,
        }

        public enum DriveTypes
        {
            Unknown = 0,
            NoRootDirectory = 1,
            RemovableDisk = 2,
            LocalDisk = 3,
            NetworkDrive = 4,
            CompactDisk = 5,
            RAMDisk = 6,
        }

        public enum DriveStatus
        {
            OK,
            Error,
            Degraded,
            Unknown,
            PredFail,
            Starting,
            Stopping,
            Service,
            Stressed,
            NonRecover,
            NoContact,
            LostComm,
        }
    }
}
