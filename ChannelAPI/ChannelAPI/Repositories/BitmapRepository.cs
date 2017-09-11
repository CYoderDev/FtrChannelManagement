using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
        private string _logoFormat;
        private string _version;
        private ILogger _logger;

        public BitmapRepository(IConfiguration config, ILoggerFactory loggerFactory)
        {
#if DEBUG
            this._bitmapDirectory = config.GetValue<string>("FiosChannelData:LogoRepository_DEBUG");
#else
            this._bitmapDirectory = config.GetValue<string>("FiosChannelData:LogoRepository");
#endif
            this._logoHeight = config.GetValue<int>("FiosChannelData:LogoHeight");
            this._logoWidth = config.GetValue<int>("FiosChannelData:LogoWidth");
            this._version = config.GetValue<string>("FiosChannelData:VersionAliasId");
            this._logoFormat = config.GetValue<string>("FiosChannelData:LogoFormat");
            if (!_logoFormat.StartsWith("."))
                this._logoFormat = "." + this._logoFormat;

            this._logger = loggerFactory.CreateLogger<BitmapRepository>();
        }

        public Stream GetBitmapById(string id)
        {
            _logger.LogDebug("Getting bitmap by id {0}", id);
            string bitmapFileName = id + this._logoFormat;

            string bitmapFullPath = Path.Combine(this._bitmapDirectory, bitmapFileName);

            if (!File.Exists(bitmapFullPath))
                return null;

            return File.OpenRead(bitmapFullPath);
        }

        public async Task<IEnumerable<BitmapStationMapDTO>> GetStationsByBitmapId(int bitmapId)
        {
            string query = "SELECT * FROM tFiosBitmapStationMap WHERE intBitMapId = @id AND strFIOSVersionAliasId = @version";

            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            {
                return await connection.QueryAsync<BitmapStationMapDTO>(query, new { id = bitmapId, version = this._version });
            }
        }

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
                }
            });

            if (tskException != null)
                throw tskException;
        }

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

        public async Task<int> UpdateChannelBitmap(int newBitmapId)
        {
            using (var stream = this.GetBitmapById(newBitmapId.ToString()))
            {
                return await UpdateChannelBitmap(newBitmapId, Image.FromStream(stream));
            }
        }

        public async Task<int> UpdateChannelBitmap(int newBitmapId, Image logo)
        {
            int retVal = 0;

            using (var connection = await DapperFactory.GetOpenConnectionAsync())
            using (var trans = connection.BeginTransaction())
            {
                try
                {
                    retVal += await updateBitmapDTO(connection, newBitmapId, logo);
                    retVal += await updateBitmapVersionDTO(connection, newBitmapId, logo);
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

        public void DeleteBitmap(string id)
        {
            var fileName = Path.Combine(this._bitmapDirectory, (id + this._logoFormat));
            if (!File.Exists(fileName))
                return;

            if (moveFileToArchive(fileName))
                File.Delete(fileName);
        }

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

        private async Task<int> updateBitmapDTO(IDbConnection connection, int bitmapId, Image logo)
        {
            var bmDTO = await connection.GetAsync<FiosBitmap>(bitmapId);
            if (bmDTO.intBitmapId == 0)
            {
                bmDTO = new FiosBitmap();
                bmDTO.intBitmapId = bitmapId;
                bmDTO.strBitMapFileName = Path.Combine(this._bitmapDirectory, bitmapId + this._logoFormat);
                bmDTO.strBitMapName = bitmapId + this._logoFormat;
                bmDTO.strBitMapMD5Digest = getMD5(logo);
                bmDTO.dtCreateDate = DateTime.Now;
                bmDTO.dtLastUpdateDate = DateTime.Now;
                return await connection.InsertAsync<FiosBitmap>(bmDTO);
            }
            else
            {
                bmDTO.intBitmapId = bitmapId;
                bmDTO.strBitMapMD5Digest = getMD5(logo);
                bmDTO.dtCreateDate = DateTime.Now;
                bmDTO.dtLastUpdateDate = DateTime.Now;
                if (await connection.UpdateAsync<FiosBitmap>(bmDTO)) { return 1; }
            }
            return 0;
        }

        private async Task<int> updateBitmapVersionDTO(IDbConnection connection, int bitmapId, Image logo)
        {
            string query = "SELECT * FROM tFIOSBitmapVersion a WHERE a.intBitMapId = @bmId AND a.strFIOSVersionAliasId = @version";
            var bmDTO = await connection.QueryFirstOrDefaultAsync<BitmapVersionDTO>(query, new { bmId = bitmapId, version = this._version });

            if (bmDTO.intBitmapId == 0)
            {
                bmDTO = new BitmapVersionDTO();
                bmDTO.intBitmapId = bitmapId;
                bmDTO.strBitmapMD5Digest = getMD5(logo);
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
                return await connection.ExecuteAsync(query, new { newDate = DateTime.Now, bmId = bitmapId });
            }
        }

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

        private void resizeImage(ref Image originalImg)
        {
            Size size = new Size(this._logoWidth, this._logoHeight);
            originalImg = new Bitmap(originalImg, size);
        }

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
