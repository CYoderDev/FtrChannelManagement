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
            this._archiveLogoDir = Path.Combine(_logoRepository, "Archived");
            this._stagingDir = Path.Combine(_logoRepository, "Staging");
        }

        #region GET
        [HttpGet]
        [Route("bitmapId/{bitmapId:int:min(1):max(9999)}")]
        public async Task<IHttpActionResult> GetByBitmapId(int bitmapId, bool active = true)
        {
            return Ok(await getLogoInfoAsync(bitmapId, active));
        }

        [HttpGet]
        [Route("Image/Exists")]
        public async Task<IHttpActionResult> GetBitmapExists(bool searchArchive = false)
        {
            string srchDir = this._activeLogoDir;

            if (searchArchive)
                srchDir = this._archiveLogoDir;

            var stream = await Request.Content.ReadAsStreamAsync();

            using (var bm = new Bitmap(stream))
            {
                if (await imageExists(srchDir, bm))
                    return Ok(true);
            }

            return Ok(false);
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
                                chLogo.IsAssigned = await isAssignedAsync(id);

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
        public Task<IHttpActionResult> GetImage(int bitmapId, bool archive = false)
        {
            var fileName = string.Format("{0}.png", bitmapId);
            var path = archive ? Path.Combine(this._archiveLogoDir, fileName) : Path.Combine(this._activeLogoDir, fileName);

            path = Path.GetFullPath(path);

            if (!path.StartsWith(this._logoRepository))
                throw new HttpException(403, "Forbidden");

            return Task.FromResult<IHttpActionResult>(new FileResult(path, "image/png"));
        }

        [HttpGet]
        [Route("IsAssigned/{bitmapId:int:range(0,10000)}")]
        public async Task<IHttpActionResult> IsAssigned(int bitmapId, string version = "1.9")
        {
            var result = await isAssignedAsync(bitmapId, version);
            return Ok(result);
        }

        [HttpGet]
        [Route("Exists/{bitmapId:int:range(0,10000)}")]
        public async Task<IHttpActionResult> IdExists(int bitmapId, bool searchArchive = false)
        {
            string srchDir = this._activeLogoDir;

            if (searchArchive)
                srchDir = this._archiveLogoDir;

            return Ok(await idExists(bitmapId, srchDir));
        }

        [HttpGet]
        [Route("NextAvailableId")]
        public async Task<IHttpActionResult> GetNextAvailableId(bool archive = false)
        {
            var result = await getNextAvailableId(archive);

            if (result > 0)
                return Ok(result);
            else
            {
                HttpError err = new HttpError("No available id's left.");
                return Content(HttpStatusCode.NotFound, err);
            }
        }
        #endregion GET

        #region PUT      
        [HttpPut]
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [Route("Image/Update/Archive/{serviceId:int}")]
        public async Task<IHttpActionResult> Archive(int serviceId)
        {
            try
            {
                var channel = new Channel();

                using (var chCtrl = new ChannelController())
                {
                    chCtrl.Request = new HttpRequestMessage();
                    chCtrl.Request.SetConfiguration(new HttpConfiguration());
                    channel = (await (await (await chCtrl.GetByServiceId(serviceId)).ExecuteAsync(CancellationToken.None)).Content.ReadAsAsync<IEnumerable<Channel>>()).FirstOrDefault();

                    if (channel == null)
                        return Content(HttpStatusCode.NotFound, string.Format("Could not find channel with service id {0}.", channel.ServiceID));

                    if (!File.Exists(channel.Logo.FileName))
                        return Content(HttpStatusCode.NotFound, string.Format("Unable to find logo id {0} in the active repository.", channel.Logo.BitmapId));


                    var newName = channel.CallSign.Replace(" ", "").Trim().ToUpper() + ".png";
                    var activeFile = channel.Logo.FileName;
                    var activeStageFile = Path.Combine(this._stagingDir, Path.GetFileName(activeFile));
                    var archiveFile = Path.Combine(this._archiveLogoDir, newName);
                    var archiveStageFile = Path.Combine(this._stagingDir, newName);

                    //Set channel logo to default logo
                    channel.Logo = await getLogoInfoAsync(10000);

                    if ((await (await chCtrl.UpdateChannel(channel.ServiceID, channel)).ExecuteAsync(CancellationToken.None)).StatusCode == HttpStatusCode.OK)
                    {
                        try
                        {
                            //backup active bitmap file to staging
                            File.Copy(activeFile, activeStageFile, true);

                            //if there is an existing archive file, back it up to staging and delete it
                            if (File.Exists(archiveFile))
                            {
                                File.Copy(archiveFile, archiveStageFile, true);
                                File.Delete(archiveFile);
                            }

                            //Move active bitmap file to the new location and rename it
                            File.Move(activeFile, archiveFile);
                        }
                        catch (Exception ex)
                        {
                            //Restore backup files from staging
                            if (File.Exists(archiveStageFile))
                                File.Copy(archiveStageFile, archiveFile, true);

                            if (File.Exists(activeStageFile))
                                File.Copy(activeStageFile, activeFile);

                            //return error
                            return InternalServerError(ex);
                        }
                        finally
                        {
                            //cleanup staging directory
                            if (File.Exists(archiveStageFile))
                                File.Delete(archiveStageFile);

                            if (File.Exists(activeStageFile))
                                File.Delete(activeStageFile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
            return Ok();
        }
        #endregion PUT

        #region POST
        [MimeMultipart]
        [HttpPost]
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [Route("Image/Upload")]
        public async Task<IHttpActionResult> Upload()
        {
            var uploadPath = HttpContext.Current.Server.MapPath("~/LogoRepository/Active");
            int bmId = await getNextAvailableId();

            if (bmId < 0)
            {
                HttpError err = new HttpError("No available id's left.");
                return Content(HttpStatusCode.InternalServerError, err);
            }

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

                    if (await imageExists(this._activeLogoDir, bm))
                    {
                        throw new Exception("Image already exists in active repository.");
                    }
                    
                    bm.Save(newFileName);

                    if (!File.Exists(newFileName))
                        return Content(HttpStatusCode.InternalServerError, "Image failed to upload");
                }
            }
            catch (Exception ex)
            {
                if (File.Exists(newFileName))
                    File.Delete(newFileName);

                return InternalServerError(ex);
            }
            finally
            {
                if (File.Exists(fileName))
                    File.Delete(fileName);
            }

            // Create response
            return Ok(new ChannelLogoInfo()
            {
                FileName = newFileName.Substring(newFileName.LastIndexOf("\\") + 1),
                BitmapId = bmId,
                IsAssigned = await isAssignedAsync(bmId),
            });
        }
        #endregion POST

        #region DELETE
        [HttpDelete]
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [Route("Image/Delete/Active/{id:int:range(0,10000)}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                if (id == 10000)
                    return Content(HttpStatusCode.Forbidden, "Cannot delete default image.");

                var fileName = Path.Combine(this._activeLogoDir, id + ".png");

                if (!File.Exists(fileName))
                    return Content(HttpStatusCode.NotFound, string.Format("Could not find {0}", fileName));       
            
                var path = Path.GetFullPath(fileName);
                var stagePath = Path.Combine(this._stagingDir, Path.GetFileName(path));

                //Backup to staging
                File.Copy(path, stagePath, true);

                //Delete
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    //Restore stage file and throw ex to outer try/catch
                    File.Copy(stagePath, path, true);
                    throw ex;
                }

                if (!File.Exists(path))
                {
                    //Delete stage file and return ok
                    File.Delete(stagePath);
                    return Ok();
                }
                else
                    return Content(HttpStatusCode.InternalServerError, "File failed to delete due to an unknown error. No changes were made.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Authorize(Roles = "VHE\\FUI-IMG, CORP\\FTW Data Center")]
        [Route("Image/Delete/Archive/{fileName}")]
        public IHttpActionResult Delete(string fileName)
        {
            try
            {
                if (fileName.StartsWith("10000"))
                    return Content(HttpStatusCode.Forbidden, "Cannot delete default image.");

                if (!fileName.EndsWith(".png"))
                    fileName += ".png";
                
                fileName = Path.Combine(this._archiveLogoDir, fileName);

                if (!File.Exists(fileName))
                    return Content(HttpStatusCode.NotFound, string.Format("Could not find {0}", fileName));

                var path = Path.GetFullPath(fileName);
                var stagePath = Path.Combine(this._stagingDir, Path.GetFileName(path));

                //Backup to staging
                File.Copy(path, stagePath, true);

                //Delete
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    //Restore stage file and throw ex to outer try/catch
                    File.Copy(stagePath, path, true);
                    throw ex;
                }

                if (!File.Exists(path))
                {
                    //Delete stage file and return ok
                    File.Delete(stagePath);
                    return Ok();
                }
                else
                    return Content(HttpStatusCode.InternalServerError, "File failed to delete due to an unknown error. No changes were made.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        #endregion DELETE

        #region PrivateMethods

        #region Async
        private async Task<bool> isAssignedAsync(int id, string version = "1.9")
        {
            string command = "SELECT TOP 1 * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version + "' AND intBitmapId = " + id;

            bool result = false;
            await DBFactory.SQL_ExecuteReaderAsync(WebApiConfig.ConnectionString, command, System.Data.CommandType.Text, null, dr =>
            {
                while (dr.Read())
                {
                    result = true;
                    break;
                }
            });
            return result;
        }
        #endregion Async

        #region Tasks
        private Task<bool> imageExists(string directory, Bitmap bitmap)
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.png", SearchOption.TopDirectoryOnly))
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
                                return Task.FromResult<bool>(true);
                        }
                    }
                }
            }
            return Task.FromResult<bool>(false);
        }

        private Task<int> getNextAvailableId(bool archive = false)
        {
            string srchDir = this._activeLogoDir;
            HttpResponseMessage responseMsg = new HttpResponseMessage(HttpStatusCode.OK);

            if (archive)
                srchDir = this._archiveLogoDir;

            for (int i = 1; i < 10000; i++)
            {
                string fileName = i + ".png";
                string filePath = Path.Combine(srchDir, fileName);

                if (!(File.Exists(filePath)))
                    return Task.FromResult<int>(i);
            }

            return Task.FromResult<int>(0);
        }
        #endregion Tasks

        #region Synchronous
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

        private bool isAssigned(int id, string version = "1.9")
        {
            string command = "SELECT TOP 1 * FROM vChannelMap WHERE strFIOSVersionAliasId = '" + version + "' AND intBitmapId = " + id;

            return DBFactory.SQL_ExecuteReader(WebApiConfig.ConnectionString, command, System.Data.CommandType.Text, null).Any();
        }
        #endregion Synchronous

        #endregion PrivateMethods

        #region InternalMethods

        #region Async
        internal async Task<ChannelLogoInfo> getLogoInfoAsync(int bitmapId, bool active = true)
        {
            ChannelLogoInfo logo = null;

            string srchDir = active ? this._activeLogoDir : this._archiveLogoDir;
            string filePath = Path.Combine(srchDir, string.Format("{0}.png", bitmapId));

            logo = new ChannelLogoInfo();

            if (File.Exists(filePath))
            {
                logo.FileName = filePath;
            }
            logo.BitmapId = bitmapId;
            logo.IsAssigned = await isAssignedAsync(bitmapId);

            return logo;
        }
        #endregion Async

        #region Tasks
        internal Task<bool> idExists(int id, string dir = null)
        {
            if (string.IsNullOrEmpty(dir))
                dir = this._activeLogoDir;

            foreach (var file in Directory.EnumerateFiles(dir, "*.png", SearchOption.TopDirectoryOnly))
            {
                var fileInfo = new FileInfo(file);
                int fileId;

                if (int.TryParse(fileInfo.Name.Replace(".png", "").Trim(), out fileId))
                {
                    if (fileId < 0 || fileId > 10000)
                        continue;

                    if (fileId == id)
                        return Task.FromResult<bool>(true);
                }
            }
            return Task.FromResult<bool>(false);
        }

        internal Task<IEnumerable<int>> getMissingBitmapIds()
        {
            List<int> missingIds = new List<int>();
            for (int i = 1; i < 10000; i++)
            {
                string fileName = i + ".png";
                string filePath = Path.Combine(this._activeLogoDir, fileName);

                if (!(File.Exists(filePath)))
                    missingIds.Add(i);
            }

            return Task.FromResult<IEnumerable<int>>(missingIds);
        }

        #endregion Tasks

        #region Synchronous
        internal ChannelLogoInfo getLogoInfo(int bitmapId, bool active = true)
        {
            ChannelLogoInfo logo = null;

            string srchDir = active ? this._activeLogoDir : this._archiveLogoDir;
            string filePath = Path.Combine(srchDir, string.Format("{0}.png", bitmapId));

            logo = new ChannelLogoInfo();
            if (File.Exists(filePath))
            {
                logo.FileName = filePath;
            }

            logo.BitmapId = bitmapId;
            logo.IsAssigned = isAssigned(bitmapId);

            return logo;
        }
        #endregion Synchronous

        #endregion InternalMethods
    }
}
