﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mail;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FrontierVOps.Common;
using FrontierVOps.Data;
using FrontierVOps.Data.Objects;
using FrontierVOps.Security;

namespace FrontierVOps.FiOS.NGVODPoster
{
    class Program
    {
        static void Main(string[] args)
        {
            int? maxImages = null;
            string customAction = null;
            bool onlyNew = false;
            string debugLog = Path.Combine(Directory.GetCurrentDirectory(), "Debug.log");
            NGVodPosterConfig config = null;   
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            //Reset the output log
            try
            {
                if (File.Exists(debugLog))
                    File.Delete(debugLog);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to reset log. {0}", ex.Message);
            }

            //Create trace listener for output log
            using (TextWriterTraceListener twtl = new TextWriterTraceListener(debugLog))
            {             
                twtl.TraceOutputOptions = TraceOptions.None;
                twtl.Filter = new EventTypeFilter(SourceLevels.Information);
                twtl.Name = "TextWriteTraceListener";
             
                try
                {
                    Trace.Listeners.Clear();
                    Trace.Listeners.Add(twtl);
                    Trace.AutoFlush = true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Failed to set console output log writer. " + ex);
                }

                //Get configuration file
                try
                {
                    config = NGVodPosterConfig.GetConfig();
                }
                catch (AggregateException aex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("***ERROR GETTING CONFIG PARAMETERS***");
                    foreach (var ex in aex.Flatten().InnerExceptions)
                    {
                        Console.WriteLine("\t{0}", ex.Message);
                        Trace.TraceError("\t" + ex.Message);
                    }
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("***ERROR GETTING CONFIG PARAMETERS***");
                    Console.WriteLine("\t{0}", ex.Message);
                    Trace.TraceError("\t" + ex.Message);
                    Console.ResetColor();
                }

                //Do not continue if configuration was not found
                if (config == null)
                    Environment.Exit(500);

                string errorLog = Path.Combine(config.LogErrorDir, "NGVodPoster_Error.log");

                if (File.Exists(errorLog))
                    File.Delete(errorLog);

                //Configure error log
                using (var twtlError = new TextWriterTraceListener(errorLog))
                {
                    twtlError.TraceOutputOptions = TraceOptions.Timestamp;
                    twtlError.Filter = new EventTypeFilter(SourceLevels.Error);
                    Trace.Listeners.Add(twtlError);

                    //Handle manual override parameters from console
                    Trace.TraceInformation("Override Params: ");
                    for (int i = 0; i < args.Length; i++)
                    {
                        try
                        {
                            if (args[i].ToUpper().Equals("-D"))
                            {
                                config.DestinationDir = args[i + 1];
                                Trace.TraceInformation("Destination: {0}", config.DestinationDir);
                                i++;
                            }
                            else if (args[i].ToUpper().Equals("-S"))
                            {
                                config.SourceDir = args[i + 1];
                                Trace.TraceInformation("Source: {0}", config.SourceDir);
                                i++;
                            }
                            else if (args[i].ToUpper().Equals("-N"))
                            {
                                maxImages = int.Parse(args[i + 1]);
                                Trace.TraceInformation("Max Images: {0}", maxImages);
                                i++;
                            }
                            else if (args[i].ToUpper().Equals("-T"))
                            {
                                config.MaxThreads = int.Parse(args[i + 1]);
                                Trace.TraceInformation("Max Threads: {0}", config.MaxThreads);
                                i++;
                            }
                            else if (args[i].ToUpper().Equals("-STO"))
                            {
                                try
                                {
                                    config.AddEmailTo(args[i + 1]);
                                    Trace.TraceInformation("Send Missing Poster Log To: {0}", args[i + 1]);
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("ERROR: Invalid Email Address Provided.");
                                    Trace.TraceError("Invalid Email Address Provided.");
                                    Console.ResetColor();
                                    throw ex;
                                }
                                i++;
                            }
                            else if (args[i].ToUpper().Equals("-ONLYNEW"))
                            {
                                onlyNew = true;
                                Trace.TraceInformation("Process Only Deltas");
                            }
                            else if (args[i].ToUpper().Equals("-OD"))
                            {
                                customAction = "OD";
                                Trace.TraceInformation("Custom Action: {0}", customAction);
                            }
                            else if (args[i].ToUpper().Equals("-OM"))
                            {
                                customAction = "OM";
                                Trace.TraceInformation("Custom Action: {0}", customAction);
                            }
                            else if (args[i].Contains("?") || args[i].ToUpper().Equals("-H"))
                            {
                                DisplayHelp();
                                break;
                            }
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("ERROR: Invalid Parameters");
                            Console.ResetColor();
                            DisplayHelp();
                        }
                    }

                    //Split the max threads by number of vho's being processed to prevent overloading the CPU
                    config.MaxThreads = config.MaxThreads / config.Vhos.Count;

                    IProgress<NgVodPosterProgress> progress = new Progress<NgVodPosterProgress>(ReportProgress);

                    //Create the controller
                    using (var ctrl = new NGVodPosterController(token, progress))
                    {
                        if (!string.IsNullOrEmpty(customAction))
                            ctrl.customAction = customAction;

                        if (onlyNew)
                            ctrl.OnlyNew = true;

                        //Inline cancel key press handler
                        Console.CancelKeyPress += (sender, e) =>
                        {
                            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                            {
                                e.Cancel = true;

                                if (!tokenSource.IsCancellationRequested)
                                {
                                    tokenSource.Cancel();
                                    ctrl.ngProgress.IsCanceled = true;
                                    progress.Report(ctrl.ngProgress);
                                }
                            }
                        };

                        try
                        {
                            //Write a menu to the console describing the progress chart
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine("\nP: Progress | OK: Successful posters processed | F: Failed |");
                            Console.WriteLine("Sk: Skipped | T: # of minutes elapsed | R: Remaining assets | ETA\n");
                            Console.ResetColor();
                            
                            //Create a separate task for each VHO listed in the config file, and run asyncronously
                            var tskList = new List<Task>();                            

                            foreach (var vho in config.Vhos)
                            {
                                tskList.Add(ctrl.BeginProcess(vho.Key, maxImages, config));
                            }

                            //Used to determine if all vho's processed successfully for source directory cleanup
                            bool allSuccessful = true;

                            try
                            {
                                Task.WaitAll(tskList.ToArray());
                            }
                            catch (AggregateException aex)
                            {
                                foreach (var ex in aex.Flatten().InnerExceptions)
                                {
                                    if (ex is TaskCanceledException || ex is OperationCanceledException)
                                    {
                                        continue;
                                    }
                                    else
                                        Trace.TraceError(ex.Message + "\n");
                                }
                            }

                            //Create missing poster log
                            try
                            {
                                Console.WriteLine("\nCreating missing poster attachment and sending...");
                                WriteToMissPosterLog(ctrl.GetAllVodAssets(config).Where(x => string.IsNullOrEmpty(x.PosterSource)), config.EmailTo, config.LogMissPosterDir);
                                Console.WriteLine("Complete\n");
                            }
                            catch (Exception ex)
                            {
                                if (ex is OperationCanceledException || ex is TaskCanceledException)
                                {
                                    Console.WriteLine("Canceled!");
                                    throw;
                                }
                                Console.WriteLine("Failed! {0}\n", ex.Message);
                                Trace.TraceError("Failed to send missing poster log. {0}", ex.Message);
                            }

                            ctrl.Complete();

                            //If all tasks ran to completion, then begin cleaning up the source folder
                            if (allSuccessful && !maxImages.HasValue && !token.IsCancellationRequested && tskList.All(x => x.Status == TaskStatus.RanToCompletion))
                            {
                                Console.WriteLine("Cleaning up source directory...");
                                ctrl.CleanupSource(ref config, token);
                                Console.WriteLine("Complete\n");
                            }
                        }
                        catch (AggregateException aex)
                        {
                            foreach (var ex in aex.InnerExceptions)
                            {
                                if (ex is TaskCanceledException)
                                    continue;
                                Trace.TraceError(ex.Message);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is OperationCanceledException || ex is TaskCanceledException)
                                Trace.TraceInformation("Operation Canceled");
                            else
                                Trace.TraceError("Application Error -- {0}", ex.Message);
                        }
                        finally
                        {
                            tokenSource.Dispose();
                        }
                    } //end using ctrl
                }//end using twtlError
            }//end using twtl
        }

        /// <summary>
        /// Displays help menu in the console and exits
        /// </summary>
        static void DisplayHelp()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n\n******HELP MENU*******\n");
            Console.WriteLine("-d = Image Final Destination Path");
            Console.WriteLine("-s = Image Source Path");
            Console.WriteLine("-n = Max number of images to process");
            Console.WriteLine("-i = Database Instance Name or IP");
            Console.WriteLine("-t = Maximum processor threads to open");
            Console.WriteLine("-sto = Send error log to these addresses (comma delimited)");
            Console.WriteLine("? or -h = Help Menu");
            Console.ResetColor();
            System.Environment.Exit(0);
        }

        /// <summary>
        /// Writes the missing poster log to a text file, and sends it via email as an attachment
        /// </summary>
        /// <param name="aex">Aggregate Exception caught by the image process tasks</param>
        /// <param name="sendTo">Email addresses to send the email to</param>
        /// <param name="logDir">The directory where the log should be written</param>
        static void WriteToMissPosterLog(IEnumerable<VODAsset> vassets, List<string> sendTo, string logDir)
        {
            if (string.IsNullOrEmpty(logDir))
                return;

            if (!Directory.Exists(logDir))
                throw new DirectoryNotFoundException(logDir + " not found");

            FileInfo posterLog = new FileInfo(Path.Combine(logDir, "MissingPosters.log"));
            
            //Reset poster log
            if (posterLog.Exists)
                posterLog.Delete();
            
            //Write all lines to the log file
            File.WriteAllLines(posterLog.FullName, vassets.Select(x => x.ToString()));

            posterLog = new FileInfo(Path.Combine(logDir, "MissingPosters.log"));

            if (((posterLog.Length / 1024F) / 1024F) > 10)
            {
                using (var zip = ZipFile.Open(posterLog.Name + ".zip", ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(posterLog.FullName, posterLog.Name);
                }
            }

            var errorLogs = Directory.EnumerateFiles(logDir).Where(x => x.Contains("MissingPosters.log.zip"));
            
            if (errorLogs.Count() <= 0)
                errorLogs = Directory.EnumerateFiles(logDir).Where(x => x.EndsWith("MissingPosters.log"));

            if (sendTo.Count > 0)
            {
                try
                {
                    //Setup email parameters and send email
                    StringBuilder body = new StringBuilder();
                    string smtp = ConfigurationManager.AppSettings["SmtpServer"];
                    int port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
                    body.AppendLine("Frontier Ops NGVODPoster missing posters found. See attached log.");
                    body.AppendFormat("{0}{0}Total Assets with Missing Posters: {1}{0}{0}", System.Environment.NewLine, vassets.Count());
                    body.AppendFormat("Provider Id's{0}{0}", System.Environment.NewLine);
                    body.AppendFormat("PID | # of missing posters");
                    vassets.GroupBy(x => x.PID ).Select(y => new { y.Key, count = y.Key.Count() }).OrderByDescending(x => x.count)
                        .ToList().ForEach(x => body.AppendFormat("{0}{1} | {2}", System.Environment.NewLine, x.Key, x.count));
                    string subject = "Missing VOD Posters";
                    Toolset.SendEmail(smtp, null, null, false, false, subject, body.ToString(), "FrontierFiOSOps@ftr.com", sendTo.ToArray(), errorLogs.ToArray());
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to send error logs to all recipients. " + ex.Message, ex);
                }
            }

            errorLogs.Where(x => x.EndsWith(".zip")).ToList().ForEach(x => File.Delete(x));
        }

        static void ReportProgress(NgVodPosterProgress value)
        {
            if (value.StopProgress)
                return;

            //Total all processed images and calculate the percentage
            int total = value.Success + value.Failed + value.Skipped;
            decimal progPerc = (decimal)total / (decimal)value.Total;

            //if cancellation is requested, clear the console line and write that the task was canceled
            if (value.IsCanceled)
            {
                value.StopProgress = true;
                Console.Write("\n--------Task Canceled--------\n");
                Console.WriteLine("Please wait while the application closes all running processes.");
            }
            //If the progress is 100%, then the task is considered complete
            else if (total == value.Total)
            {
                ClearCurrentConsoleLine();
                Console.Write("-----Task Complete----");
                Console.WriteLine("\n OK: {0} | F:{1} | Sk: {2} | T: {3}", value.Success, value.Failed, value.Skipped, (int)value.Time.Elapsed.TotalMinutes + (value.Time.Elapsed.Seconds > 30 ? 1 : 0));
                Console.WriteLine(Environment.NewLine);
                value.StopProgress = true;
            }
            //If the progress is divisible by the provided value, then report progress
            else if (Math.Ceiling((progPerc * 100)) % 1 == 0)
            {
                double minRemaining = (value.Time.Elapsed.TotalMinutes / (total - value.Skipped)) * (value.Total - total);

                string rem = minRemaining > 60 ? minRemaining > 1440 ? (minRemaining / 60 / 24).ToString("N1") + " days" : (minRemaining / 60).ToString("N1") + " hrs" :
                    minRemaining < 1 ? (minRemaining * 60).ToString("N0") + "s" : minRemaining.ToString("N0") + " min";

#if DEBUG
                Trace.TraceInformation("MinRem: {0} | ElapsedMin: {1} | Total: {2} | Skipped: {3} | Val Total: {4}   ", rem, (int)value.Time.Elapsed.TotalMinutes, total, value.Skipped, value.Total);
#endif

                //Write progress to the same console line
                Console.Write(string.Format("\rP: {0:P1} | OK: {1} | F: {2} | Sk: {3} | T: {4} | R: {5} | {6}   ",
                    progPerc, value.Success, value.Failed, value.Skipped, (int)value.Time.Elapsed.TotalMinutes + (value.Time.Elapsed.Seconds > 30 ? 1 : 0), value.Total - total, rem));

                //Report to trace every 5 minutes
                if ((int)value.Time.Elapsed.TotalMinutes % 5 == 0 && value.Time.Elapsed.Seconds <= 15)
                    Trace.TraceInformation("P: {0:P1} | R: {1} | {2}", progPerc, value.Total - total, rem);
            }
        }

        /// <summary>
        /// Clears a single console line
        /// </summary>
        private static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
