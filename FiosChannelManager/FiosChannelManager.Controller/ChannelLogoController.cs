using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Linq.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FrontierVOps.Data;
using FiosChannelManager.DataObjects;

namespace FiosChannelManager.Controller
{
    public class ChannelLogoController
    {
        public string ConnectionString { get { return this._connectionString; } set { this._connectionString = value; } }
        private string _connectionString;

        public string LogoRepository {get {return this._logoRepository; } set { this._logoRepository = value; } }
        private string _logoRepository;

        public CancellationTokenSource CancelTokenSource { get; private set; }

        private IEnumerable<FiOSChannel> _fiosChannels;

        public ChannelLogoController(string LogoRepository)
        {
            if (!(Directory.Exists(LogoRepository)))
            {
                throw new DirectoryNotFoundException(string.Format("Could not find directory at {0}.", LogoRepository));
            }
            this.CancelTokenSource = new CancellationTokenSource();
            this._logoRepository = LogoRepository;
            this._fiosChannels = ChannelController.GetChannels();
        }

        /// <summary>
        /// Performs query on all FiOS channels
        /// </summary>
        /// <param name="FiosServiceId"></param>
        /// <param name="ChannelNumber"></param>
        /// <param name="CallSign"></param>
        /// <param name="StationName"></param>
        /// <param name="BitmapId"></param>
        /// <param name="RegionId"></param>
        /// <param name="TMSId"></param>
        /// <returns></returns>
        public IEnumerable<FiOSChannel> GetChannelQuery(string FiosServiceId, int? ChannelNumber, string CallSign, string StationName, int? BitmapId, string RegionId, string TMSId)
        {
            IEnumerable<FiOSChannel> channels = new List<FiOSChannel>(this._fiosChannels);
            

            var query = 
                from x in this._fiosChannels
                where 
                    x.FiosServiceId == (string.IsNullOrEmpty(FiosServiceId) ? x.FiosServiceId : FiosServiceId) &&
                    x.ChannelPosition == (ChannelNumber == null ? x.ChannelPosition : ChannelNumber) &&
                    string.IsNullOrEmpty(CallSign) ? x.CallSign == x.CallSign : SqlMethods.Like(x.CallSign, "%" + CallSign + "%") &&
                    string.IsNullOrEmpty(StationName) ? x.StationName == x.StationName : SqlMethods.Like(x.StationName, "%" + StationName + "%") &&
                    x.BitmapId == (BitmapId == null ? x.BitmapId : BitmapId) &&
                    x.RegionId == (string.IsNullOrEmpty(RegionId) ? x.RegionId : RegionId) &&
                    x.TMSId == (string.IsNullOrEmpty(TMSId) ? x.TMSId : TMSId)
                select x;

            return query;
        }

        public IEnumerable<FiOSChannel> GetChannelQuery(FiOSChannel ChannelToFind)
        {
            return this.GetChannelQuery(ChannelToFind.FiosServiceId, ChannelToFind.ChannelPosition, ChannelToFind.CallSign, ChannelToFind.StationName, ChannelToFind.BitmapId, ChannelToFind.RegionId, ChannelToFind.TMSId);
        }

        /// <summary>
        /// Gets all channels that have the default empty logo assigned.
        /// </summary>
        /// <returns>FiOS Channels</returns>
        public IEnumerable<FiOSChannel> GetChannelsWithoutLogo()
        {
            varCheck();
            return this._fiosChannels.Where(x => x.BitmapId == 10000);
        }

        /// <summary>
        /// Gets the bitmap id by channel number and region id
        /// </summary>
        /// <param name="ChannelNumber">Channel Position</param>
        /// <param name="RegionId">Region to which the channel belongs</param>
        /// <returns>Bitmap Id</returns>
        public int GetBitmapId(int ChannelNumber, string RegionId)
        {
            varCheck();
            return this._fiosChannels.SingleOrDefault(x => x.ChannelPosition.Value == ChannelNumber && x.RegionId == RegionId).BitmapId.Value;
        }

        /// <summary>
        /// Gets all assigned bitmap id's from the database
        /// </summary>
        /// <returns>Bitmap id's</returns>
        public IEnumerable<int> GetAssignedBitmapIds()
        {
            varCheck();
            return this._fiosChannels.Where(x => x.BitmapId.Value < 10000 && x.BitmapId.Value > 1).Select(x => x.BitmapId.Value);
        }

        /// <summary>
        /// Get bitmap id's from the database that do not correlate to a logo in the repository.
        /// </summary>
        /// <returns>Bitmap id's</returns>
        public IEnumerable<int> GetUnassignedBitmapIds()
        {
            varCheck();
            return GetAssignedBitmapIds().Except(getRepositoryBitmapIds());
        }

        /// <summary>
        /// Get the logo path to logos that are assigned to a channel in the guide
        /// </summary>
        /// <returns>Path to logo</returns>
        public IEnumerable<string> GetAssignedRepositoryLogos()
        {
            varCheck();
            return getRepositoryLogos().Intersect(getDatabaseAssignedLogoNames());
        }

        /// <summary>
        /// Get the logo path to logos that exist in the repository but not in the database.
        /// </summary>
        /// <returns>Path to logo</returns>
        public IEnumerable<string> GetUnassignedRepositoryLogos()
        {
            varCheck();
            return getRepositoryLogos().Except(getDatabaseAssignedLogoNames());
        }

        /// <summary>
        /// Gets the next available bitmap id that is not currently assigned.
        /// </summary>
        /// <returns>Bitmap Id</returns>
        public int GetNextAvailableId()
        {
            varCheck();
            var usedBMIds = this._fiosChannels.Select(x => x.BitmapId);
            int retVal = 0;
            for (int i = 2; i < 10000; i++)
            {
                string logoFileName = Path.Combine(this._logoRepository, i.ToString() + ".png");
                if (File.Exists(logoFileName) || usedBMIds.Contains(i))
                {
                    continue;
                }

                retVal = i;
                break;
            }

            if (retVal == 0)
                throw new Exception("No remaining bitmap id's available.");

            return retVal;
        }

        /// <summary>
        /// Checks if logo already exists in logo repository directory.
        /// </summary>
        /// <param name="FilePath">Full path to the file to check for duplicate.</param>
        /// <returns></returns>
        public bool isDuplicate(string FilePath)
        {
            using (Image img = resizeImage(FilePath))
            {
                return string.IsNullOrEmpty(getDuplicate(img));
            }
        }

        /// <summary>
        /// Gets all FiOS channels that are assigned to a duplicate image at the provided path.
        /// </summary>
        /// <param name="FilePath">Full path to the logo file.</param>
        /// <returns></returns>
        public IEnumerable<FiOSChannel> GetDuplicate(string FilePath)
        {
            using (Image img = resizeImage(FilePath))
            {
                return getFiosChannelFromFileName(getDuplicate(img));
            }
        }

        private IEnumerable<string> getRepositoryLogos()
        {
            varCheck();
            return Directory.EnumerateFiles(this._logoRepository, "*.png", SearchOption.TopDirectoryOnly);
        }

        private IEnumerable<int> getRepositoryBitmapIds()
        {
            foreach (var file in getRepositoryLogos())
            {
                var fInfo = new FileInfo(file);
                int bmId;

                string strBmId = fInfo.Name.Replace(fInfo.Extension, "");

                if (int.TryParse(strBmId, out bmId) && bmId < 10000 && bmId > 1)
                {
                    yield return bmId;
                }
            }
        }

        private IEnumerable<string> getDatabaseAssignedLogoNames()
        {
            return GetAssignedBitmapIds().Select(x => Path.Combine(this._logoRepository, x + ".png"));
        }

        private Image resizeImage(string FilePath)
        {
            using (var bm = new Bitmap(FilePath))
            using (var bm2 = new Bitmap((Image)bm, 100, 80))
            {
                if (bm.Width == 100 && bm.Height == 80)
                {
                    bm2.Dispose();
                    return bm;
                }
                return bm2;
            }
        }

        private byte[] imgToByte(Image img)
        {
            using (var ms = new MemoryStream())
            {
                img.Save(ms, img.RawFormat);
                return ms.ToArray();
            }
        }

        private string getDuplicate(Image sourceImg)
        {
            string dupImg = string.Empty;
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = this.CancelTokenSource.Token;
            po.MaxDegreeOfParallelism = 32;
            Parallel.ForEach(getRepositoryLogos(), po, (repoFile) =>
                {
                    po.CancellationToken.ThrowIfCancellationRequested();

                    if (!string.IsNullOrEmpty(dupImg))
                    {
                        return;
                    }

                    if (!(File.Exists(repoFile)))
                    {
                        return;
                    }


                    using (Image repoImg = resizeImage(repoFile))
                    {
                        var srcBits = imgToByte(sourceImg);
                        var destBits = imgToByte(repoImg);

                        if (srcBits == destBits)
                        {
                            dupImg = repoFile;
                        }
                    }

                });

            return dupImg;
        }

        private IEnumerable<FiOSChannel> getFiosChannelFromFileName(string FileName)
        {
            var fInfo = new FileInfo(FileName);

            int bmId;

            if (int.TryParse(fInfo.Name.Replace(fInfo.Extension, ""), out bmId))
            {
                return this._fiosChannels.Where(x => x.BitmapId == bmId);
            }

            throw new Exception(string.Format("Could not find FiOS channel assigned to {0}.", FileName));
        }

        private void varCheck()
        {
            if (this._fiosChannels == null || this._fiosChannels.Count() < 1)
            {
                throw new NullReferenceException("Fios Channels value is null.");
            }

            if (string.IsNullOrEmpty(this._logoRepository))
            {
                throw new NullReferenceException("Logo repository value is null.");
            }

            if (!(Directory.Exists(LogoRepository)))
            {
                throw new DirectoryNotFoundException(string.Format("Could not find directory at {0}.", LogoRepository));
            }
        }
    }
}
