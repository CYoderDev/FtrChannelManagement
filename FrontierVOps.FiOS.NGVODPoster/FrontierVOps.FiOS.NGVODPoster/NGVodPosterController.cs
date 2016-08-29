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
        internal async Task BeginProcess(string vho, int? maxImages, NGVodPosterConfig config)
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
            string connectionStr = ngVho.IMGDb.CreateConnectionString(true);

#if DEBUG
            Trace.TraceInformation("Connection string for {0} --> {1}", vho, connectionStr);
#endif

            this.token.ThrowIfCancellationRequested();
            
            if (ngProgress.Total != 0)
                this.ngProgress.Reset();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            //Start run task to ensure it ran to completion before attempting cleanup
            var mainTsk = Task.Factory.StartNew(() => 
                ProcessAsset(config, ngVho.Name, ngVho.PosterDir, connectionStr, this.token));

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

            if (ngProgress.CompleteCount > config.Vhos.Count && ngProgress.CompleteCount % config.Vhos.Count == 0 && !token.IsCancellationRequested)
            {
                await Task.Delay(15000);
                this.ngProgress.StopProgress = true;
                this.ngProgress.IsComplete = !this.ngProgress.IsCanceled;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            await cleanupDatabase(connectionStr, vho, this.token);

            //Clean up poster directory based on the vho's active assets if the process had no errors, no max values were specified,
            //and cancellation was not requested.

            if (mainTsk.Status == TaskStatus.RanToCompletion && !maxImages.HasValue && !token.IsCancellationRequested)
            {
                try
                {
                    await Task.Factory.StartNew(() => 
                        cleanupDestination(GetVODAssets(connectionStr, maxImages, vho, config.SourceDir, ngVho.PosterDir, this.token).Select(x => x.AssetId), ngVho.PosterDir, config.MaxThreads))
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error while cleaning up destination directory. {0}", ex.Message);
                }
            }
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
        internal IEnumerable<VODAsset> GetAllVodAssets(NGVodPosterConfig config, CancellationToken cancelToken)
        {
            List<VODAsset> vassets = new List<VODAsset>();
            foreach(var vho in config.Vhos)
            {
                cancelToken.ThrowIfCancellationRequested();
                string conStr = vho.Value.IMGDb.CreateConnectionString();
                string sproc = "sp_FUI_GetAllVODFolderAssetInfo";

                
                foreach (var dr in DBFactory.SQL_ExecuteReader(conStr, sproc, System.Data.CommandType.StoredProcedure))
                {
                    cancelToken.ThrowIfCancellationRequested();
                    var vAsset = new VODAsset();

                    vAsset.AssetId = int.Parse(dr.GetString(0));
                    vAsset.Title = dr.GetString(1);
                    vAsset.PID = dr.GetString(2);
                    vAsset.PAID = dr.GetString(3);
                    vAsset.PosterSource = dr.GetString(4);
                    vAsset.PosterDest = GetDestImagePath(vAsset.AssetId, vho.Value.PosterDir);
                    vassets.Add(vAsset);

                    yield return vAsset;
                }
            }
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
        internal IEnumerable<VODAsset> GetVODAssets(string ConnectionString, int? maxAssets, string vhoName, string srcDir, string destDir, CancellationToken cancelToken)
        {
            Trace.TraceInformation("INFO({0}): Getting VOD Asset Info from Database", vhoName);

            string sproc = "sp_FUI_GetAllVODFolderAssetInfo";
            int index = 1;
            foreach (var dr in DBFactory.SQL_ExecuteReader(ConnectionString, sproc, System.Data.CommandType.StoredProcedure, null))
            {
                cancelToken.ThrowIfCancellationRequested();
                //only add assets up to max value if set
                if (maxAssets.HasValue && index == maxAssets.Value)
                    break;

                var vAsset = new VODAsset();

                vAsset.AssetId = int.Parse(dr.GetString(0));
                vAsset.Title = dr.GetString(1);
                vAsset.PID = dr.GetString(2);
                vAsset.PAID = dr.GetString(3);
                vAsset.PosterSource = dr.IsDBNull(4) ? string.Empty : dr.GetString(4);
                vAsset.PosterDest = GetDestImagePath(vAsset.AssetId, destDir);

                index++;

                yield return vAsset;
            };
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
            IEnumerable<string> files = null;
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

                files = Directory.EnumerateFiles(srcPath, fileQuery, SearchOption.TopDirectoryOnly).AsParallel();

                if (files.Count() > 1 && files.Any(x => Path.GetExtension(x).ToLower().Equals(".jpg")))
                    file = files.Where(x => Path.GetExtension(x).ToLower().Equals(".jpg")).FirstOrDefault();
                else 
                    file = files.FirstOrDefault();
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
        private void ProcessAsset(NGVodPosterConfig config, string vhoName, string destDir, string connectionString, CancellationToken cancelToken)
        {
            Trace.TraceInformation("INFO({0}): Processing VOD Asset Posters...", vhoName.ToUpper());

            //Begin processing each VOD asset obtained from the database asyncronously
            ParallelOptions po = new ParallelOptions() { MaxDegreeOfParallelism = config.MaxThreads, CancellationToken = cancelToken };

            //Add to vod asset count to progress total
            Interlocked.Add(ref this.ngProgress.Total, GetVODAssets(connectionString, MaxImages, vhoName, config.SourceDir, destDir, cancelToken).Count());
            Interlocked.Add(ref this.ngProgress.TotalNoPoster, GetVODAssets(connectionString, MaxImages, vhoName, config.SourceDir, destDir, cancelToken)
                .Where(x => string.IsNullOrEmpty(x.PosterSource)).Count());
            
            try
            {
                using (var dataController = new NGVodPosterDataController(connectionString))
                {
                    dataController.BeginTransaction();
                    Parallel.ForEach<VODAsset>(GetVODAssets(config.Vhos[vhoName].IMGDb.CreateConnectionString(), MaxImages, vhoName, config.SourceDir, destDir, cancelToken)
                        .OrderByDescending(x => !string.IsNullOrEmpty(x.PosterSource)).ThenByDescending(x => x.AssetId), po, (va) =>
                        {
                            try
                            {
                                po.CancellationToken.ThrowIfCancellationRequested();

                                if (!va.PosterSource.Contains(config.SourceDir))
                                {
                                    va.PosterSource = Path.Combine(config.SourceDir, va.PosterSource);
                                }

                                Task insertTsk = null;
                                //Get poster source if it doesn't already exist, or if it doesn't contain the PID/PAID values of the asset
                                if (string.IsNullOrEmpty(va.PosterSource) || !File.Exists(va.PosterSource) || !va.PosterSource.ToLower().Contains(va.PID.ToLower()) || !va.PosterSource.ToLower().Contains(va.PAID.ToLower()))
                                {
                                    if (!string.IsNullOrEmpty(va.PosterSource))
                                        Interlocked.Increment(ref ngProgress.TotalNoPoster);

                                    try
                                    {
                                        va.PosterSource = GetSourceImagePath(va.PID, va.PAID, config.SourceDir);
                                    }
                                    catch (Exception ex)
                                    {
                                        //Increment progress failed value if error was thrown
                                        Interlocked.Increment(ref this.ngProgress.Failed);
                                        Trace.TraceError("Error getting source image path. {0}", ex.Message);
                                        return;
                                    }

                                    if (string.IsNullOrEmpty(va.PosterSource))
                                    {
                                        //If file exists on destination server but does not on the source server, then delete it on the destination.
                                        //This prevents incorrect posters from being displayed if the asset ID is changed by the VOD provider.
                                        if (File.Exists(va.PosterDest))
                                        {
#if DEBUG
                                            Trace.TraceInformation("Deleting {0} in {1} because there is no source that matches it", Path.GetFileName(va.PosterDest), vhoName);
#else
                                            File.Delete(va.PosterDest);
#endif
                                            Interlocked.Increment(ref this.ngProgress.Deleted);
                                        }

                                        Interlocked.Increment(ref this.ngProgress.Failed);
                                        return;
                                    }
                                    else
                                    {
                                        insertTsk = dataController.InsertAssetAsync(va, cancelToken);
                                    }
                                }

                                //Skip if destination file is newer than the source file
                                if (File.Exists(va.PosterDest)
                                    && (File.GetLastWriteTime(va.PosterDest).CompareTo(File.GetLastWriteTime(va.PosterSource)) >= 0
                                    && File.GetCreationTime(va.PosterDest).CompareTo(File.GetCreationTime(va.PosterSource)) >= 0))
                                {
                                    Interlocked.Increment(ref ngProgress.Skipped);
                                    return;
                                }

                                try
                                {
                                    //Resize and save the image to the destination
                                    var res = ProcessImage(va, config.ImgHeight, config.ImgWidth, vhoName, po.CancellationToken);

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
                                    try
                                    {
                                        sendToBadPosterDir(va.PosterSource);
                                    }
                                    catch (Exception ex2)
                                    {
                                        Trace.TraceError("Failed to send to bad poster directory in source folder. {0}", ex2.Message);
                                    }

                                    //Retry to get the poster source after sending the bad one to the BadImage directory on the source server
                                    try
                                    {
                                        Trace.TraceInformation("Attempting to re-process image for {0} in {1}.", va.AssetId, vhoName);
                                        va.PosterSource = GetSourceImagePath(va.PID, va.PID, config.SourceDir);
                                        if (!string.IsNullOrEmpty(va.PosterSource))
                                        {
                                            var res2 = ProcessImage(va, config.ImgHeight, config.ImgWidth, vhoName, po.CancellationToken);
                                            if (res2 == 0)
                                            {
                                                Interlocked.Decrement(ref this.ngProgress.Failed);
                                                Interlocked.Increment(ref this.ngProgress.Success);
                                            }
                                            else if (null != insertTsk)
                                            {
                                                //If there is an existing insert task, and the image was not able to be processed
                                                //then we have to remove the newly created asset from the database
                                                insertTsk.Wait();
                                                insertTsk = dataController.DeleteVodAssetAsync(va, cancelToken);
                                            }
                                        }
                                    }
                                    catch (Exception ex3)
                                    {
                                        Trace.TraceError("Failed re-processing image for {0} in {1}. {2}", va.AssetId, vhoName, ex3.Message);
                                    }
                                }

                                //Wait on the async task (will not cause deadlock since it is a console application)
                                if (null != insertTsk)
                                {
                                    try
                                    {
                                        insertTsk.Wait(cancelToken);
                                    }
                                    catch (AggregateException aex)
                                    {
                                        foreach (var ex in aex.InnerExceptions)
                                        {
                                            if (ex is OperationCanceledException || ex is TaskCanceledException)
                                                continue;
                                            Trace.TraceError("Insert task failed in {0} for {1}. {2}", vhoName, va.ToString(), ex.Message);
                                        }
                                    }
                                    finally
                                    {
                                        insertTsk.Dispose();
                                    }
                                }

                                po.CancellationToken.ThrowIfCancellationRequested();
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                        }); //end parallel foreach statement
                    try
                    {
                        dataController.CommitTransaction();
                    }
                    catch
                    {
                        dataController.RollbackTransaction();
                    }
                }//end using dataConnection
            }
            catch (OperationCanceledException)
            {
                this.ngProgress.StopProgress = true;
                this.ngProgress.IsCanceled = true;
            }
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
                    throw new ArgumentNullException("Destination folder cannot be null. " + VAsset.ToString());
                }

                //Verify file extension is .jpg
                if (!VAsset.PosterDest.EndsWith(".jpg"))
                    throw new ArgumentException("(ProcessImages) Invalid destination file name.", VAsset.PosterDest);

                //Throw error if source file not found, used to populate missing poster log
                if (!File.Exists(VAsset.PosterSource))
                    throw new FileNotFoundException(string.Format("(ProcessImages) Source poster file not found. AssetID: {0}", VAsset.AssetId), VAsset.PosterSource);                

                //if destination file already exists, check if it needs updated using timestamp
                if (File.Exists(VAsset.PosterDest))
                {
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
                    FileInfo srcFInfo = new FileInfo(VAsset.PosterSource);
                    if (((srcFInfo.Length / 1024F) / 1024F) > 50)
                    {
                        string tmpName = Path.Combine(Path.GetFullPath(srcFInfo.FullName), Path.GetFileNameWithoutExtension(srcFInfo.FullName) + "_tmp.jpg");

                        using (var origBM = new Bitmap(srcFInfo.FullName))
                        using (var srcBM = new Bitmap(origBM, Width, Height))
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
                using (var destBM = new Bitmap(sourceBM, Width, Height))
                //using (var destBM = Toolset.ResizeBitmap(sourceBM, Width, Height, null, null, true))
                {
                    token.ThrowIfCancellationRequested();
                    destBM.Save(VAsset.PosterDest, ImageFormat.Jpeg);
                }


                //Verify file was saved and it exists
                if (!File.Exists(VAsset.PosterDest))
                    throw new Exception("(ProcessImages) File failed to save in destination folder.");

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
        /// Performs a cleanup of the source directory by removing any posters that are not assigned to any active
        /// assets across all VHO's. Only perform this operation if all processes have performed successfully.
        /// </summary>
        /// <param name="usedSourceFiles">All active assets in all vho's.</param>
        /// <param name="srcPath">Full path to the source directory.</param>
        internal void CleanupSource(ref NGVodPosterConfig config, CancellationToken cancelToken)
        {
            Trace.TraceInformation("Starting cleanup source...");
            //Get all unused assets.
            //var unusedSrcFiles = Directory.EnumerateFiles(srcPath).Except(usedSourceFiles, new SourceFileComparer());
            var unusedSrcFiles = Directory.EnumerateFiles(config.SourceDir)
                .Except(NGVodPosterDataController.GetAllPosterSourceMaps(config, cancelToken).Select(x => x.Item2), new SourceFileComparer());

            Trace.TraceInformation("INFO: Archiving unused posters. Count => {0}", unusedSrcFiles.Count());
#if DEBUG
            var allAssets = GetAllVodAssets(config, cancelToken);
#endif

            var archiveDir = Path.Combine(config.SourceDir, "Archive");

            //create an archive directory to store the newly cleaned up files
            if (!Directory.Exists(archiveDir))
                Directory.CreateDirectory(archiveDir);          
            
            Parallel.ForEach(unusedSrcFiles, (delFile) =>
                {
                    this.token.ThrowIfCancellationRequested();

#if DEBUG
                    if (checkIfExists(delFile, ref allAssets))
                    {
                        Trace.TraceInformation("FILE MATCHES ASSET! {0}", delFile);
                    }
#endif

                    //if the file exists and the filer is older than the amount of surpassed running days, copy to the archive and delete it
                    if (File.Exists(delFile) && File.GetLastWriteTime(delFile).Date < DateTime.Now.Date.AddDays(-ngProgress.Time.Elapsed.Days))
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

        private bool checkIfExists(string srcFile, ref IEnumerable<VODAsset>allAssets)
        {

            string[] strPrts = Path.GetFileNameWithoutExtension(srcFile).Split('_');

            string pid = strPrts[0].ToUpper() == "IMG" ? strPrts[1] : strPrts[0];
            string paid = strPrts[0].ToUpper() == "IMG" ? strPrts[2] : strPrts[1];

            return allAssets.Any(x => x.PID.ToLower().Equals(pid.ToLower()) && x.PAID.ToLower().Equals(paid.ToLower())) && Path.GetExtension(srcFile).ToLower().Equals(".jpg");
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

            ParallelOptions po = new ParallelOptions() { MaxDegreeOfParallelism = Math.Max(System.Environment.ProcessorCount / 2, 1) };

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

#if DEBUG
            Trace.TraceInformation("Getting files to delete from {0}", DestDir);
#endif
            //Get files to delete
            var delFiles = Directory.EnumerateFiles(DestDir).Except(usedFiles, new SourceFileComparer());

#if DEBUG
            Trace.TraceInformation("Complete! {0}", DestDir);
#endif

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

        private async Task cleanupDatabase(string connectionString, string vhoName, CancellationToken cancelToken)
        {
            Trace.TraceInformation("Cleaning up database in {0}", vhoName);
            using (var dataController = new NGVodPosterDataController(connectionString))
            {
                if (!cancelToken.IsCancellationRequested)
                {
                    try
                    {
                        await dataController.CleanupSourceMapTable(cancelToken);
                        Trace.TraceInformation("Successfully cleaned database in {0}", vhoName);
                    }
                    catch (AggregateException aex)
                    {
                        foreach (var ex in aex.InnerExceptions)
                        {
                            if (ex is OperationCanceledException || ex is TaskCanceledException)
                                throw ex;

                            Trace.TraceError("Failed to clean database in {0}. {1}", vhoName, ex.Message);
                        }
                    }
                }
            }
        }
        #endregion Cleanup Methods

        /// <summary>
        /// Timer handle to report progress
        /// </summary>
        /// <returns></returns>
        private Task HandleTimer(NgVodPosterProgress progress, IProgress<NgVodPosterProgress> iProgress)
        {
            iProgress.Report(progress);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Sends corrupt or invalid source files to the BadImages directory
        /// </summary>
        /// <param name="badPoster">Full path to the bad image file</param>
        private void sendToBadPosterDir(string badPoster)
        {
            if (!File.Exists(badPoster))
                return;

            string dirName = Path.GetDirectoryName(badPoster);
            dirName = Path.Combine(dirName, "BadImages");

            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            string newFile = Path.Combine(dirName, Path.GetFileName(badPoster));

            File.Copy(badPoster, newFile, true);
            File.Delete(badPoster);
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
