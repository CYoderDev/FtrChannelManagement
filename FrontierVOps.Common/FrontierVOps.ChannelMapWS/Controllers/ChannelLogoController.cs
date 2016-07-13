using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using FrontierVOps.Common;
using FrontierVOps.Common.Web.ActionResults;
using FrontierVOps.Data;
using FrontierVOps.ChannelMapWS.Filters;
using FrontierVOps.ChannelMapWS.Models;

namespace FrontierVOps.ChannelMapWS.Controllers
{
    [RoutePrefix("api/logo/channel")]
    public class ChannelLogoController : ApiController
    {
        private string _logoRepository;
        private string _activeLogoDir;
        private string _archiveLogoDir;
        private string _stagingDir;

        public ChannelLogoController()
        {
            this._logoRepository = HttpContext.Current.Server.MapPath("~/LogoRepository");

            this._activeLogoDir = Path.Combine(_logoRepository, "Active");
            this._archiveLogoDir = Path.Combine(_logoRepository, "Archive");
            this._stagingDir = Path.Combine(_logoRepository, "Staging");
        }

        #region GET
        [HttpGet]
        [Route("bitmapId/{bitmapId:int:min(1):max(9999)}")]
        public ChannelLogoInfo GetByBitmapId(int bitmapId, bool active = true)
        {
            ChannelLogoInfo logo = null;

            string srchDir = active ? this._activeLogoDir : this._archiveLogoDir;
            string filePath = Path.Combine(srchDir, string.Format("{0}.png", bitmapId));

            if (File.Exists(filePath))
            {
                logo = new ChannelLogoInfo();

                logo.FileName = filePath;
                logo.BitmapId = bitmapId;
                logo.IsAssigned = IsAssigned(bitmapId);
            }

            return logo;
        }

        [HttpGet]
        [Route("Image/Exists")]
        public bool GetBitmapExists([FromBody]Bitmap bitmap, bool searchArchive = false)
        {
            string srchDir = this._activeLogoDir;

            if (searchArchive)
                srchDir = this._archiveLogoDir;

            foreach (var file in Directory.EnumerateFiles(srchDir, "*.png", SearchOption.TopDirectoryOnly))
            {
                int logoId;
                FileInfo fInfo = new FileInfo(file);

                if (int.TryParse(fInfo.Name.Replace(".png", "").Trim(), out logoId))
                {
                    if (logoId > 0 && logoId < 10000)
                    {
                        if (logoId == 3)
                            Console.Write(logoId);
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
        [Route("Image/Find")]
        public async Task<IHttpActionResult> FindChannelLogo(bool searchArchive = false)
        {
            string srchDir = this._activeLogoDir;

            if (searchArchive)
                srchDir = this._archiveLogoDir;

            var readTask = await Request.Content.ReadAsStreamAsync();

            var chLogos = new List<ChannelLogoInfo>();

            foreach (var file in Directory.EnumerateFiles(srchDir, "*.png", SearchOption.TopDirectoryOnly))
            {
                FileInfo fInfo = new FileInfo(file);
                
                int id;

                if (int.TryParse(fInfo.Name.Replace(".png", "").Trim(), out id))
                {
                    if (id > 0 && id < 10000)
                    {
                        using (var serverBM = new Bitmap(file))
                        using (var clientBM = new Bitmap(readTask))
                        {
                            if (Toolset.CompareBitmaps(clientBM, serverBM))
                            {
                                var chLogo = new ChannelLogoInfo();
                                chLogo.FileName = fInfo.Name;
                                chLogo.BitmapId = id;
                                chLogo.IsAssigned = IsAssigned(id);

                                chLogos.Add(chLogo);
                            }
                        }
                    }
                }
            }
            if (chLogos.Count > 0)
            {
                return Ok(chLogos);
            }

            return NotFound();
        }

        [HttpGet]
        [Route("Image/GetBitmap/{bitmapId:int:range(0,10000)}")]
        public IHttpActionResult GetImage(int bitmapId, bool archive = false)
        {
            var fileName = string.Format("{0}.png", bitmapId);
            var path = archive ? Path.Combine(this._archiveLogoDir, fileName) : Path.Combine(this._activeLogoDir, fileName);

            path = Path.GetFullPath(path);

            if (!path.StartsWith(this._logoRepository))
                throw new HttpException(403, "Forbidden");

            //var result = new HttpResponseMessage(HttpStatusCode.OK);

            //var stream = new FileStream(path, FileMode.Open);
            
            //result.Content = new StreamContent(stream);
            //result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            return new FileResult(path, "image/png");
        }

        [HttpGet]
        [Route("IsAssigned/{bitmapId:int:range(0,10000)}")]
        public bool IsAssigned(int bitmapId, string version = "1.9")
        {
            string command = "SELECT TOP 1 * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version + "' AND intBitmapId = " + bitmapId;

            return DBFactory.SQL_ExecuteReader(WebApiConfig.ConnectionString, command, System.Data.CommandType.Text).Count() > 0;
        }

        [HttpGet]
        [Route("Exists/{bitmapId:int:range(0,10000)}")]
        public bool IdExists(int bitmapId, bool searchArchive = false)
        {
            string srchDir = this._activeLogoDir;

            if (searchArchive)
                srchDir = this._archiveLogoDir;

            foreach(var file in Directory.EnumerateFiles(srchDir, "*.png", SearchOption.TopDirectoryOnly))
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
        public IHttpActionResult GetNextAvailableId(bool archive = false)
        {
            string srchDir = this._activeLogoDir;
            HttpResponseMessage responseMsg = new HttpResponseMessage(HttpStatusCode.OK);

            if (archive)
                srchDir = this._archiveLogoDir;

            for (int i = 1; i < 10000; i++ )
            {
                string fileName = i + ".png";
                string filePath = Path.Combine(srchDir, fileName);

                if (!(File.Exists(filePath)))
                    return Ok(i);
            }
            
            HttpError err = new HttpError("No available id's left.");
            return Content(HttpStatusCode.NotFound, err);
        }
        #endregion GET

        #region PUT      
        
        #endregion PUT

        #region POST
        [MimeMultipart]
        [HttpPost]
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [Route("Image/Upload")]
        public async Task<ChannelLogoInfo> Upload()
        {
            var uploadPath = HttpContext.Current.Server.MapPath("~/LogoRepository/Active");
            int bmId;
            GetNextAvailableId().ExecuteAsync(CancellationToken.None).Result.TryGetContentValue(out bmId);

            var streamProvider = new MultipartFormDataStreamProvider(uploadPath);

            // Read the MIME multipart asynchronously 
            await Request.Content.ReadAsMultipartAsync(streamProvider);

            var fileName = streamProvider.FileData.Select(entry => entry.LocalFileName).First();
            var newFileName = fileName.Substring(0,fileName.LastIndexOf("\\") + 1) + bmId + ".png";

            try
            {
                using (var bm = Toolset.ResizeBitmap(fileName, 100, 80, null, null, true))
                {

                    if (!ValidateBitmap(bmId) && File.Exists(newFileName))
                    {
                        throw new Exception("Image reformat failed.");
                    }

                    if (GetBitmapExists(bm, false))
                    {
                        throw new Exception("Image already exists in active repository.");
                    }
                    
                    bm.Save(newFileName);

                    if (!File.Exists(newFileName))
                        throw new Exception("Image file failed to save.");
                }
            }
            catch (Exception ex)
            {
                if (File.Exists(newFileName))
                    File.Delete(newFileName);

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message, ex));
            }
            finally
            {
                if (File.Exists(fileName))
                    File.Delete(fileName);
            }

            // Create response
            return new ChannelLogoInfo()
            {
                FileName = newFileName.Substring(newFileName.LastIndexOf("\\") + 1),
                BitmapId = bmId,
                IsAssigned = IsAssigned(bmId),
            };
        }
        #endregion POST

        #region PrivateMethods
        private bool ValidateBitmap(Bitmap bm)
        {
            return (bm.Width.Equals(100) && bm.Height.Equals(80)) && bm.RawFormat.Equals(ImageFormat.Png);
        }

        private bool ValidateBitmap(int id)
        {
            string fileName = string.Join(".", id, "png");
            string filePath = Path.Combine(HttpContext.Current.Server.MapPath("~/LogoRepository/Staging"), fileName);
            
            return File.Exists(filePath) && ValidateBitmap(new Bitmap(filePath));
        }

        private void archiveFile(FileInfo file, bool deleteOld)
        {
            string archivePath = Path.Combine(this._activeLogoDir, "Archive");
            string archiveFile = Path.Combine(archivePath, file.Name);

            if (File.Exists(archiveFile) && !deleteOld)
            {
                File.Copy(archiveFile, archiveFile + ".old", true);
                File.SetAttributes(archiveFile + ".old", FileAttributes.Temporary);
            }

            file.CopyTo(archivePath, true);

            if (deleteOld)
                file.Delete();
        }

        private void restoreArchive(int id)
        {
            string archivePath = Path.Combine(this._activeLogoDir, "Archive");
            string archiveFile = Path.Combine(archivePath, id + ".png");
            string oldFile = archiveFile + ".old";

            if (File.Exists(archiveFile))
            {
                File.Copy(archiveFile, Path.Combine(this._activeLogoDir, id + ".png"), true);
            }

            if (File.Exists(oldFile))
            {
                File.Copy(oldFile, archiveFile, true);
                deleteOld(oldFile);
            }
        }

        private void deleteOld(string oldFilePath)
        {
            string archiveFile = Path.Combine(this._activeLogoDir, "Archive", oldFilePath.Substring(oldFilePath.LastIndexOf("\\") + 1));

            if (File.Exists(oldFilePath))
            {
                File.Delete(oldFilePath);
            }

            if (File.Exists(archiveFile))
            {
                File.Delete(archiveFile);
            }
        }
        #endregion PrivateMethods
    }
}
