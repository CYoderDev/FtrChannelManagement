using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using Dapper.Contrib.Extensions;
using Dapper.Mapper;
using ChannelAPI.Models;

namespace ChannelAPI.Repositories
{
    public class BitmapRepository
    {
        private string _bitmapDirectory;
        private int _logoHeight;
        private int _logoWidth;
        internal int _maxBitmapId = 10000;
        private string _logoFormat;
        private string _version;
        private ILogger _logger;

        public BitmapRepository(IConfiguration config, ILoggerFactory loggerFactory, IHostingEnvironment hostingEnvironment)
        {
            //Get variables from the configuration file
            this._bitmapDirectory = Path.Combine(hostingEnvironment.WebRootPath, "ChannelLogoRepository");
            this._logoHeight = config.GetValue<int>("FiosChannelData:LogoHeight");
            this._logoWidth = config.GetValue<int>("FiosChannelData:LogoWidth");
            this._version = config.GetValue<string>("FiosChannelData:VersionAliasId");
            this._logoFormat = config.GetValue<string>("FiosChannelData:LogoFormat");
            if (!_logoFormat.StartsWith("."))
                this._logoFormat = "." + this._logoFormat;

            this._logger = loggerFactory.CreateLogger<BitmapRepository>();
        }

        /// <summary>
        /// Gets the imagefile by the service ID
        /// </summary>
        /// <param name="id">Bitmap ID</param>
        /// <returns>The image file as an IO stream</returns>
        public Stream GetBitmapById(string id)
        {
            _logger.LogDebug("Getting bitmap by id {0}", id);
            string bitmapFileName = id + this._logoFormat;

            string bitmapFullPath = Path.Combine(this._bitmapDirectory, bitmapFileName);

            if (!File.Exists(bitmapFullPath))
                return null;

            return File.OpenRead(bitmapFullPath);
        }

        /// <summary>
        /// Returns all stations that are mapped to the Bitmap ID
        /// </summary>
        /// <param name="bitmapId">Bitmap ID</param>
        /// <returns>Fios stations mapped to the provided Bitmap ID</returns>
        public async Task<IEnumerable<dynamic>> GetStationsByBitmapId(int bitmapId)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("SELECT * FROM vChannels");
            query.AppendLine("WHERE intBitMapId = @id AND strFIOSVersionAliasId = @version");

            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                var result = await connection.QueryAsync<dynamic>(query.ToString(), new { id = bitmapId, version = this._version });
                return result.Select(x => new {
                    x.strFIOSServiceId,
                    x.strStationName,
                    x.strStationCallSign,
                    x.strVHOId,
                    x.strFIOSRegionName,
                    x.intChannelPosition
                }).Distinct();
            }
        }

        /// <summary>
        /// Creates the image file from the provided image in the repository.
        /// </summary>
        /// <param name="logo">Image that represents the logo</param>
        /// <param name="id">The bitmap ID to assign to the image</param>
        /// <returns></returns>
        public async Task InsertBitmap(Image logo, string id)
        {
            if (logo.Height != this._logoHeight || logo.Width != this._logoWidth)
                resizeImage(ref logo);

            Exception tskException = null;

            var imgPath = Path.Combine(this._bitmapDirectory, (id + this._logoFormat));

            if (File.Exists(imgPath))
                throw new ArgumentException(string.Format("File at {0} already exists. Use PUT method to update existing bitmap id.", imgPath));

            await Task.Factory.StartNew(() => 
            {
                try
                {
                    logo.Save(Path.Combine(this._bitmapDirectory, (id + this._logoFormat)), getImageFormat());
                }
                catch (Exception ex)
                {
                    tskException = ex;
                    this._logger.LogError("Failed to save image file. {0} - {1}", ex.Message, ex.StackTrace);
                }
            });

            if (tskException != null)
                throw tskException;
        }

        /// <summary>
        /// Updates an existing image file, archiving the old one.
        /// </summary>
        /// <param name="logo">Image that will overwite the existing one</param>
        /// <param name="id">Bitmap ID of the image</param>
        /// <returns></returns>
        public async Task UpdateBitmap(Image logo, string id)
        {
            if (logo.Height != this._logoHeight || logo.Width != this._logoWidth)
                resizeImage(ref logo);

            var logoPath = Path.Combine(this._bitmapDirectory, (id + this._logoFormat));

            if (await Task.Factory.StartNew<bool>(() => { return moveFileToArchive(logoPath); }))
            {
                logo.Save(logoPath, getImageFormat());
            }
            else
            {
                throw new IOException("Failed to backup image file to archive.");
            }
        }

        /// <summary>
        /// Updates the station's assigned bitmap ID
        /// </summary>
        /// <param name="newBitmapId">The new bitmap ID to assign to the station</param>
        /// <returns></returns>
        public async Task<int> UpdateChannelBitmap(int newBitmapId)
        {
            //Get the image stream from the existing bitmap
            using (var stream = this.GetBitmapById(newBitmapId.ToString()))
            {
                return await UpdateChannelBitmap(newBitmapId, Image.FromStream(stream));
            }
        }

        /// <summary>
        /// Updates the station's assigned bitmap ID and version
        /// </summary>
        /// <param name="newBitmapId"></param>
        /// <param name="logo"></param>
        /// <returns></returns>
        public async Task<int> UpdateChannelBitmap(int newBitmapId, Image logo)
        {
            int retVal = 0;

            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            using (var trans = connection.BeginTransaction())
            {
                try
                {
                    retVal += await updateBitmapDTO(connection, trans, newBitmapId, logo);
                    retVal += await updateBitmapVersionDTO(connection, trans, newBitmapId, logo);
                }
                catch(Exception ex)
                {
                    trans.Rollback();
                    throw ex;
                }
                trans.Commit();
            }

            return retVal;
        }

        /// <summary>
        /// Delete the image file from the repository.
        /// </summary>
        /// <param name="id"></param>
        public void DeleteBitmap(string id)
        {
            var fileName = Path.Combine(this._bitmapDirectory, (id + this._logoFormat));
            if (!File.Exists(fileName))
                return;

            if (moveFileToArchive(fileName))
                File.Delete(fileName);
        }

        /// <summary>
        /// Get all BitmapID's that currently exist in the repository directory.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<int> GetAllRepositoryIds()
        {
            return Directory.EnumerateFiles(this._bitmapDirectory, "*" + _logoFormat, SearchOption.TopDirectoryOnly).Select(x =>
            {
                int iVal;
                var fileName = Path.GetFileNameWithoutExtension(x);
                if (int.TryParse(fileName, out iVal))
                    return iVal;
                return 0;
            }).Where(x => x > 0).Distinct();
        }

        /// <summary>
        /// Returns any duplicate images if any exist.
        /// </summary>
        /// <param name="originalImg">The original image to compare against.</param>
        /// <returns>The Bitmap Id's of any duplicate images, or null if none are returned</returns>
        public int? GetDuplicates(Image originalImg)
        {
            int? matchedId = null;

            //Make sure the provided image dimensions match the standard height/width of the logo
            //for comparison reasons.
            if (originalImg.Height != this._logoHeight ||
                originalImg.Width != this._logoWidth)
            {
                resizeImage(ref originalImg);
            }


            using (var ms = new MemoryStream())
            {
                originalImg.Save(ms, this.getImageFormat());
                var imgBytes = ms.ToArray();

                //Spend some extra CPU resources to increase the speed of the comparison between each file
                //by having the iteration run in parallel. A max of 20 threads will be used, if available on the host.
                ParallelOptions po = new ParallelOptions() { MaxDegreeOfParallelism = 20 };
                Parallel.ForEach(Directory.EnumerateFiles(this._bitmapDirectory), (file) =>
                {
                    if (matchedId.HasValue)
                        return;
                    using (Image img = new Bitmap(file))
                    {
                        using (var msSrc = new MemoryStream())
                        {
                            img.Save(msSrc, img.RawFormat);
                            var srcBytes = msSrc.ToArray();

                            //Skip comparison if the number of bytes between both images are not the same
                            if (imgBytes.Count() != srcBytes.Count())
                                return;

                            //If bytes are equal, or the number of differences are less than 10
                            if (imgBytes.SequenceEqual(srcBytes) ||
                                imgBytes.Where((x, i) => x != srcBytes[i]).Count() < 10)
                            {
                                var idToAdd = filePathToBitmapId(file);
                                if (idToAdd.HasValue)
                                    matchedId = idToAdd.Value;
                            }
                        }
                    }
                });
            }

            return matchedId;
        }

        /// <summary>
        /// Gets the next unused Bitmap Id from the repository.
        /// </summary>
        /// <returns></returns>
        public int GetNextAvailableId()
        {
            for (int i = 1; i < this._maxBitmapId; i++)
            {
                string file = Path.Combine(this._bitmapDirectory, i + this._logoFormat);

                if (File.Exists(file))
                    continue;
                else
                    return i;
            }

            _logger.LogError("No remaining bitmap ids available in the server directory. Please extend range.");
            throw new ApplicationException("No available bitmap ids. Please contact administrator.");
        }

        /// <summary>
        /// Converts a file path to a Bitmap ID.
        /// </summary>
        /// <param name="path">Full path to the image file.</param>
        /// <returns></returns>
        private int? filePathToBitmapId(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            int bmId;

            if (int.TryParse(fileName, out bmId))
            {
                return bmId;
            }
            else
            {
                this._logger.LogWarning("Could not map file at {0} to a bitmapid.", path);
                return null;
            }
        }

        /// <summary>
        /// Updates the Bitmap details in the database to prompt the STB to update the
        /// cache and download the newest image.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="bitmapId"></param>
        /// <param name="logo"></param>
        /// <returns></returns>
        private async Task<int> updateBitmapDTO(IDbConnection connection, IDbTransaction transaction, int bitmapId, Image logo)
        {
            //Get the bitmap details from the database, if they do not exist then it will return a bitmap ID of 0
            var bmDTO = await connection.GetAsync<FiosBitmap>(bitmapId, transaction: transaction);

            if (bmDTO.intBitmapId == 0)
            {
                bmDTO = new FiosBitmap();
                bmDTO.intBitmapId = bitmapId;
                bmDTO.strBitMapFileName = Path.Combine(this._bitmapDirectory, bitmapId + this._logoFormat);
                bmDTO.strBitMapName = bitmapId + this._logoFormat;
                bmDTO.strBitMapMD5Digest = logo == null ? bmDTO.strBitMapMD5Digest : getMD5(logo);
                bmDTO.dtCreateDate = DateTime.Now;
                bmDTO.dtLastUpdateDate = DateTime.Now;
                return await connection.InsertAsync<FiosBitmap>(bmDTO, transaction: transaction);
            }
            else
            {
                bmDTO.intBitmapId = bitmapId;
                bmDTO.strBitMapMD5Digest = getMD5(logo);
                bmDTO.dtCreateDate = DateTime.Now;
                bmDTO.dtLastUpdateDate = DateTime.Now;
                if (await connection.UpdateAsync<FiosBitmap>(bmDTO, transaction)) { return 1; }
            }
                
            return 0;
        }

        /// <summary>
        /// Updates the Bitmap Version table with the new bitmap details.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="bitmapId"></param>
        /// <param name="logo"></param>
        /// <returns></returns>
        private async Task<int> updateBitmapVersionDTO(IDbConnection connection, IDbTransaction transaction, int bitmapId, Image logo)
        {
            string query = "SELECT * FROM tFIOSBitmapVersion a WHERE a.intBitMapId = @bmId AND a.strFIOSVersionAliasId = @version";
            var bmDTO = await connection.QueryFirstOrDefaultAsync<BitmapVersionDTO>(query, param: new { bmId = bitmapId, version = this._version }, transaction: transaction);

            if (bmDTO.intBitmapId == 0)
            {
                bmDTO = new BitmapVersionDTO();
                bmDTO.intBitmapId = bitmapId;
                bmDTO.strBitmapMD5Digest = logo == null ? bmDTO.strBitmapMD5Digest : getMD5(logo);
                bmDTO.strFIOSVersionAliasId = _version;
                bmDTO.dtLastUpdateDate = DateTime.Now;
                bmDTO.dtCreateDate = DateTime.Now;
                bmDTO.dtImageLastUpdateTimestamp = DateTime.Now;
                bmDTO.strBitMapIsfOffline_YN = "N";
                bmDTO.strBitMapIsfOffline_YN_HD = "N";
                return await connection.InsertAsync<BitmapVersionDTO>(bmDTO);
            }
            else
            {
                //Update timestamp on all versions, not just the provided version
                query = "UPDATE tFIOSBitmapVersion SET dtCreateDate = @newDate, dtLastUpdateDate = @newDate WHERE intBitMapId = @bmId";
                return await connection.ExecuteAsync(query, new { newDate = DateTime.Now, bmId = bitmapId }, transaction: transaction);
            }
        }

        /// <summary>
        /// Generate an MD5 hash for the provided image.
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        private string getMD5(Image img)
        {
            byte[] bytImg = null;
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, getImageFormat());
                bytImg = ms.ToArray();
            }

            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(bytImg);
                return Encoding.ASCII.GetString(result);
            }
        }

        /// <summary>
        /// Moves an existing image file to the archive directory.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool moveFileToArchive(string filePath)
        {
            if (!File.Exists(filePath))
                return true;
            string archiveDirectory = Path.Combine(this._bitmapDirectory, "Archive");
            if (!Directory.Exists(archiveDirectory))
                Directory.CreateDirectory(archiveDirectory);

            string fileName = Path.GetFileName(filePath);
            string newFilePath = Path.Combine(archiveDirectory, fileName);

            File.Copy(filePath, newFilePath, true);

            try
            {
                File.Delete(filePath);
            }
            catch
            {
                restoreFileFromArchive(fileName);
                return false;
            }
            return File.Exists(newFilePath) && !File.Exists(fileName);
        }

        /// <summary>
        /// Restores an image file from the archive directory.
        /// </summary>
        /// <param name="fileName"></param>
        private void restoreFileFromArchive(string fileName)
        {
            string archiveDirectory = Path.Combine(this._bitmapDirectory, "Archive");
            if (!Directory.Exists(archiveDirectory))
                throw new DirectoryNotFoundException(string.Format("Could not find archive directory at {0}.", archiveDirectory));

            string repoFile = Path.Combine(this._bitmapDirectory, fileName);
            string archiveFile = Path.Combine(archiveDirectory, fileName);

            if (!File.Exists(archiveFile))
                throw new FileNotFoundException("Could not find logo in archive.", archiveFile);

            File.Copy(archiveFile, repoFile, true);
            File.Delete(archiveFile);
        }

        /// <summary>
        /// Resize an image to match the standard logo specifications.
        /// </summary>
        /// <param name="originalImg"></param>
        private void resizeImage(ref Image originalImg)
        {
            Size size = new Size(this._logoWidth, this._logoHeight);
            originalImg = new Bitmap(originalImg, size);
        }

        /// <summary>
        /// Get the image file format.
        /// </summary>
        /// <returns></returns>
        private ImageFormat getImageFormat()
        {
            switch (this._logoFormat.ToLower())
            {
                case ".png":
                    return ImageFormat.Png;
                case ".jpg":
                case ".jpeg":
                    return ImageFormat.Jpeg;
                case ".bmp":
                    return ImageFormat.Bmp;
                case ".gif":
                    return ImageFormat.Gif;
                case ".ico":
                    return ImageFormat.Icon;
                case ".tiff":
                    return ImageFormat.Tiff;
                case ".exif":
                    return ImageFormat.Exif;
                default:
                    return ImageFormat.Png;
            }
        }
    }
}
