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
    internal class NGVodPosterController
    {
        internal string SourcePath { get; private set; }
        internal string DestPath { get; private set; }
        internal int? MaxImages { get; set; }
        internal int MaxThreads { get; set; }
        internal bool IsComplete { get; private set; }
        internal bool IsCanceled { get; private set; }
        internal int NumberOfServers { get; set; }

        private CancellationToken token;
        private Progress<int> progress;
        private int progTotal = 0;
        private int progSuccess = 0;
        private int progFailed = 0;
        private int progSkipped = 0;
        private int progThreads = 0;
        private Stopwatch progTimer;
        private string indexFile;
        bool stopProgress = true;

        internal NGVodPosterController(string sourcePath, string destPath, CancellationToken token)
        {
            this.token = token;
            this.SourcePath = sourcePath;
            this.DestPath = destPath;
            this.MaxThreads = System.Environment.ProcessorCount;
            indexFile = Path.Combine(Directory.GetCurrentDirectory(), "index.txt");
            this.progTimer = new Stopwatch();
            this.IsCanceled = false;
            this.IsComplete = false;

            progress = new Progress<int>();

            progress.ProgressChanged += (sender, val) =>
                {
                    if (stopProgress)
                        return;
                    
                    int total = progSuccess + progFailed + progSkipped;
                    decimal progPerc = (decimal)total / (decimal)progTotal;

                    if (token.IsCancellationRequested)
                    {
                        this.stopProgress = true;
                        ClearCurrentConsoleLine();
                        Console.Write("--------Task Canceled--------", progThreads);
                    }
                    else if (Math.Ceiling((progPerc * 100)) == 100 && progThreads == 0)
                    {
                        ClearCurrentConsoleLine();
                        Console.Write("-----Task Complete----");
                        Console.WriteLine(Environment.NewLine);
                        stopProgress = true;
                    }
                    else if (Math.Ceiling((progPerc * 100)) % val == 0)
                    {
                        Console.Write("\rP: {0:P1} | Thds: {1} | OK: {2} | F: {3} | Sk: {4} | T: {5} | R: {6}   " , 
                            progPerc, progThreads, progSuccess, progFailed, progSkipped, (int)progTimer.Elapsed.TotalMinutes + (progTimer.Elapsed.Seconds > 30 ? 1 : 0), progTotal - total);
                    }
                };
        }

        internal async Task BeginProcess(string ConnectionString)
        {
            validateParams();
            var exceptions = new ConcurrentQueue<Exception>();

            if (this.token.IsCancellationRequested)
                this.token.ThrowIfCancellationRequested();

            var vAssets = await GetVODAssets(ConnectionString, this.MaxImages);

            int width = int.Parse(ConfigurationManager.AppSettings["ImageWidth"]);
            int height = int.Parse(ConfigurationManager.AppSettings["ImageHeight"]);

            this.progTimer.Start();

            using (System.Timers.Timer timer = new System.Timers.Timer())
            {
                timer.Interval = 15000;
                timer.AutoReset = true;
                timer.Elapsed += async (sender, e) => await HandleTimer();
                timer.Enabled = true;
                timer.Start();
                //Start run task
                var mainTsk = Task.Factory.StartNew(() => Run(vAssets, this.token), this.token);

                try
                {
                    await mainTsk;
                    timer.Stop();
                    if (mainTsk.Status == TaskStatus.RanToCompletion)
                    {
                        if (!this.MaxImages.HasValue)
                        {
                            await Task.Factory.StartNew(() => cleanupSource(vAssets.Where(x => !string.IsNullOrEmpty(x.PosterSource)).Select(x => x.PosterSource)), this.token);
                            await Task.Factory.StartNew(() => cleanupDestination(vAssets.Select(x => x.AssetId), this.DestPath), this.token);
                        }
                    }
                }
                catch (AggregateException aex)
                {
                    foreach (var ex in aex.InnerExceptions)
                    {
                        exceptions.Enqueue(ex);
                    }
                }
            }
            if (exceptions.Count > 0)
                throw new AggregateException(exceptions).Flatten();
        }

        internal void Complete()
        {
            this.stopProgress = true;
            this.progTimer.Stop();
            Console.ForegroundColor = ConsoleColor.Green;
            if (this.IsComplete)
                Console.WriteLine("\n\nImage Resize and Copy Complete!");
            else if (this.IsCanceled)
                Console.WriteLine("\n\nImage Resize and Copy Canceled!");

            if (this.IsComplete || this.IsCanceled)
            {
                Console.WriteLine("Runtime: {0} hours, {1} minutes, {2} seconds", progTimer.Elapsed.Hours, progTimer.Elapsed.Minutes, progTimer.Elapsed.Seconds);
                Console.WriteLine("\nResult:");
                Console.WriteLine("Successful: {0} ({1:P2})", progSuccess, ((decimal)progSuccess / (decimal)progTotal));
                Console.WriteLine("Failed: {0} ({1:P2})", progFailed, ((decimal)progFailed / (decimal)progTotal));
                Console.WriteLine("Skipped: {0} ({1:P2})", progSkipped, ((decimal)progSkipped / (decimal)progTotal));
                Console.WriteLine("Total: {0}", progTotal);
                Console.ResetColor();
            }
            this.IsComplete = !this.IsCanceled;
            Console.ResetColor();
        }

        private async Task<IEnumerable<VODAsset>> GetVODAssets(string ConnectionString, int? maxAssets)
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
                        if (maxAssets.HasValue && vodAssets.Count == maxAssets.Value)
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

        private string GetSourceImagePath(string PID, string PAID, string srcPath)
        {
            validateParams();

            string file = string.Empty;

            string commonName = Path.Combine(srcPath, string.Format("IMG_{0}_{1}_{1}_POS.jpg", PID, PAID, PAID));

            if (File.Exists(commonName))
            {
                file = commonName;
            }
            else
            {
                string fileQuery = string.Format("*{0}_{1}*", PID, PAID);

                file = Directory.EnumerateFiles(this.SourcePath, fileQuery, SearchOption.TopDirectoryOnly).AsParallel()
                    .FirstOrDefault();
            }

            return file;
        }

        private Task<string> GetSourceImagePath(IDictionary<int, string> srcDict,int AssetId, string PID, string PAID, string srcPath)
        {
            if (!File.Exists(this.indexFile))
                return Task.FromResult<string>(GetSourceImagePath(PID, PAID, srcPath));

            
            string retVal = string.Empty;

            if (srcDict.TryGetValue(AssetId, out retVal))
            {
                if (File.Exists(retVal))
                    return Task.FromResult<string>(retVal);
            }
            
            return Task.FromResult<string>(GetSourceImagePath(PID, PAID, srcPath));
        }

        private string GetDestImagePath(int AssetId)
        {
            validateParams();

            return Path.Combine(this.DestPath, AssetId.ToString() + ".jpg");
        }

        private void Run(IEnumerable<VODAsset> VAssets, CancellationToken cancelToken)
        {
            Console.WriteLine("INFO: Processing VOD Asset Posters...");

            var exceptions = new ConcurrentQueue<Exception>();
            int width = int.Parse(ConfigurationManager.AppSettings["ImageWidth"]);
            int height = int.Parse(ConfigurationManager.AppSettings["ImageHeight"]);
            string indexFile = Path.Combine(Directory.GetCurrentDirectory(), "index.txt");
            var srcPath = this.SourcePath;   

            //Create index file
            if (!File.Exists(indexFile))
                File.Create(indexFile);

            //Reset progress variables and set the total
            resetProgress();
            progTotal = VAssets.Count();

            //Create dictionary for indexing asset id to source image file
            IDictionary<int, string> dictSrcPath = new Dictionary<int, string>();

            Console.WriteLine("INFO: Reading Indexes...");
            //Read index file and populate dictionary
            using (var fs = File.Open(this.indexFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    var objs = line.Split('|');
                    int id;
                    if (int.TryParse(objs[0], out id))
                    {
                        if ((File.Exists(objs[1]) || File.Exists(Path.Combine(srcPath, objs[1])))
                            && !dictSrcPath.ContainsKey(id))
                        {
                            dictSrcPath.Add(id, objs[1]);
                        }
                        else if (dictSrcPath.ContainsKey(id))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("WARNING: Duplicate asset id in index. Id: {0}", id);
                            Console.ResetColor();
                        }
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\nP: Progress | Thds: # of threads open | OK: Successful posters processed | ");
            Console.WriteLine("F: Failed | Sk: Skipped | T: # of minutes elapsed | R: Remaining assets\n");      

            Console.ForegroundColor = ConsoleColor.Cyan;
            this.stopProgress = false;
            
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cancelToken;
            po.MaxDegreeOfParallelism = this.MaxThreads;
            try
            {
                Parallel.ForEach<VODAsset>(VAssets, po, (va) =>
                    {
                        try
                        {
                            po.CancellationToken.ThrowIfCancellationRequested();

                            Interlocked.Increment(ref progThreads);
                            var tskList = new List<Task<string>>() 
                            { 
                                GetSourceImagePath(dictSrcPath, va.AssetId, va.PID, va.PAID, srcPath), 
                                Task.Factory.StartNew(() => GetDestImagePath(va.AssetId)) 
                            };

                            try
                            {
                                Task.WaitAll(tskList.ToArray());
                            }
                            catch (Exception ex)
                            {
                                Interlocked.Increment(ref progFailed);
                                throw ex;
                            }

                            va.PosterSource = tskList[0].Result;

                            if (string.IsNullOrEmpty(va.PosterSource))
                            {
                                throw new ArgumentNullException(string.Format("Poster source missing. \n\tTitle:  {0}\n\tAssetID: {1}\n\tPID: {2}\n\tPAID: {3}\n\tFolderPath: {4}\n\tFolderId: {5}",
                                    va.Title, va.AssetId, va.PID, va.PAID, va.FolderPath, va.FolderId));
                            }
                            //Add poster source to dictionary if it is not null and doesn't already exist
                            else if (!dictSrcPath.ContainsKey(va.AssetId))
                                dictSrcPath.Add(va.AssetId, va.PosterSource);
                            //if dictionary already contains key, and the new source path is different, then update the value
                            else if (dictSrcPath.ContainsKey(va.AssetId))
                            {
                                string srcVal = string.Empty;
                                if (dictSrcPath.TryGetValue(va.AssetId, out srcVal) && srcVal != va.PosterSource)
                                {
                                    dictSrcPath[va.AssetId] = va.PosterSource;
                                }
                            }

                            va.PosterDest = tskList[1].Result;
                            ProcessImage(va, height, width, po.CancellationToken);

                            po.CancellationToken.ThrowIfCancellationRequested();
                        }
                        catch (OperationCanceledException ex)
                        {
                            throw ex;
                        }
                        catch (ArgumentNullException ex)
                        {
                            exceptions.Enqueue(ex);
                            Interlocked.Increment(ref progFailed);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Enqueue(ex);
                        }
                        finally
                        {
                            Interlocked.Decrement(ref progThreads);
                        }
                    });
            }
            catch (OperationCanceledException)
            {
                this.stopProgress = true;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n\nOperation Canceled, closing open threads and finishing...\n\n");
                this.IsCanceled = true;
            }
            this.stopProgress = true;
            
            try
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("INFO: Writing Indexes");
                createIndex(dictSrcPath);
            }
            catch (Exception ex)
            {
                exceptions.Enqueue(new Exception("Failed to write to index file. " + ex.Message, ex));
            }
            
            
            Thread.Sleep(100);
            
            Complete();

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions).Flatten();
        }

        private void ProcessImage(VODAsset VAsset, int Height, int Width, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            string tmpPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp");

            if (!Directory.Exists(tmpPath))
                Directory.CreateDirectory(tmpPath);

            var exceptions = new ConcurrentQueue<Exception>();

            string tmpFile = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(VAsset.PosterDest))
                {
                    throw new ArgumentNullException(string.Format("Poster destination null. \n\tTitle:  {0}\n\tAssetID: {1}\n\tPID: {2}\n\tPAID: {3}\n\tFolderPath: {4}\n\tFolderId: {5}",
                        VAsset.Title, VAsset.AssetId, VAsset.PID, VAsset.PAID, VAsset.FolderPath, VAsset.FolderId));
                }

                //if destination file already exists, check if it needs updated
                if (File.Exists(VAsset.PosterDest))
                {
                    FileInfo destFInfo = new FileInfo(VAsset.PosterDest);
                    FileInfo srcFInfo = new FileInfo(VAsset.PosterSource);

                    //Skip if file is newer
                    if (destFInfo.LastWriteTime.CompareTo(srcFInfo.LastWriteTime) >= 0)
                    {
                        Interlocked.Increment(ref progSkipped);
                        return;
                    }

                    tmpFile = Path.Combine(tmpPath, Path.GetFileName(VAsset.PosterDest));

                    if (File.Exists(tmpFile))
                        File.Delete(tmpFile);

                    File.Move(VAsset.PosterDest, tmpFile);
                    File.SetAttributes(tmpFile, FileAttributes.Temporary);
                }

                if (!VAsset.PosterDest.EndsWith(".jpg"))
                    throw new ArgumentException("(ProcessImages) Invalid destination file name.", VAsset.PosterDest);

                //Throw error if source file not found
                if (!File.Exists(VAsset.PosterSource))
                    throw new FileNotFoundException(string.Format("(ProcessImages) Source poster file not found. AssetID: {0}", VAsset.AssetId), VAsset.PosterSource);

                try
                {
                    using (var sourceBM = new Bitmap(VAsset.PosterSource))
                    using (var destBM = Toolset.ResizeBitmap(sourceBM, 160, 229, null, null, true))
                    {
                        token.ThrowIfCancellationRequested();
                        destBM.Save(VAsset.PosterDest, ImageFormat.Jpeg);
                        Interlocked.Increment(ref progSuccess);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("(ProcessImages) An error occured while processing image for {0}.\n\tAssetId: {1}\n\tTitle: {2}\n\tFolder{3} -- {4}", 
                        VAsset.PosterSource, VAsset.AssetId, VAsset.Title, VAsset.FolderPath, ex.Message), ex);
                }

                if (!File.Exists(VAsset.PosterDest))
                    throw new Exception("(ProcessImages) File failed to savee in destination folder.");
            }
            catch (Exception ex)
            {
                //Increment skipped if cancellation was requested, failed if otherwise
                if (token.IsCancellationRequested)
                    Interlocked.Increment(ref progSkipped);
                else
                    Interlocked.Increment(ref progFailed);

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
        }

        private void cleanupSource(IEnumerable<string> usedSourceFiles)
        {
            var unusedSrcFiles = Directory.EnumerateFiles(this.SourcePath).Except(usedSourceFiles);
            var exceptions = new ConcurrentQueue<Exception>();
            var archiveDir = Path.Combine(this.SourcePath, "Archive");

            if (!Directory.Exists(archiveDir))
                Directory.CreateDirectory(archiveDir);

            Console.WriteLine("Archiving unused posters...");
            Parallel.ForEach(unusedSrcFiles, (delFile) =>
                {
                    if (File.Exists(delFile))
                    {
                        var archFileName = Path.Combine(archiveDir, Path.GetFileName(delFile));
                        try
                        {
                            File.Copy(delFile, archFileName, true);
                            File.Delete(delFile);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Enqueue(ex);
                            if (File.Exists(archFileName) && !File.Exists(delFile))
                            {
                                File.Copy(archFileName, delFile, true);
                            }
                        }
                    }
                });

            Console.WriteLine("Cleaning old posters from archive directory...");
            Parallel.ForEach(Directory.EnumerateFiles(archiveDir), (archiveFile) =>
                {
                    if (File.Exists(archiveFile))
                    {
                        FileInfo fInfo = new FileInfo(archiveFile);

                        if (fInfo.LastWriteTime.CompareTo(DateTime.Now.AddDays(90)) <= 0)
                        {
                            try
                            {
                                fInfo.Delete();
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

        private void cleanupDestination(IEnumerable<int>usedAssetIds, string DestDir)
        {
            List<string> usedFiles = new List<string>();
            var exceptions = new ConcurrentQueue<Exception>();

            ParallelOptions po = new ParallelOptions() { MaxDegreeOfParallelism = this.MaxThreads };
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

            var delFiles = Directory.EnumerateFiles(DestDir).Except(usedFiles);

            Parallel.ForEach<string>(delFiles, po, (delFile) =>
                {
                    try
                    {
                        if (File.Exists(delFile))
                        {
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

        private void createIndex(IDictionary<int, string> srcDict)
        {
            if (!File.Exists(indexFile))
                return;
            
            List<string> contents = new List<string>();
            foreach (var entry in srcDict)
            {
                contents.Add(string.Join("|", entry.Key, entry.Value));
            }
            File.WriteAllLines(indexFile, contents);
        }

        private void createIndex(IEnumerable<VODAsset> VAssets)
        {
            if (File.Exists(indexFile))
                File.Delete(indexFile);

            File.Create(indexFile);

            using (StreamWriter sw = File.AppendText(indexFile))
            {
                foreach (var va in VAssets.Where(x => !string.IsNullOrEmpty(x.PosterSource)).OrderBy(x => x.AssetId))
                {
                    string posterSrc = string.Empty;

                    try
                    {
                        posterSrc = Path.GetFileName(va.PosterSource);
                    }
                    catch (ArgumentException)
                    {
                        posterSrc = va.PosterSource;
                    }

                    sw.WriteLine(string.Join("|", va.AssetId, posterSrc));
                }
            }
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
            this.stopProgress = false;
        }

        private void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth)); 
            Console.SetCursorPosition(0, currentLineCursor);
        }

        private Task HandleTimer()
        {
            ((IProgress<int>)progress).Report(1);
            return Task.FromResult(0);
        }
    }
}
