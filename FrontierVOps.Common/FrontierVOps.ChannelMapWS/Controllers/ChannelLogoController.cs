using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;
using FrontierVOps.Common;
using FrontierVOps.Data;
using FrontierVOps.ChannelMapWS.Models;

namespace FrontierVOps.ChannelMapWS.Controllers
{
    [RoutePrefix("api/logo/channel")]
    public class ChannelLogoController : ApiController
    {
        private string _logoDir;

        public ChannelLogoController()
        {
#if DEBUG
            this._logoDir = WebConfigurationManager.AppSettings["LogoRepositoryDEV"];
#else
            this._logoDir = WebConfigurationManager.AppSettings["LogoRespositoryPROD"];
#endif
        }

        #region GET
        [HttpGet]
        [Route("bitmapId/{bitmapId:int:min(1):max(9999)}")]
        public ChannelLogo GetByBitmapId(int bitmapId)
        {
            ChannelLogo logo = new ChannelLogo();
            logo.LogoFile = logo.TryGetLogoFile(bitmapId);
            logo.ID = bitmapId;

            if (null != logo.LogoFile)
            {
                using (var bm = new Bitmap(logo.LogoFile.FullName))
                {
                    logo.Image = Toolset.ConvertToBytes(bm);
                }
                return logo;
            }
            return null;
        }

        [HttpGet]
        [Route("Image/Exists/String")]
        public bool GetBitmapExists([FromBody]string bitmap, bool searchArchive = false)
        {
            SearchOption srchOpt = searchArchive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var file in Directory.EnumerateFiles(this._logoDir, "*.png", srchOpt))
            {
                int logoId;
                FileInfo fInfo = new FileInfo(file);

                if (int.TryParse(fInfo.Name.Replace(".png", "").Trim(), out logoId))
                {
                    if (logoId > 0 && logoId < 10000)
                    {
                        using (var serverBM = new Bitmap(file))
                        {
                            if (Toolset.CompareBitmaps(bitmap, serverBM))
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        [HttpGet]
        [Route("Image/Exists/Bytes")]
        public bool GetBitmapExists([FromBody]byte[] bitmap, bool searchArchive = false)
        {
            SearchOption srchOpt = searchArchive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var file in Directory.EnumerateFiles(this._logoDir, "*.png", srchOpt))
            {
                int logoId;
                FileInfo fInfo = new FileInfo(file);

                if (int.TryParse(fInfo.Name.Replace(".png", "").Trim(), out logoId))
                {
                    if (logoId > 0 && logoId < 10000)
                    {
                        if (logoId == 4019)
                            Console.Write(logoId);
                        using (var serverBM = new Bitmap(file))
                        {
                            var svrBytes = Toolset.ConvertToBytes(serverBM);
                            if (Toolset.CompareBitmaps(bitmap, svrBytes))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        [HttpGet]
        [Route("Image/Find/String")]
        public IEnumerable<ChannelLogo> FindChannelLogo([FromBody]string bitmap, bool searchArchive = false)
        {
            SearchOption srchOpt = searchArchive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var file in Directory.EnumerateFiles(this._logoDir, "*.png", srchOpt))
            {
                FileInfo fInfo = new FileInfo(file);
                int id;

                if (int.TryParse(fInfo.Name.Replace(".png", "").Trim(), out id))
                {
                    if (id > 0 && id < 10000)
                    {
                        using (var serverBM = new Bitmap(file))
                        {
                            if (Toolset.CompareBitmaps(bitmap, serverBM))
                            {
                                var chLogo = new ChannelLogo();
                                chLogo.LogoFile = new FileInfo(file);
                                int idVal;
                                int.TryParse(chLogo.LogoFile.Name.Replace(".png", "").Trim(), out idVal);
                                chLogo.ID = idVal;
                                chLogo.Image = Toolset.ConvertToBytes(serverBM);
                                if (chLogo.ID.HasValue)
                                    chLogo.IsAssigned = IsAssigned(chLogo.ID.Value);

                                yield return chLogo;
                            }
                        }
                    }
                }
            }
        }

        [HttpGet]
        [Route("Image/Find/Bytes")]
        public IEnumerable<ChannelLogo> FindChannelLogo([FromBody]byte[] bitmap, bool searchArchive = false)
        {
            SearchOption srchOpt = searchArchive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var file in Directory.EnumerateFiles(this._logoDir, "*.png", srchOpt))
            {
                FileInfo fInfo = new FileInfo(file);
                int id;

                if (int.TryParse(fInfo.Name.Replace(".png", ""), out id))
                {
                    if (id > 0 && id < 10000)
                    {
                        using (var serverBM = new Bitmap(file))
                        {
                            if (Toolset.CompareBitmaps(bitmap, Toolset.ConvertToBytes(serverBM)))
                            {
                                var chLogo = new ChannelLogo();
                                chLogo.LogoFile = new FileInfo(file);
                                int idVal;
                                int.TryParse(chLogo.LogoFile.Name.Replace(".png", "").Trim(), out idVal);
                                chLogo.ID = idVal;
                                chLogo.Image = Toolset.ConvertToBytes(serverBM);
                                if (chLogo.ID.HasValue)
                                    chLogo.IsAssigned = IsAssigned(chLogo.ID.Value);

                                yield return chLogo;
                            }
                        }
                    }
                }
            }
        }

        [HttpGet]
        [Route("IsAssigned/{bitmapId:int:range(0,10000)}")]
        public bool IsAssigned(int bitmapId, string version = "1.9")
        {
            string command = "SELECT TOP 1 * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version + "' AND intBitmapId = " + bitmapId;

            if (DBFactory.SQL_ExecuteReader(WebApiConfig.ConnectionString, command, System.Data.CommandType.Text).Count() > 0)
                return true;

            return false;
        }

        [HttpGet]
        [Route("Exists/{bitmapId:int:range(0,10000)}")]
        public bool IdExists(int bitmapId, bool searchArchive = false)
        {
            var srchOpt = searchArchive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach(var file in Directory.EnumerateFiles(this._logoDir, "*.png", srchOpt))
            {
                var fileInfo = new FileInfo(file);
                int fileId;

                if (int.TryParse(fileInfo.Name.Replace(".png","").Trim(), out fileId))
                {
                    if (fileId < 0 || fileId > 10000)
                        continue;

                    if (fileId == bitmapId)
                        return true;
                }
            }
            return false;
        }

        [HttpGet]
        [Route("NextAvailableId")]
        public int GetNextAvailableId()
        {
            for (int i = 1; i < 10000; i++ )
            {
                string fileName = i + ".png";
                string filePath = Path.Combine(this._logoDir, fileName);
                string archivePath = Path.Combine(this._logoDir, "Archive", fileName);

                if (!(File.Exists(filePath) || File.Exists(archivePath)))
                    return i;
            }

            throw new Exception("No available ID's left, please run cleanup job");
        }
        #endregion GET

        #region PUT

        #endregion PUT

        #region POST
        [HttpPost]
        [Authorize(Roles="VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [Route("Image/New")]
        public ChannelLogo AddNewLogo([FromBody]ChannelLogo chLogo)
        {
            if (!chLogo.ID.HasValue)
                chLogo.ID = GetNextAvailableId();

            if (chLogo.ID < 0 || chLogo.ID > 10000)
                throw new Exception("Channel Logo ID value is outside acceptable range");

            if (IdExists(chLogo.ID.Value, true))
                throw new Exception("A channel logo with this ID already exists on the server");

            string fileName = chLogo.ID.Value + ".png";
            string filePath = Path.Combine(this._logoDir, fileName);


            using (Bitmap tmpBm = Toolset.ConvertToBitmap(chLogo.Image))
            using (Bitmap bmLogo = ValidateBitmap(tmpBm) ? tmpBm : Toolset.ResizeBitmap(tmpBm, 100, 80, null, null))
            {
                //temp bitmap is no longer needed, so dispose early to free resources
                //tmpBm.Dispose();

                bool logoExists = GetBitmapExists(chLogo.Image, true);

                if (logoExists)
                {
                    var dupLogo = FindChannelLogo(chLogo.Image, true).FirstOrDefault();

                    if (dupLogo != null)
                    {
                        throw new Exception("Duplicate logo image found at " + dupLogo.LogoFile.FullName + ". Update or delete existing image.");
                    }
                    else
                    {
                        //if the bitmap says it exists but is unable to locate it, then save it and let duplication cleanup handle it
                        logoExists = false;
                    }
                }
                
                //Not using else statement in case logoexists value changed in logic above
                if (!logoExists)
                {
                    try
                    {
                        bmLogo.Save(filePath, ImageFormat.Png);
                    }
                    catch (Exception ex)
                    {
                        throw new IOException("Failed to save channel logo image. " + ex.Message, ex);
                    }
                }
            }

            if (!File.Exists(filePath))
                throw new Exception("Failed to create new channel logo. Logo file was not saved.");

            return GetByBitmapId(chLogo.ID.Value);
        }
        #endregion POST

        #region PrivateMethods
        private bool ValidateBitmap(Bitmap bm)
        {
            return (bm.Width.Equals(100) && bm.Height.Equals(80)) && bm.RawFormat.Equals(ImageFormat.Png);
        }
        #endregion PrivateMethods
    }
}
