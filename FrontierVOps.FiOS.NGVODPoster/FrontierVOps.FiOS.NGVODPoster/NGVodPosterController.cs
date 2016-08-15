﻿using System;
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
        /// Get whether processing is complete.
        /// </summary>
        internal bool IsComplete { get; private set; }

        /// <summary>
        /// Get whether processing has been canceled.
        /// </summary>
        internal bool IsCanceled { get; private set; }

        /// <summary>
        /// Get all VOD assets from all VHO's.
        /// </summary>
        internal ConcurrentQueue<VODAsset> AllVAssets { get; private set; }

        internal NgVodPosterProgress ngProgress { get; private set; }
        #endregion Internal Properties

        #region Private Properties
        /// <summary>
        /// Cancel token
        /// </summary>
        private CancellationToken token;

        /// <summary>
        /// Used to calculate the amount of time surpassed during processing
        /// </summary>
        private System.Timers.Timer timer;
        
        #endregion Private Properties

        /// <summary>
        /// Methods used to process VOD asset posters on a source server, and save them as resized
        /// posters on a destination server.
        /// </summary>
        /// <param name="token"></param>
        internal NGVodPosterController(CancellationToken token, IProgress<NgVodPosterProgress> progress)
        {
            //Set local property default values
            this.token = token;
            this.AllVAssets = new ConcurrentQueue<VODAsset>();
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
        /// <returns></returns>
        internal async Task BeginProcess(string vho, int? maxImages, NGVodPosterConfig config, IProgress<NgVodPosterProgress> progress, CancellationToken token)
        {
            //Thread safe exceptions queue
            var exceptions = new ConcurrentQueue<Exception>();

            if (this.token.IsCancellationRequested)
                this.token.ThrowIfCancellationRequested();

            Console.WriteLine("\n\n----Beginning {0}----\n", vho);
            Trace.WriteLine(vho);

            //Get the VHO values from the configuration
            NGVodVHO ngVho = new NGVodVHO();

            if (!config.Vhos.TryGetValue(vho, out ngVho))
            {
                throw new Exception("Unable to get VHO from config");
            }

            //Get the T-SQL connection string for the IMG front end database
            string connectionStr = ngVho.IMGDb.CreateConnectionString();

            //Get VOD assets for the VHO
            ngVho.ActiveAssets = await GetVODAssets(connectionStr, maxImages, vho, config.SourceDir);

            token.ThrowIfCancellationRequested();

            //Start run task to ensure it ran to completion before attempting cleanup
            var mainTsk = Task.Factory.StartNew(() => Run(ngVho.ActiveAssets, false, config, ngVho.PosterDir, ngVho.Name, progress, this.token));

            try
            {
                //Wait to finish
                await mainTsk;

                Complete();
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.InnerExceptions)
                    exceptions.Enqueue(aex.Flatten());
            }

            //Clean up poster directory based on the vho's active assets
            try
            {
                if (mainTsk.Status == TaskStatus.RanToCompletion && !maxImages.HasValue)
                {
                    await Task.Factory.StartNew(() => cleanupDestination(ngVho.ActiveAssets.Select(x => x.AssetId), ngVho.PosterDir, config.MaxThreads));
                }
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.InnerExceptions)
                    exceptions.Enqueue(aex.Flatten());
            }
            catch (Exception ex)
            {
                exceptions.Enqueue(ex);
            }
            

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions).Flatten();
        }
        
        /// <summary>
        /// Reports completion to trace output
        /// </summary>
        internal void Complete()
        {
            this.ngProgress.Time.Stop();
            if (this.ngProgress.IsComplete)
                Trace.WriteLine("\n\nImage Resize and Copy Complete!");
            else if (this.ngProgress.IsCanceled)
                Trace.WriteLine("\n\nImage Resize and Copy Canceled!");

            if (this.ngProgress.IsComplete || this.ngProgress.IsCanceled)
            {
                Trace.WriteLine(string.Format("Runtime: {0} hours, {1} minutes, {2} seconds", this.ngProgress.Time.Elapsed.Hours, this.ngProgress.Time.Elapsed.Minutes, this.ngProgress.Time.Elapsed.Seconds));
                Trace.WriteLine("\nResult:");
                Trace.WriteLine(string.Format("Successful: {0} ({1:P2})", this.ngProgress.Success, ((decimal)this.ngProgress.Success / (decimal)this.ngProgress.Total)));
                Trace.WriteLine(string.Format("Failed: {0} ({1:P2})", this.ngProgress.Failed, ((decimal)this.ngProgress.Failed / (decimal)this.ngProgress.Total)));
                Trace.WriteLine(string.Format("Skipped: {0} ({1:P2})", this.ngProgress.Skipped, ((decimal)this.ngProgress.Skipped / (decimal)this.ngProgress.Total)));
                Trace.WriteLine(string.Format("Total: {0}", this.ngProgress.Total));
            }
            this.IsComplete = !this.ngProgress.IsCanceled;
        }

        /// <summary>
        /// Cleans up the poster source directory based on all active assets from all 3 vho's
        /// </summary>
        /// <param name="AllVODAssets"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task CleanupSource(IEnumerable<VODAsset> AllVODAssets, NGVodPosterConfig config)
        {        
            this.token.ThrowIfCancellationRequested();

            await Task.Factory.StartNew(() => cleanupSource(AllVODAssets.Where(x => !(string.IsNullOrEmpty(x.PosterSource))).Select(x => x.PosterSource), config.SourceDir));
        }

        /// <summary>
        /// Get VOD assets from the database asyncronously
        /// </summary>
        /// <param name="ConnectionString">SQL connection string</param>
        /// <param name="maxAssets">Max number of assests to get</param>
        /// <returns></returns>
        internal async Task<IEnumerable<VODAsset>> GetVODAssets(string ConnectionString, int? maxAssets, string vhoName, string srcPath)
        {
            Trace.WriteLine(string.Format("INFO({0}): Getting VOD Asset Info from Database", vhoName));
            Console.WriteLine("({0}) Getting VOD Asset Info", vhoName);

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
                .GroupBy(x => new { x.AssetId, x.PID, x.PAID, x.Title })
                .Select(x => new VODAsset()
                {
                    AssetId = x.Key.AssetId,
                    Title = x.Key.Title,
                    PID = x.Key.PID,
                    PAID = x.Key.PAID,
                    Folders = x.Where(y => y.AssetId.Equals(x.Key.AssetId)).SelectMany(y => y.Folders).Distinct().ToList(),
                }).Distinct().ToList();

            var srcFiles = new List<string>(Directory.EnumerateFiles(srcPath).Select(x => Path.GetFileName(x)));

            vodAssets.ForEach(x => 
                {
                    this.AllVAssets.Enqueue(x);
                });
#if DEBUG
            //vodAssets.ForEach(x => Trace.WriteLine(string.Format("\nAssetId: {0}\nPID: {1}\nPaid: {2}\nTitle: {3}\nFolders: {4}", x.AssetId, x.PID, x.PAID, x.Title, string.Join(",", x.Folders.Select(y => y.Path)))));
#endif

            Console.WriteLine("\nINFO: Get VOD Assets Complete --> Count: {0}", vodAssets.Count);
            return vodAssets;
        }

        /// <summary>
        /// Tries to get the source file path by enumerating the source directory and matching the PID PAID value
        /// </summary>
        /// <param name="PID"></param>
        /// <param name="PAID"></param>
        /// <param name="srcPath"></param>
        /// <returns></returns>
        private Task<string> GetSourceImagePath(string PID, string PAID, string srcPath)
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

            return Task.FromResult<string>(file);
        }

        /// <summary>
        /// Try to get the source image file from the source directory by reading the index dictionary. If not found, then try
        /// enumerating the source directory for the file.
        /// </summary>
        /// <param name="srcDict">Dictionary containing asset ID to source file map</param>
        /// <param name="AssetId">Asset ID currently being processed</param>
        /// <param name="PID">PID value of the asset being processed</param>
        /// <param name="PAID">PAID value of the asset being processed</param>
        /// <param name="srcPath">Source directory where the raw image files are being stored</param>
        /// <returns></returns>
        private Task<string> GetSourceImagePath(IDictionary<int, string> srcDict,int AssetId, string PID, string PAID, string srcPath)
        {   
            string retVal = string.Empty;

            if (srcDict.TryGetValue(AssetId, out retVal))
            {
                if (File.Exists(retVal) && retVal.Contains(PID) && retVal.Contains(PAID))
                    return Task.FromResult<string>(retVal);
                else if (!retVal.Contains(PID) || !retVal.Contains(PAID))
                {
                    srcDict.Remove(AssetId);
                }
            }
            
            return GetSourceImagePath(PID, PAID, srcPath);
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

        #region Processing Methods
        /// <summary>
        /// Begins matching asset id's to source image files, and if found it will resize and save the file to the destination
        /// </summary>
        /// <param name="VAssets">List of the VHO's VOD assets</param>
        /// <param name="onlyNew">Process only new assets</param>
        /// <param name="config">The NGVodPosterConfig configuration</param>
        /// <param name="posterDest">The UNC path to the poster destination directory</param>
        /// <param name="vhoName">Name of the VHO that is being processed</param>
        /// <param name="cancelToken">Cancellation token</param>
        private void Run(IEnumerable<VODAsset> VAssets, bool onlyNew, NGVodPosterConfig config, string posterDest, string vhoName, IProgress<NgVodPosterProgress> iProgress, CancellationToken cancelToken)
        {
            Trace.WriteLine(string.Format("INFO({0}): Processing VOD Asset Posters...", vhoName.ToUpper()));
            Console.WriteLine("Processing VOD Asset Posters...{0}", vhoName.ToUpper());

            var exceptions = new ConcurrentQueue<Exception>();
            string indexFile = Path.Combine(Directory.GetCurrentDirectory(), vhoName + "_index.txt");

            //Create dictionary for indexing asset id to source image file
            IDictionary<int, string> dictSrcPath = new Dictionary<int, string>();

            Trace.WriteLine(string.Format("INFO({0}): Reading Indexes...", vhoName.ToUpper()));
            Console.WriteLine("Reading Indexes for {0}...This could take a few minutes", vhoName.ToUpper());

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
                        if ((File.Exists(objs[1]) || File.Exists(Path.Combine(config.SourceDir, objs[1])))
                            && !dictSrcPath.ContainsKey(id) && VAssets.Any(x => x.AssetId.Equals(id)))
                        {
                            dictSrcPath.Add(id, objs[1]);
                        }
                        else if (dictSrcPath.ContainsKey(id))
                        {
                            Trace.WriteLine(string.Format("WARNING({0}): Duplicate asset id in index. Id: {1}", vhoName.ToUpper(), id));
                        }
                    }
                }
            } 

            Console.ForegroundColor = ConsoleColor.Cyan;

            //Begin processing each VOD asset obtained from the database asyncronously
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cancelToken;
            po.MaxDegreeOfParallelism = config.MaxThreads;

            Interlocked.Add(ref this.ngProgress.Total, VAssets.Count());
            //this.ngProgress.Total += VAssets.Count();
            this.ngProgress.StopProgress = false;

            try
            {          
                Parallel.ForEach<VODAsset>(VAssets.OrderByDescending(x => x.PosterSource == null).ThenByDescending(x => x.AssetId), po, (va) =>
                    {
                        try
                        {
                            po.CancellationToken.ThrowIfCancellationRequested();

                            //Increment progress threads
                            //Interlocked.Increment(ref progThreads);

                            //If only new assets, then skip the asset if the destination file already exists
                            if (onlyNew)
                            {
                                va.PosterDest = GetDestImagePath(va.AssetId, posterDest);

                                if (File.Exists(va.PosterDest))
                                {
                                    //Interlocked.Increment(ref this.ProcessProgress.Skipped);
                                    Interlocked.Increment(ref this.ngProgress.Skipped);
                                    return;
                                }
                                else
                                    va.PosterSource = GetSourceImagePath(dictSrcPath, va.AssetId, va.PID, va.PAID, config.SourceDir).Result;
                            }
                            //Otherwise, match all assets to a source file asyncronously
                            else
                            {
                                var tskList = new List<Task<string>>() 
                                { 
                                    GetSourceImagePath(dictSrcPath, va.AssetId, va.PID, va.PAID, config.SourceDir), 
                                    Task.Factory.StartNew(() => GetDestImagePath(va.AssetId, posterDest)) 
                                };

                                try
                                {
                                    Task.WaitAll(tskList.ToArray());
                                }
                                catch (Exception ex)
                                {
                                    //Increment progress failed value if error was thrown
                                    Interlocked.Increment(ref this.ngProgress.Failed);
                                    throw ex;
                                }

                                //Set poster source and destination values based on the task result
                                va.PosterSource = tskList[0].Result;
                                va.PosterDest = tskList[1].Result;
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

                                throw new ArgumentNullException(string.Format("Poster source missing. \n\tTitle:  {0}\n\tAssetID: {1}\n\tPID: {2}\n\tPAID: {3}\n\tFolderPaths: {4}\n\tFolderIds: {5}",
                                    va.Title, va.AssetId, va.PID, va.PAID, string.Join(",", va.Folders.Select(x => x.Path)), string.Join(",", va.Folders.Select(x => x.ID))));
                            }
                            //Add poster source to dictionary if it is not null and doesn't already exist
                            else if (!dictSrcPath.ContainsKey(va.AssetId))
                                dictSrcPath.Add(va.AssetId, va.PosterSource);

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
                                throw ex;
                            }

                            po.CancellationToken.ThrowIfCancellationRequested();
                        }
                        catch (OperationCanceledException ex)
                        {                            
                            throw ex;
                        }
                        catch (ArgumentNullException ex)
                        {
                            exceptions.Enqueue(ex);
                            Interlocked.Increment(ref this.ngProgress.Failed);
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
                iProgress.Report(this.ngProgress);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n\nOperation Canceled, closing open threads and finishing...\n\n");
            }

            this.ngProgress.StopProgress = true;
            this.ngProgress.IsComplete = !this.ngProgress.IsCanceled;

            try
            {
                Trace.WriteLine("INFO: Writing Indexes");
                createIndex(dictSrcPath, indexFile);
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

            var exceptions = new ConcurrentQueue<Exception>();
         
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
                    exceptions.Enqueue(new Exception(string.Format("Failed to resize source image above 50mb - {0}", ex.Message)));
                }

                try
                {
                    //Resize poster, save, and increment progress success if no errors
                    using (var sourceBM = new Bitmap(VAsset.PosterSource))
                    using (var destBM = Toolset.ResizeBitmap(sourceBM, Width, Height, null, null, true))
                    {
                        token.ThrowIfCancellationRequested();
                        destBM.Save(VAsset.PosterDest, ImageFormat.Jpeg);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("(ProcessImages) An error occured while processing image for {0}.\n\tAssetId: {1}\n\tTitle: {2}\n\tFolder: {3} \n\t{4}", 
                        VAsset.PosterSource, VAsset.AssetId, VAsset.Title, string.Join(",", VAsset.Folders.Select(x => x.Path)), ex.Message), ex);
                }

                //Verify file was saved and it exists
                if (!File.Exists(VAsset.PosterDest))
                    throw new Exception("(ProcessImages) File failed to savee in destination folder.");

                return 0;
            }
            catch (Exception ex)
            {
                exceptions.Enqueue(ex);

                //Restore backup file from temp directory
                if (!string.IsNullOrEmpty(tmpFile) && File.Exists(tmpFile))
                    File.Copy(tmpFile, VAsset.PosterDest, true);
            }
            finally
            {
                //Delete backup file from temp directory
                if (!string.IsNullOrEmpty(tmpFile) && File.Exists(tmpFile))
                    File.Delete(tmpFile);
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
            else
                return 0;
        }
        #endregion Process Methods

        #region Cleanup Methods
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

            Trace.WriteLine(string.Format("INFO: Archiving unused posters. Count => {0}", unusedSrcFiles.Count()));
#if DEBUG
            Trace.WriteLine("Source Path: " + srcPath);
            Trace.WriteLine("Unused Source Files: ");
            Trace.WriteLine(string.Join(System.Environment.NewLine, unusedSrcFiles.Select(x => Path.GetFileName(x))));
#endif

            var exceptions = new ConcurrentQueue<Exception>();
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
                            Trace.WriteLine(string.Format("Copying {0} to {1} and deleting {0}", Path.GetFileName(delFile), Path.GetFileName(archFileName)));
#else
                            File.Copy(delFile, archFileName, true);
                            File.Delete(delFile);
#endif
                        }
                        catch (Exception ex)
                        {
                            exceptions.Enqueue(ex);
                            if (File.Exists(archFileName) && !File.Exists(delFile))
                            {
#if DEBUG
                                Trace.WriteLine(string.Format("Copying {0} back to {1} and overwriting", Path.GetFileName(archFileName), Path.GetFileName(delFile)));
#else
                                File.Copy(archFileName, delFile, true);
#endif
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
                                Trace.WriteLine(string.Format("Deleting {0}", fInfo.FullName));
#else
                                fInfo.Delete();
#endif
                            }
                            catch (Exception ex)
                            {
                                exceptions.Enqueue(ex);
                            }
                        }
                    }
                });

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions).Flatten();
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
            var exceptions = new ConcurrentQueue<Exception>();

            Trace.WriteLine(string.Format("Cleaning up destination folder. Count => {0}", usedAssetIds.Count()));

            ParallelOptions po = new ParallelOptions() { MaxDegreeOfParallelism = maxThreads };

            //Verify each file exists and add to usedFiles list if it does
            Parallel.ForEach<int>(usedAssetIds, po, (usedId) =>
                {
                    try
                    {
                        string fileName = Path.Combine(DestDir, string.Format("{0}.jpg"));

                        if (File.Exists(fileName))
                            usedFiles.Add(fileName);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                });

            //Get files to delete
            var delFiles = Directory.EnumerateFiles(DestDir).Except(usedFiles);

            Trace.WriteLine(string.Format("Number of files to remove from destination folder => {0}", delFiles.Count()));

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
                                File.Delete(delFile);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(ex);
                    }
                });

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions).Flatten();
        }
        #endregion Cleanup Methods

        /// <summary>
        /// Writes the asset id to source file map to the index file
        /// </summary>
        /// <param name="srcDict"></param>
        private void createIndex(IDictionary<int, string> srcDict, string indexFile)
        {
            if (!File.Exists(indexFile) || this.MaxImages.HasValue)
                return;
            
            List<string> contents = new List<string>();
            foreach (var entry in srcDict)
            {
                contents.Add(string.Join("|", entry.Key, entry.Value));
            }
            File.WriteAllLines(indexFile, contents);
        }

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
                this.AllVAssets = null;
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
