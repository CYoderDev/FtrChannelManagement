using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
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
    internal class NGVodPosterController
    {
        internal string SourcePath { get; private set; }
        internal string DestPath { get; private set; }
        internal int? MaxImages { get; set; }

        private Progress<int> progress;
        private int progTotal = 0;
        private int progSuccess = 0;
        private int progFailed = 0;
        private int progSkipped = 0;
        private int progThreads = 0;

        internal NGVodPosterController(string sourcePath, string destPath)
        {
            this.SourcePath = sourcePath;
            this.DestPath = destPath;

            progress = new Progress<int>();

            progress.ProgressChanged += (sender, val) =>
                {
                    if (progTotal == 0)
                        return;

                    int total = progSuccess + progFailed + progSkipped;
                    decimal progPerc = (decimal)total / (decimal)progTotal;

                    if (Math.Ceiling((progPerc * 100)) == 100)
                    {
                        ClearCurrentConsoleLine();
                        Console.Write("-----Task Complete----");
                        Console.WriteLine(Environment.NewLine);
                        Thread.Sleep(100);
                        progTotal = 0;
                    }
                    else if (Math.Ceiling((progPerc * 100)) % val == 0)
                    {
                        Console.Write("\rINFO: Progress: {0:P0}", progPerc);
                        Console.Write("\t|\t # of active threads: {0}", progThreads);
                    }
                };
        }

        internal async Task BeginProcess(string ConnectionString, CancellationToken cancelToken)
        {
            validateParams();
            var exceptions = new ConcurrentQueue<Exception>();

            if (cancelToken.IsCancellationRequested)
                cancelToken.ThrowIfCancellationRequested();

            var vAssets = await GetVODAssets(ConnectionString);

            int width = int.Parse(ConfigurationManager.AppSettings["ImageWidth"]);
            int height = int.Parse(ConfigurationManager.AppSettings["ImageHeight"]);

            try
            {
                SetImagePaths(ref vAssets, cancelToken);
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.InnerExceptions)
                {
                    exceptions.Enqueue(ex);
                }
            }

            try
            {
                ProcessImages(vAssets, height, width, cancelToken);
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.InnerExceptions)
                {
                    exceptions.Enqueue(ex);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions).Flatten();
        }

        private async Task<IEnumerable<VODAsset>> GetVODAssets(string ConnectionString)
        {
            Console.WriteLine("INFO: Getting VOD Asset Info from Database");
            validateParams();

            string sproc = "sp_FUI_GetAllVODFolderAssetInfo";
            List<VODAsset> vodAssets = new List<VODAsset>();
            await DBFactory.SQL_ExecuteReaderAsync(ConnectionString, sproc, System.Data.CommandType.StoredProcedure, null, dr =>
                {
                    while (dr.Read())
                    {
                        //only add assets up to max value if set
                        if (this.MaxImages.HasValue && vodAssets.Count == this.MaxImages.Value)
                            break;
                        try
                        {
                            var vAsset = new VODAsset();

                            vAsset.FolderId = int.Parse(dr.GetString(0));
                            vAsset.ParentFolderId = int.Parse(dr.GetString(1));
                            vAsset.FolderPath = dr.GetString(2);
                            vAsset.FolderTitle = dr.GetString(3);
                            vAsset.AssetId = int.Parse(dr.GetString(4));
                            vAsset.Title = dr.GetString(5);
                            vAsset.PID = dr.GetString(6);
                            vAsset.PAID = dr.GetString(7);

                            vodAssets.Add(vAsset);
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("ERROR: (GetVODAssets - SQL Data Read) --> {0}", ex.Message);
                            Console.ResetColor();
                        }
                    }
                });

            Console.WriteLine("\nINFO: Get VOD Assets Complete --> Count: {0}", vodAssets.Count);
            return vodAssets;
        }

        private string GetSourceImagePath(string PID, string PAID, string destPath)
        {
            validateParams();

            string file = string.Empty;

            string commonName = Path.Combine(destPath, string.Format("IMG_{0}_{1}_{1}_POS.jpg", PID, PAID, PAID));

            if (File.Exists(commonName))
            {
                file = commonName;
            }
            else
            {
                string fileQuery = string.Format("*{0}_{1}*", PID, PAID);

                file = Directory.EnumerateFiles(this.SourcePath, fileQuery, SearchOption.TopDirectoryOnly).FirstOrDefault();
            }

            return file;
        }

        private string GetDestImagePath(int AssetId)
        {
            validateParams();

            return Path.Combine(this.DestPath, AssetId.ToString() + ".jpg");
        }

        private void SetImagePaths(ref IEnumerable<VODAsset> VAssets, CancellationToken cancelToken)
        {
            Console.WriteLine("INFO: Setting image paths...");
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cancelToken;

            var exceptions = new ConcurrentQueue<Exception>();

            resetProgress();
            progTotal = VAssets.Count();

            var destPath = this.DestPath;

            Console.ForegroundColor = ConsoleColor.Cyan;
            ((IProgress<int>)progress).Report(1);
            Parallel.ForEach<VODAsset>(VAssets, po, (va) =>
                {
                    try
                    {
                        Interlocked.Increment(ref progThreads);
                        va.PosterSource = GetSourceImagePath(va.PID, va.PAID, destPath);
                        va.PosterDest = GetDestImagePath(va.AssetId);
                        Interlocked.Increment(ref progSuccess);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref progFailed);
                        exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        ((IProgress<int>)progress).Report(5);
                        Interlocked.Decrement(ref progThreads);
                    }
                });
            Thread.Sleep(100);
            Console.ForegroundColor = ConsoleColor.White;

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }

        private void ProcessImages(IEnumerable<VODAsset> VAssets, int Height, int Width, CancellationToken cancelToken)
        {
            Console.WriteLine("INFO: Resizing and saving images");

            resetProgress();
            progTotal = VAssets.Count();

            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 20;
            po.CancellationToken = cancelToken;

            string tmpPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp");

            if (!Directory.Exists(tmpPath))
                Directory.CreateDirectory(tmpPath);

            var exceptions = new ConcurrentQueue<Exception>();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Parallel.ForEach<VODAsset>(VAssets, po, (va) =>
                {
                    string tmpFile = string.Empty;
                    try
                    {
                        if (po.CancellationToken.IsCancellationRequested)
                            po.CancellationToken.ThrowIfCancellationRequested();

                        Interlocked.Increment(ref progThreads);

                        if (string.IsNullOrEmpty(va.PosterSource))
                        {
                            throw new ArgumentNullException("PosterSource", string.Format("(ProcessImages) Poster source cannot be null. \n\tTitle:  {0}\n\tAssetID: {1}\n\tPID: {2}\n\tPAID: {3}\n\tFolderPath: {4}\n\tFolderId: {5}",
                               va.Title, va.AssetId, va.PID, va.PAID, va.FolderPath, va.FolderId));
                        }

                        if (string.IsNullOrEmpty(va.PosterDest))
                        {
                            throw new ArgumentNullException("PosterDest", string.Format("(ProcessImages) Poster destination cannot be null. \n\tTitle:  {0}\n\tAssetID: {1}\n\tPID: {2}\n\tPAID: {3}\n\tFolderPath: {4}\n\tFolderId: {5}", 
                                va.Title, va.AssetId, va.PID, va.PAID, va.FolderPath, va.FolderId));
                        }
                        
                        //Skip if file already exists
                        if (File.Exists(va.PosterDest))
                        {
                            FileInfo destFInfo = new FileInfo(va.PosterDest);
                            FileInfo srcFInfo = new FileInfo(va.PosterSource);

                            //Skip if file is newer
                            if (destFInfo.LastWriteTime.CompareTo(srcFInfo.LastWriteTime) > 0)
                            {
                                Interlocked.Increment(ref progSkipped);
                                return;
                            }

                            tmpFile = Path.Combine(tmpPath, Path.GetFileName(va.PosterDest));

                            if (File.Exists(tmpFile))
                                File.Delete(tmpFile);

                            File.Move(va.PosterDest, tmpFile);
                            File.SetAttributes(tmpFile, FileAttributes.Temporary);
                        }

                        if (!va.PosterDest.EndsWith(".jpg"))
                            throw new ArgumentException("(ProcessImages) Invalid destination file name.", va.PosterDest);

                        //Throw error if source file not found
                        if (!File.Exists(va.PosterSource))
                            throw new FileNotFoundException(string.Format("(ProcessImages) Source poster file not found. AssetID: {0}", va.AssetId), va.PosterSource);

                        try
                        {
                            using (var sourceBM = new Bitmap(va.PosterSource))
                            using (var destBM = Toolset.ResizeBitmap(sourceBM, 160, 229, null, null, true))
                            {
                                //sourceBM.Dispose();
                                destBM.Save(va.PosterDest, ImageFormat.Jpeg);
                                Interlocked.Increment(ref progSuccess);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("(ProcessImages) An error occured while processing image for {0}.\n\tAssetId: {1}\n\tTitle: {2}\n\tFolder{3}", va.PosterSource, va.AssetId, va.Title, va.FolderPath), ex);
                        }

                        if (!File.Exists(va.PosterDest))
                            throw new Exception("(ProcessImages) File failed to savee in destination folder.");
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref progFailed);
                        exceptions.Enqueue(ex);
                        if (!string.IsNullOrEmpty(tmpFile) && File.Exists(tmpFile))
                            File.Copy(tmpFile, va.PosterDest, true);
                        return;
                    }
                    finally
                    {
                        ((IProgress<int>)progress).Report(1);
                        if (!string.IsNullOrEmpty(tmpFile) && File.Exists(tmpFile))
                            File.Delete(tmpFile);
                        Interlocked.Decrement(ref progThreads);
                    }
                });

            //Allow for task to finish and progress to report.
            Thread.Sleep(10);
            Console.ForegroundColor = ConsoleColor.Green;
            progTotal = VAssets.Count();
            Console.WriteLine("\n\nImage Resize and Copy Complete!");
            Console.WriteLine("\nResult:");
            Console.WriteLine("Successful: {0} ({1:P2})", progSuccess, ((decimal)progSuccess / (decimal)progTotal));
            Console.WriteLine("Failed: {0} ({1:P2})", progFailed, ((decimal)progFailed / (decimal)progTotal));
            Console.WriteLine("Skipped: {0} ({1:P2})", progSkipped, ((decimal)progSkipped / (decimal)progTotal));
            Console.WriteLine("Total: {0}", progTotal);
            Console.ResetColor();

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }

        private void validateParams()
        {
            if (string.IsNullOrEmpty(this.SourcePath))
                throw new ArgumentNullException("Source path cannot be null");

            if (string.IsNullOrEmpty(this.DestPath))
                throw new ArgumentNullException("Destination path cannot be null");

            if (!Directory.Exists(this.SourcePath))
                throw new DirectoryNotFoundException(string.Format("Directory {0} cannot be found", this.SourcePath));

            if (!Directory.Exists(this.DestPath))
                throw new DirectoryNotFoundException(string.Format("Directory {0} cannot be found", this.DestPath));
        }

        private void resetProgress()
        {
            this.progTotal = 0;
            this.progSuccess = 0;
            this.progFailed = 0;
            this.progSkipped = 0;
            this.progThreads = 0;
        }

        private void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth)); 
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
