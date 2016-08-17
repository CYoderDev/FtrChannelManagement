using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FrontierVOps.Common;
using FrontierVOps.Data;

namespace FrontierVOps.FiOS.NGVODPoster
{
    internal class NGVodPosterController : IDisposable
    {
        #region Internal Properties
        /// <summary>
        /// Get or set the max number of images to process.
        /// </summary>
        internal int? MaxImages { get; set; }

        /// <summary>
        /// Progress of the process operation
        /// </summary>
        internal NgVodPosterProgress ngProgress { get; private set; }
        #endregion Internal Properties

        #region Private Fields
        /// <summary>
        /// Cancel token
        /// </summary>
        private CancellationToken token;

        /// <summary>
        /// Used to calculate the amount of time surpassed during processing
        /// </summary>
        private System.Timers.Timer timer;
        
        #endregion Private Fields

        /// <summary>
        /// Methods used to process VOD asset posters on a source server, and save them as resized
        /// posters on a destination server.
        /// </summary>
        /// <param name="token"></param>
        internal NGVodPosterController(CancellationToken token, IProgress<NgVodPosterProgress> progress)
        {
            //Set local property default values
            this.token = token;
            this.ngProgress = new NgVodPosterProgress();

            this.timer = new System.Timers.Timer();
            //set timer values and start it
            this.timer.Interval = 15000;
            this.timer.AutoReset = true;
            this.timer.Elapsed += async (sender, e) => await HandleTimer(this.ngProgress, progress);
            this.timer.Enabled = true;
            this.timer.Start();
            this.ngProgress.Time.Start();
        }

        /// <summary>
        /// Begins processing of NGVodPoster image files for the provided VHO
        /// </summary>
        /// <param name="vho">Name of the VHO</param>
        /// <param name="maxImages">Maximum numbere of images to process</param>
        /// <param name="config">NGVodPoster configuration</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Task result returns with all assets that do not have posters assigned</returns>
        internal async Task<IEnumerable<VODAsset>> BeginProcess(string vho, int? maxImages, NGVodPosterConfig config, CancellationToken token)
        {

            if (this.token.IsCancellationRequested)
                this.token.ThrowIfCancellationRequested();

            Console.WriteLine("\n\n----Beginning {0}----\n", vho);
            Trace.TraceInformation(vho);

            //Get the VHO values from the configuration
            NGVodVHO ngVho = null;

            if (!config.Vhos.TryGetValue(vho, out ngVho))
            {
                throw new Exception("Unable to get VHO from config");
            }

            //Get the T-SQL connection string for the IMG front end database
            string connectionStr = ngVho.IMGDb.CreateConnectionString();

            //Get VOD assets for the VHO
            var activeAssets = await GetVODAssets(connectionStr, maxImages, vho, config.SourceDir, ngVho.PosterDir);

            token.ThrowIfCancellationRequested();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            string indexFile = Path.Combine(Directory.GetCurrentDirectory(), vho + "_index.txt");

            //Start run task to ensure it ran to completion before attempting cleanup
            var mainTsk = Task.Factory.StartNew(() => ProcessAsset(ref activeAssets, config.SourceDir, config.ImgHeight, config.ImgWidth, config.MaxThreads, ngVho.PosterDir, ngVho.Name, indexFile, this.token));

            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                this.ngProgress.StopProgress = false;
                //Wait to finish
                await mainTsk;
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.InnerExceptions)
                    Trace.TraceError("Error during processing method. {0}", ex.Message);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            //Clean up poster directory based on the vho's active assets if the process had no errors, no max values were specified,
            //and cancellation was not requested,
            try
            {
                if (mainTsk.Status == TaskStatus.RanToCompletion && !maxImages.HasValue && !token.IsCancellationRequested)
                {
                    await Task.Factory.StartNew(() => cleanupDestination(activeAssets.Select(x => x.AssetId), ngVho.PosterDir, config.MaxThreads));
                }
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.InnerExceptions)
                    Trace.TraceError("Error while cleaning up destination directory. {0}", ex.Message);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while cleaning up destination directory. {0}", ex.Message);
            }

            //Return missing posters
            return activeAssets;
        }
        
        /// <summary>
        /// Reports completion to trace output
        /// </summary>
        internal void Complete()
        {
            this.ngProgress.Time.Stop();
            if (this.ngProgress.IsComplete)
                Trace.TraceInformation("\n\nImage Resize and Copy Complete!");
            else if (this.ngProgress.IsCanceled)
                Trace.TraceInformation("\n\nImage Resize and Copy Canceled!");

            if (this.ngProgress.IsComplete || this.ngProgress.IsCanceled)
            {
                Trace.TraceInformation(string.Format("Runtime: {0} hours, {1} minutes, {2} seconds", this.ngProgress.Time.Elapsed.Hours, this.ngProgress.Time.Elapsed.Minutes, this.ngProgress.Time.Elapsed.Seconds));
                Trace.TraceInformation("\nResult:");
                Trace.TraceInformation(string.Format("Successful: {0} ({1:P2})", this.ngProgress.Success, ((decimal)this.ngProgress.Success / (decimal)this.ngProgress.Total)));
                Trace.TraceInformation(string.Format("Failed: {0} ({1:P2})", this.ngProgress.Failed, ((decimal)this.ngProgress.Failed / (decimal)this.ngProgress.Total)));
                Trace.TraceInformation(string.Format("Skipped: {0} ({1:P2})", this.ngProgress.Skipped, ((decimal)this.ngProgress.Skipped / (decimal)this.ngProgress.Total)));
                Trace.TraceInformation(string.Format("Total: {0}", this.ngProgress.Total));
            }
            this.ngProgress.IsComplete = !this.ngProgress.IsCanceled;
        }

        #region Get Data Methods
        /// <summary>
        /// Gets all VOD assets from all vho's in the config
        /// </summary>
        /// <param name="config">Configuration parameters</param>
        /// <returns></returns>
        internal IEnumerable<VODAsset> GetAllVodAssets(ref NGVodPosterConfig config, string indexFileDir)
        {

#if DEBUG
            int f = 0;
            int s = 0;
#endif

            List<VODAsset> vassets = new List<VODAsset>();
            foreach(var vho in config.Vhos)
            {
                string conStr = vho.Value.IMGDb.CreateConnectionString();
                string sproc = "sp_FUI_GetAllVODFolderAssetInfo";

                
                foreach (var dr in DBFactory.SQL_ExecuteReader(conStr, sproc, System.Data.CommandType.StoredProcedure))
                {
                    var vAsset = new VODAsset();
                    var vFolder = new VODFolder();

                    vFolder.ID = int.Parse(dr.GetString(0));
                    vFolder.ParentId = int.Parse(dr.GetString(1));
                    vFolder.Path = dr.GetString(2);
                    vFolder.Title = dr.GetString(3);
                    vAsset.AssetId = int.Parse(dr.GetString(4));
                    vAsset.Title = dr.GetString(5);
                    vAsset.PID = dr.GetString(6);
                    vAsset.PAID = dr.GetString(7);
                    vAsset.Folders.Add(vFolder);
                    
                    vAsset.PosterDest = GetDestImagePath(vAsset.AssetId, vho.Value.PosterDir);
                    vassets.Add(vAsset);
                }
            }

            vassets = vassets.GroupBy(x => new { x.AssetId, x.PID, x.PAID, x.Title, x.PosterDest })
                                        .Select(x => new VODAsset()
                                        {
                                            AssetId = x.Key.AssetId,
                                            Title = x.Key.Title,
                                            PID = x.Key.PID,
                                            PAID = x.Key.PAID,
                                            Folders = x.Where(y => y.AssetId.Equals(x.Key.AssetId)).SelectMany(y => y.Folders).Distinct().ToList(),
                                            PosterDest = x.Key.PosterDest,
                                        }).Distinct().ToList();

            var fromDict = GetAllVodAssets(indexFileDir, config.SourceDir);
            ParallelOptions po = new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount, CancellationToken = this.token };
            Parallel.ForEach(vassets, po, (va) =>
                {
                    po.CancellationToken.ThrowIfCancellationRequested();
                    va.PosterSource = fromDict.Where(x => x.Key.Item1.Equals(va.AssetId) && x.Value.Contains(va.PID) && x.Value.Contains(va.PAID)).Select(x => x.Value).FirstOrDefault();

#if DEBUG
                    if (string.IsNullOrEmpty(va.PosterSource))
                        f++;
                    else
                        s++;

                    //Trace.TraceInformation(vAsset.ToString());
                    Console.Write("\rFound: {0} - Not Found: {1} - Total Di: {2} - Total Assets: {3}      ", s, f, fromDict.Count, vassets.Count);
#endif
                });

            return vassets;
        }

        /// <summary>
        /// Get all VOD Assets from index file
        /// </summary>
        /// <param name="indexFilePath">Directory where the index file is located</param>
        /// <param name="srcPath">Directory where the source poster files are located</param>
        /// <param name="vhoName">Name of the VHO being processed</param>
        /// <returns></returns>
        internal IDictionary<Tuple<int, string>, string> GetAllVodAssets(string indexFilePath, string srcPath, string vhoName = "*")
        {

            var ret = new Dictionary<Tuple<int, string>, string>();

            var indexFiles = Directory.EnumerateFiles(indexFilePath, vhoName.ToUpper() + "_index.txt", SearchOption.TopDirectoryOnly);

            foreach (var file in indexFiles)
            {
                string vho = file.Substring(0, file.IndexOf("_")).Trim();

                using (var fs = File.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    string line = string.Empty;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var objs = line.Split('|');
                        int id;
                        if (int.TryParse(objs[0], out id))
                        {
                            var tpl = new Tuple<int, string>(id, vho);
                            if (objs[1].Contains(srcPath))
                                ret.Add(tpl, objs[1]);
                            else
                                ret.Add(tpl, Path.Combine(srcPath, objs[1]));

                        }
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Try to get the source file for a VOD asset from the index file
        /// </summary>
        /// <param name="vasset">Vod Asset to try to locate a source file</param>
        /// <param name="indexFileDir">Directory where the index file is located</param>
        /// <param name="srcDir">Directory where the source files are located</param>
        /// <param name="srcFile">Source file for the VOD asset</param>
        /// <returns>Whether it was able to locate the source file</returns>
        internal bool TryGetSrcFile(VODAsset vasset, string indexFileDir, string srcDir, out string srcFile)
        {
            srcFile = GetAllVodAssets(indexFileDir, srcDir).Where(x => x.Key.Item1.Equals(vasset.AssetId) && x.Value.Contains(vasset.PID) && x.Value.Contains(vasset.PAID)).Select(x => x.Value).FirstOrDefault();

            if (File.Exists(srcFile))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Get VOD assets from the database asyncronously for a VHO
        /// </summary>
        /// <param name="ConnectionString">SQL connection string</param>
        /// <param name="maxAssets">Max number of assests to get</param>
        /// <param name="vhoName">Name of the vho being processed</param>
        /// <param name="srcDir">Poster source directory</param>
        /// <param name="destDir">Poster destination directory</param>
        /// <returns>All VOD Assets for a particular VHO</returns>
        internal async Task<IEnumerable<VODAsset>> GetVODAssets(string ConnectionString, int? maxAssets, string vhoName, string srcDir, string destDir)
        {
            Trace.TraceInformation("INFO({0}): Getting VOD Asset Info from Database", vhoName);

            string sproc = "sp_FUI_GetAllVODFolderAssetInfo";
            List<VODAsset> vodAssets = new List<VODAsset>();
            await DBFactory.SQL_ExecuteReaderAsync(ConnectionString, sproc, System.Data.CommandType.StoredProcedure, null, dr =>
                {
                    while (dr.Read())
                    {
                        //only add assets up to max value if set
                        if (maxAssets.HasValue && vodAssets.Count == maxAssets.Value)
                            break;
                        try
                        {
                            var vAsset = new VODAsset();
                            var vFolder = new VODFolder();

                            vFolder.ID = int.Parse(dr.GetString(0));
                            vFolder.ParentId = int.Parse(dr.GetString(1));
                            vFolder.Path = dr.GetString(2);
                            vFolder.Title = dr.GetString(3);
                            vAsset.AssetId = int.Parse(dr.GetString(4));
                            vAsset.Title = dr.GetString(5);
                            vAsset.PID = dr.GetString(6);
                            vAsset.PAID = dr.GetString(7);
                            vAsset.Folders.Add(vFolder);
                            vAsset.PosterDest = GetDestImagePath(vAsset.AssetId, destDir);
                            
                            vodAssets.Add(vAsset);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("ERROR({0}): (GetVODAssets - SQL Data Read) --> {1}", vhoName, ex.Message);
                        }
                    }
                });

            //Group all assets and merge the vod folders
            vodAssets = vodAssets
                .GroupBy(x => new { x.AssetId, x.PID, x.PAID, x.Title, x.PosterDest })
                .Select(x => new VODAsset()
                {
                    AssetId = x.Key.AssetId,
                    Title = x.Key.Title,
                    PID = x.Key.PID,
                    PAID = x.Key.PAID,
                    PosterDest = x.Key.PosterDest,
                    Folders = x.Where(y => y.AssetId.Equals(x.Key.AssetId)).SelectMany(y => y.Folders).Distinct().ToList(),
                }).Distinct().ToList();

            //Get all VOD Assets from the index file into a dictionary
            IDictionary<Tuple<int, string>, string> fromDict = GetAllVodAssets(Directory.GetCurrentDirectory(), srcDir, vhoName);

            //Get the VOD poster source file from the index if it exists
            Parallel.ForEach(vodAssets, (va) =>
                {
                    va.PosterSource = fromDict.Where(x => x.Key.Item1.Equals(va.AssetId) && x.Value.Contains(va.PID) && x.Value.Contains(va.PAID)).Select(x => x.Value).FirstOrDefault();
                });
#if DEBUG
            //vodAssets.ForEach(x => Trace.WriteLine(string.Format("\nAssetId: {0}\nPID: {1}\nPaid: {2}\nTitle: {3}\nFolders: {4}", x.AssetId, x.PID, x.PAID, x.Title, string.Join(",", x.Folders.Select(y => y.Path)))));
#endif

            Trace.TraceInformation("\nGet VOD Assets Complete --> Count: {0}", vodAssets.Count);
            return vodAssets;
        }

        /// <summary>
        /// Tries to get the source file path by enumerating the source directory and matching the PID PAID value
        /// </summary>
        /// <param name="PID"></param>
        /// <param name="PAID"></param>
        /// <param name="srcPath"></param>
        /// <returns></returns>
        private string GetSourceImagePath(string PID, string PAID, string srcPath)
        {
            string file = string.Empty;

            //First attempt to get it by a commonly used file name
            string commonName = Path.Combine(srcPath, string.Format("IMG_{0}_{1}_{1}_POS.jpg", PID, PAID, PAID));

            //If common file name doesn't exist, try enumerating the directory for "PID_PAID"
            if (File.Exists(commonName))
            {
                file = commonName;
            }
            else
            {
                string fileQuery = string.Format("*{0}_{1}*", PID, PAID);

                file = Directory.EnumerateFiles(srcPath, fileQuery, SearchOption.TopDirectoryOnly).AsParallel()
                    .FirstOrDefault();
            }

            return file;
        }

        /// <summary>
        /// Gets the name that the image file should be saved as when processed.
        /// </summary>
        /// <param name="AssetId">Asset ID of the VOD asset being processed</param>
        /// <param name="DestPath">Path to where the image will be saved after being processed</param>
        /// <returns></returns>
        private string GetDestImagePath(int AssetId, string DestPath)
        {
            return Path.Combine(DestPath, AssetId.ToString() + ".jpg");
        }
        #endregion Get Data Methods

        #region Processing Methods
        /// <summary>
        /// Begins matching asset id's to source image files, and if found it will resize and save the file to the destination
        /// </summary>
        /// <param name="VAssets">List of the VHO's VOD assets</param>
        /// <param name="config">The NGVodPosterConfig configuration</param>
        /// <param name="posterDest">The UNC path to the poster destination directory</param>
        /// <param name="vhoName">Name of the VHO that is being processed</param>
        /// <param name="dictSrcPath">Dictionary of asset id to source file mapping</param>
        /// <param name="indexFile">Index file path</param>
        /// <param name="cancelToken">Cancellation token</param>
        private void ProcessAsset(ref IEnumerable<VODAsset> VAssets, string srcDir, int imgHeight, int imgWidth, int maxThreads, 
            string posterDest, string vhoName, string indexFile, CancellationToken cancelToken)
        {
            Trace.TraceInformation("INFO({0}): Processing VOD Asset Posters...", vhoName.ToUpper());

            var exceptions = new ConcurrentQueue<Exception>();

            //Begin processing each VOD asset obtained from the database asyncronously
            ParallelOptions po = new ParallelOptions() { MaxDegreeOfParallelism = maxThreads, CancellationToken = cancelToken };

            //Add to vod asset count to progress total
            Interlocked.Add(ref this.ngProgress.Total, VAssets.Count());

            try
            {          
                Parallel.ForEach<VODAsset>(VAssets.OrderByDescending(x => x.PosterSource != null).ThenByDescending(x => x.AssetId), po, (va) =>
                    {
                        try
                        {
                            po.CancellationToken.ThrowIfCancellationRequested();

                            //Get poster source if it doesn't already exist, or if it doesn't contain the PID/PAID values of the asset
                            if (string.IsNullOrEmpty(va.PosterSource) || !File.Exists(va.PosterSource) || !va.PosterSource.Contains(va.PID) || !va.PosterSource.Contains(va.PAID))
                            {
                                try
                                {
                                    va.PosterSource = GetSourceImagePath(va.PID, va.PAID, srcDir);
                                }
                                catch (Exception ex)
                                {
                                    //Increment progress failed value if error was thrown
                                    Interlocked.Increment(ref this.ngProgress.Failed);
                                    Trace.TraceError("Error getting source image path. {0}", ex.Message);
                                    throw;
                                }                               

                                if (string.IsNullOrEmpty(va.PosterSource))
                                {
                                    //If file exists on destination server but does not on the source server, then delete it on the destination.
                                    //This prevents incorrect posters from being displayed if the asset ID is changed by the VOD provider.
                                    if (File.Exists(va.PosterDest))
                                    {
                                        File.Delete(va.PosterDest);
                                        Interlocked.Increment(ref this.ngProgress.Deleted);
                                    }

                                    //NoPosterAssets.Enqueue(va);
                                    Interlocked.Increment(ref this.ngProgress.Failed);
                                    return;
                                }
                            }
                            else
                            {
                                va.PosterDest = GetDestImagePath(va.AssetId, posterDest);
                            }

                            try
                            {
                                //Resize and save the image to the destination
                                var res = ProcessImage(va, imgHeight, imgWidth, vhoName, po.CancellationToken);
                                    
                                switch (res)
                                {
                                    case 0:
                                        Interlocked.Increment(ref this.ngProgress.Success);
                                        break;
                                    case 1:
                                        Interlocked.Increment(ref this.ngProgress.Skipped);
                                        break;
                                    default:
                                        Interlocked.Increment(ref this.ngProgress.Failed);
                                        break;
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                Interlocked.Increment(ref this.ngProgress.Skipped);
                            }
                            catch (Exception ex)
                            {
                                Interlocked.Increment(ref this.ngProgress.Failed);
                                Trace.TraceError("Error processing image for {0} in {1}. {2}", va.AssetId, vhoName, ex.Message);
                                throw;
                            }

                            po.CancellationToken.ThrowIfCancellationRequested();
                        }
                        catch (OperationCanceledException)
                        {                            
                            throw;
                        }
                        catch (Exception ex)
                        {
                            exceptions.Enqueue(ex);
                        }
                    });
            }
            catch (OperationCanceledException)
            {
                this.ngProgress.StopProgress = true;
                this.ngProgress.IsCanceled = true;
            }

            this.ngProgress.StopProgress = true;
            this.ngProgress.IsComplete = !this.ngProgress.IsCanceled;

            //Write indexes
            try
            {
                Trace.TraceInformation("INFO({0}): Writing Indexes", vhoName);
                createIndex(vhoName, srcDir, ref VAssets, indexFile);
            }
            catch (Exception ex)
            {
                exceptions.Enqueue(new Exception("Failed to write to index file. " + ex.Message, ex));
            }
                        
            if (exceptions.Count > 0)
                throw new AggregateException(exceptions).Flatten();
        }

        /// <summary>
        /// Resizes and saves the image as the asset ID jpg
        /// </summary>
        /// <param name="VAsset">VOD Asset</param>
        /// <param name="Height">Final height of the image</param>
        /// <param name="Width">Final width of the image</param>
        /// <param name="token">Cancellation token</param>
        private int ProcessImage(VODAsset VAsset, int Height, int Width, string vhoName, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            //Temporary folder path used to backup any existing images before any manipulation
            string tmpPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp");
            string tmpFile = string.Empty;

            //Create root temp folder if it doesn't exist
            if (!Directory.Exists(tmpPath))
                Directory.CreateDirectory(tmpPath);

            //Make a temp path child folder by VHO name to prevent asset id conflicts
            tmpPath = Path.Combine(tmpPath, vhoName);
            if (!Directory.Exists(tmpPath))
                Directory.CreateDirectory(tmpPath);
         
            try
            {
                //Poster destination file name cannot be null
                if (string.IsNullOrEmpty(VAsset.PosterDest))
                {
                    throw new ArgumentNullException(string.Format("Poster destination null. \n\tTitle:  {0}\n\tAssetID: {1}\n\tPID: {2}\n\tPAID: {3}\n\tFolderPath: {4}\n\tFolderId: {5}",
                        VAsset.Title, VAsset.AssetId, VAsset.PID, VAsset.PAID, string.Join(",", VAsset.Folders.Select(x => x.Path)), string.Join(",", VAsset.Folders.Select(x => x.ID))));
                }

                //Verify file extension is .jpg
                if (!VAsset.PosterDest.EndsWith(".jpg"))
                    throw new ArgumentException("(ProcessImages) Invalid destination file name.", VAsset.PosterDest);

                //Throw error if source file not found, used to populate missing poster log
                if (!File.Exists(VAsset.PosterSource))
                    throw new FileNotFoundException(string.Format("(ProcessImages) Source poster file not found. AssetID: {0}", VAsset.AssetId), VAsset.PosterSource);

                FileInfo destFInfo = new FileInfo(VAsset.PosterDest);
                FileInfo srcFInfo = new FileInfo(VAsset.PosterSource);

                //if destination file already exists, check if it needs updated using timestamp
                if (destFInfo.Exists)
                {
                    //Skip if file is newer, and increment progress skipped
                    if (destFInfo.LastWriteTime.CompareTo(srcFInfo.LastWriteTime) >= 0 
                        && destFInfo.CreationTime.CompareTo(srcFInfo.CreationTime) >= 0)
                    {
                        return 1;
                    }

                    tmpFile = Path.Combine(tmpPath, Path.GetFileName(VAsset.PosterDest));

                    //If a temp file exists in the temp directory for this asset, remove it
                    if (File.Exists(tmpFile))
                        File.Delete(tmpFile);

                    //Move the file to the temp directory, and set it as a temp file
                    File.Move(VAsset.PosterDest, tmpFile);
                    File.SetAttributes(tmpFile, FileAttributes.Temporary);
                }

                //Resize source image file if it is above 50MB
                try
                {
                    if (((srcFInfo.Length / 1024F) / 1024F) > 50)
                    {
                        string tmpName = Path.Combine(Path.GetFullPath(srcFInfo.FullName), Path.GetFileNameWithoutExtension(srcFInfo.FullName) + "_tmp.jpg");
                        using (var srcBM = Toolset.ResizeBitmap(srcFInfo.FullName, 200, null, null, null, false))
                        {
                            srcBM.Save(tmpName, ImageFormat.Jpeg);
                        }

                        File.Copy(tmpName, srcFInfo.FullName, true);
                        File.Delete(tmpName);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Failed to resize source image file above 50mb. {0}", ex.Message);
                }


                //Resize poster, save, and increment progress success if no errors
                using (var sourceBM = new Bitmap(VAsset.PosterSource))
                using (var destBM = Toolset.ResizeBitmap(sourceBM, Width, Height, null, null, true))
                {
                    token.ThrowIfCancellationRequested();
                    destBM.Save(VAsset.PosterDest, ImageFormat.Jpeg);
                }


                //Verify file was saved and it exists
                if (!File.Exists(VAsset.PosterDest))
                    throw new Exception("(ProcessImages) File failed to savee in destination folder.");

                return 0;
            }
            catch
            {
                //Restore backup file from temp directory
                if (!string.IsNullOrEmpty(tmpFile) && File.Exists(tmpFile))
                    File.Copy(tmpFile, VAsset.PosterDest, true);

                throw;
            }
            finally
            {
                //Delete backup file from temp directory
                if (!string.IsNullOrEmpty(tmpFile) && File.Exists(tmpFile))
                    File.Delete(tmpFile);
            }
        }
        #endregion Process Methods

        #region Cleanup Methods
        /// <summary>
        /// Cleans up the poster source directory based on all active assets from all 3 vho's
        /// </summary>
        /// <param name="AllVODAssets"></param>
        /// <param name="config"></param>
        public void CleanupSource(IEnumerable<VODAsset> AllVODAssets, ref NGVodPosterConfig config)
        {
            cleanupSource(AllVODAssets.Where(x => !(string.IsNullOrEmpty(x.PosterSource))).Select(x => x.PosterSource), config.SourceDir);
        }

        /// <summary>
        /// Performs a cleanup of the source directory by removing any posters that are not assigned to any active
        /// assets across all VHO's. Only perform this operation if all processes have performed successfully.
        /// </summary>
        /// <param name="usedSourceFiles">All active assets in all vho's.</param>
        /// <param name="srcPath">Full path to the source directory.</param>
        private void cleanupSource(IEnumerable<string> usedSourceFiles, string srcPath)
        {
            //Get all unused assets.
            var unusedSrcFiles = Directory.EnumerateFiles(srcPath).Except(usedSourceFiles);

            Trace.TraceInformation("INFO: Archiving unused posters. Count => {0}", unusedSrcFiles.Count());
#if DEBUG
            Trace.TraceInformation("Source Path: " + srcPath);
            Trace.TraceInformation("Unused Source Files: ");
            Trace.TraceInformation(string.Join(System.Environment.NewLine, unusedSrcFiles.Select(x => Path.GetFileName(x))));
#endif

            var archiveDir = Path.Combine(srcPath, "Archive");

            //create an archive directory to store the newly cleaned up files
            if (!Directory.Exists(archiveDir))
                Directory.CreateDirectory(archiveDir);          
            
            Parallel.ForEach(unusedSrcFiles, (delFile) =>
                {
                    this.token.ThrowIfCancellationRequested();

                    //if the file exists, copy to the archive and delete it
                    if (File.Exists(delFile))
                    {
                        var archFileName = Path.Combine(archiveDir, Path.GetFileName(delFile));
                        try
                        {
#if DEBUG
                            Trace.TraceInformation("Copying {0} to {1} and deleting {0}", Path.GetFileName(delFile), Path.GetFileName(archFileName));
#else
                            File.Copy(delFile, archFileName, true);
                            File.Delete(delFile);
#endif
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error while cleaning up source directory file {0}. {1}", delFile, ex.Message);

                            try
                            {
                                if (File.Exists(archFileName) && !File.Exists(delFile))
                                {
#if DEBUG
                                    Trace.TraceInformation("Copying {0} back to {1} and overwriting", Path.GetFileName(archFileName), Path.GetFileName(delFile));
#else
                                File.Copy(archFileName, delFile, true);
#endif
                                }
                            }
                            catch (Exception exe)
                            {
                                Trace.TraceError("Failed to restore {0} from archive. {1}", archFileName, exe.Message);
                            }
                        }
                    }
                });

            //Cleanup the archive directory of any files older than 90 days
            Trace.WriteLine("INFO: Cleaning old posters from archive directory...");
            Parallel.ForEach(Directory.EnumerateFiles(archiveDir), (archiveFile) =>
                {
                    this.token.ThrowIfCancellationRequested();

                    if (File.Exists(archiveFile))
                    {
                        FileInfo fInfo = new FileInfo(archiveFile);

                        if (fInfo.LastWriteTime.CompareTo(DateTime.Now.AddDays(90)) <= 0)
                        {
                            try
                            {
#if DEBUG
                                Trace.TraceInformation("Deleting {0}", fInfo.FullName);
#else
                                fInfo.Delete();
#endif
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("Failed to delete {0} while cleaning up source directory. {1}", fInfo.FullName, ex.Message);
                            }
                        }
                    }
                });
        }

        /// <summary>
        /// Clean up the VHO's poster directory by removing any posters that are not active
        /// </summary>
        /// <param name="usedAssetIds">All used asset id's in the VHO</param>
        /// <param name="DestDir">VHO destination directory UNC path</param>
        /// <param name="maxThreads">Max number of threads to use during the clean up process</param>
        private void cleanupDestination(IEnumerable<int>usedAssetIds, string DestDir, int maxThreads)
        {
            List<string> usedFiles = new List<string>();

            Trace.TraceInformation("Cleaning up {0}. Active Count => {1}", DestDir, usedAssetIds.Count());

            ParallelOptions po = new ParallelOptions() { MaxDegreeOfParallelism = System.Environment.ProcessorCount };

            //Verify each file exists and add to usedFiles list if it does
            Parallel.ForEach<int>(usedAssetIds, po, (usedId) =>
                {
                    try
                    {
                        string fileName = Path.Combine(DestDir, string.Format("{0}.jpg", usedId));

                        if (File.Exists(fileName))
                            usedFiles.Add(fileName);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error cleaning destination folder while obtaining active poster {0}. {1}", usedId, ex.Message);
                    }
                });

            //Get files to delete
            var delFiles = Directory.EnumerateFiles(DestDir).Except(usedFiles);

            Trace.TraceInformation("Number of files to remove from {0} => {1}", DestDir, delFiles.Count());

            //Delete the file
            Parallel.ForEach<string>(delFiles, po, (delFile) =>
                {
                    try
                    {
                        if (File.Exists(delFile))
                        {
                            //Verify that the used list does not contain the asset id from the file name
                            //before deleting.
                            int id;
                            if (int.TryParse(Path.GetFileNameWithoutExtension(delFile), out id) && !usedAssetIds.Contains(id))
                            {
#if DEBUG
                                Trace.TraceInformation("Deleting {0} from {1}", delFile, DestDir);
#else
                                File.Delete(delFile);
#endif
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error in cleaning destination folder while removing {0}. {1}", delFile, ex.Message);
                    }
                });
        }
        #endregion Cleanup Methods

        #region Indexing
        /// <summary>
        /// Writes the asset id to source file map to the index file
        /// </summary>
        /// <param name="vho">Name of the vho being processed</param>
        /// <param name="srcDir">Directory where source files are located</param>
        /// <param name="activeAssets">All actively used assets</param>
        /// <param name="indexFile">Full path to the index file</param>
        private void createIndex(string vho, string srcDir, ref IEnumerable<VODAsset> activeAssets, string indexFile)
        {
            if (!File.Exists(indexFile) || this.MaxImages.HasValue)
                return;

            IDictionary<int, string> srcDict = readIndex(vho, srcDir);

            foreach (var asset in activeAssets)
            {
                if (srcDict.ContainsKey(asset.AssetId))
                {
                    string srcFile = srcDict[asset.AssetId].Contains(srcDir) ? srcDict[asset.AssetId] : Path.Combine(srcDir, srcDict[asset.AssetId]);
                    
                    //Skip if the poster source value on the asset is incorrect but the dictionary file is correct, or if everything is ok
                    if ((string.IsNullOrEmpty(asset.PosterSource) && srcFile.Contains(asset.PID) && srcFile.Contains(asset.PAID) && File.Exists(srcFile))
                        || (!string.IsNullOrEmpty(asset.PosterSource) && asset.PosterSource == srcFile && File.Exists(srcFile)))
                    {
                        continue;
                    }
                    //Remove Value from dictionary if the asset poster source was not found, the file in the dictionary doesn't exist, or it doesn't contain the PID/PAID value
                    else if (string.IsNullOrEmpty(asset.PosterSource) || !File.Exists(srcFile) || !srcFile.Contains(asset.PID) || !srcFile.Contains(asset.PAID))
                    {
                        srcDict.Remove(asset.AssetId);
                    }
                    //Update Value if the asset poster is not the same as the dictionary source file, and it exists and contains the PID/PAID values
                    else if (!string.IsNullOrEmpty(asset.PosterSource) && asset.PosterSource != srcFile && 
                        asset.PosterSource.Contains(asset.PID) && asset.PosterSource.Contains(asset.PAID) && File.Exists(asset.PosterSource))
                    {
                        srcDict[asset.AssetId] = Path.GetFileName(asset.PosterSource);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(asset.PosterSource))
                        continue;

                    if (File.Exists(asset.PosterSource) && asset.PosterSource.Contains(asset.PID) && asset.PosterSource.Contains(asset.PAID))
                        srcDict.Add(asset.AssetId, Path.GetFileName(asset.PosterSource));
                }
            }
            
            List<string> contents = new List<string>();
            foreach (var entry in srcDict)
            {
                contents.Add(string.Join("|", entry.Key, entry.Value));
            }
            File.WriteAllLines(indexFile, contents);
        }

        /// <summary>
        /// Reads the index file
        /// </summary>
        /// <param name="vho">Name of the vho being processed</param>
        /// <param name="srcDir">Directory where the source files are located</param>
        /// <returns>A dictionary of the asset id (key) and source file path</returns>
        private IDictionary<int, string> readIndex(string vho, string srcDir)
        {
            string indexFile = Path.Combine(Directory.GetCurrentDirectory(), vho + "_index.txt");

            //Create dictionary for indexing asset id to source image file
            IDictionary<int, string> dictSrcPath = new Dictionary<int, string>();

            Trace.TraceInformation("INFO({0}): Reading Indexes...", vho.ToUpper());
            Console.WriteLine("\nReading Indexes for {0}...This could take a few minutes", vho.ToUpper());

            //Read index file and populate dictionary with asset id to source path image file mapping
            using (var fs = File.Open(indexFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    var objs = line.Split('|');
                    int id;
                    if (int.TryParse(objs[0], out id))
                    {
                        string srcFile = objs[1];
                        if (!srcFile.Contains(srcDir))
                            srcFile = Path.Combine(srcDir, srcFile);

                        if (File.Exists(srcFile)
                            && !dictSrcPath.ContainsKey(id))
                        {
                            dictSrcPath.Add(id, Path.GetFileName(srcFile));
                        }
                        else if (dictSrcPath.ContainsKey(id))
                        {
                            Trace.TraceError(string.Format("WARNING({0}): Duplicate asset id in index. Id: {1}", vho.ToUpper(), id));
                        }
                    }
                }
            }
            return dictSrcPath;
        }
        #endregion Indexing

        /// <summary>
        /// Timer handle to report progress
        /// </summary>
        /// <returns></returns>
        private Task HandleTimer(NgVodPosterProgress progress, IProgress<NgVodPosterProgress> iProgress)
        {
            //((IProgress<int>)progress).Report(1);
            iProgress.Report(progress);
            return Task.FromResult(0);
        }

        #region Dispose
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.MaxImages = null;
                //this.AllVAssets = null;
                this.timer.Stop();
                this.timer.Dispose();
                this.ngProgress.Dispose();
            }
            Console.ResetColor();
        }

        ~NGVodPosterController()
        {
            Dispose(false);
        }
        #endregion Dispose
    }
}
