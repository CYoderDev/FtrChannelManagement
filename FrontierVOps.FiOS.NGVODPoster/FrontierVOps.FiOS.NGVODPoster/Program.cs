using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
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
                twtl.TraceOutputOptions = TraceOptions.DateTime;
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
                    Trace.TraceError("***ERROR GETTING CONFIG PARAMETERS***");
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
                    Trace.TraceError("***ERROR GETTING CONFIG PARAMETERS***");
                    Console.WriteLine("\t{0}", ex.Message);
                    Trace.TraceError("\t" + ex.Message);
                    Console.ResetColor();
                }

                //Do not continue if configuration was not found
                if (config == null)
                    Environment.Exit(500);

                string errorLog = Path.Combine(config.LogErrorDir, "NGVodPoster_Error.log");

                //Configure error log
                using (var twtlError = new TextWriterTraceListener(errorLog))
                {
                    twtlError.TraceOutputOptions = TraceOptions.DateTime;
                    twtlError.Filter = new EventTypeFilter(SourceLevels.Error);
                    Trace.Listeners.Add(twtlError);

                    //Handle manual override parameters from console
                    Trace.WriteLine("Override Params: ");
                    for (int i = 0; i < args.Length; i++)
                    {
                        try
                        {
                            if (args[i].ToUpper().Equals("-D"))
                            {
                                config.DestinationDir = args[i + 1];
                                Trace.WriteLine(string.Format("Destination: {0}", config.DestinationDir));
                                i++;
                            }
                            else if (args[i].ToUpper().Equals("-S"))
                            {
                                config.SourceDir = args[i + 1];
                                Trace.WriteLine(string.Format("Source: {0}", config.SourceDir));
                                i++;
                            }
                            else if (args[i].ToUpper().Equals("-N"))
                            {
                                maxImages = int.Parse(args[i + 1]);
                                Trace.WriteLine(string.Format("Max Images: {0}", maxImages));
                                i++;
                            }
                            else if (args[i].ToUpper().Equals("-T"))
                            {
                                config.MaxThreads = int.Parse(args[i + 1]);
                                Trace.WriteLine(string.Format("Max Threads: {0}", config.MaxThreads));
                                i++;
                            }
                            else if (args[i].ToUpper().Equals("-STO"))
                            {
                                try
                                {
                                    config.AddEmailTo(args[i + 1]);
                                    Trace.WriteLine(string.Format("Send Missing Poster Log To: {0}", args[i + 1]));
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("ERROR: Invalid Email Address Provided.");
                                    throw ex;
                                }
                                i++;
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
                        //Inline cancel key press handler
                        Console.CancelKeyPress += (sender, e) =>
                        {
                            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                            {
                                e.Cancel = true;

                                if (!tokenSource.IsCancellationRequested)
                                {
                                    tokenSource.Cancel();
                                    Console.ResetColor();
                                }
                            }
                        };

                        try
                        {
                            //Create a separate task for each VHO listed in the config file, and run asyncronously
                            var tskList = new List<Task>();                            

                            foreach (var vho in config.Vhos)
                            {
                                tskList.Add(ctrl.BeginProcess(vho.Key, maxImages, config, progress, token));
                            }
                            //List<Exception> exceptions = new List<Exception>();

                            //Used to determine if all vho's processed successfully for source directory cleanup
                            bool allSuccessful = true;

                            try
                            {
                                //Write a menu to the console describing the progress chart
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                Console.WriteLine("\nP: Progress | Thds: # of threads open | OK: Successful posters processed | ");
                                Console.WriteLine("F: Failed | Sk: Skipped | T: # of minutes elapsed | R: Remaining assets\n");
                                Console.ResetColor();

                                Task.WaitAll(tskList.ToArray());
                            }
                            catch (AggregateException aex)
                            {
                                Trace.WriteLine("\n\n****ERRORS****\n");
                                foreach (var ex in aex.Flatten().InnerExceptions)
                                {
                                    if (ex is TaskCanceledException || ex is OperationCanceledException || ex is ArgumentNullException)
                                        continue;
                                    else
                                        Trace.TraceError(ex.Message + "\n");
                                }

                                try
                                {
                                    //Creating missing poster logs
                                    WriteToMissPosterLog(aex.Flatten(), config.EmailTo, config.LogMissPosterDir);
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError("Failed to send missing poster log. - {0}", ex.Message);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                allSuccessful = false;
                            }

                            //If all tasks ran to completion, then begin cleaning up the source folder
                            if (allSuccessful && !maxImages.HasValue)
                            {
                                var cleanupSrcTsk = ctrl.CleanupSource(ctrl.AllVAssets, config);
                                cleanupSrcTsk.Wait();
                            }

                        }
                        catch (Exception ex)
                        {
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
        static void WriteToMissPosterLog(AggregateException aex, List<string> sendTo, string logDir)
        {
            if (string.IsNullOrEmpty(logDir))
                return;

            if (!Directory.Exists(logDir))
                throw new DirectoryNotFoundException(logDir + " not found");

            string posterLog = Path.Combine(logDir, "MissingPosters.log");

            //Get only exceptions that mention the source file/folder
            var exceptions = aex.InnerExceptions.Where(x => x.Message.ToUpper().Contains("SOURCE"));

            if (exceptions.Count() == 0)
                return;

            //Reset poster log
            if (File.Exists(posterLog))
                File.Delete(posterLog);
            
            //Write all lines to the log file
            File.WriteAllLines(posterLog, exceptions.Select(x => x.Message));

            var errorLogs = Directory.EnumerateFiles(logDir).Where(x => x.EndsWith("MissingPosters.log"));

            if (sendTo.Count > 0)
            {
                try
                {
                    //Setup email parameters and send email
                    string smtp = ConfigurationManager.AppSettings["SmtpServer"];
                    int port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
                    string body = "Frontier Ops NGVODPoster missing posters found. See attached log.";
                    string subject = "Missing VOD Posters";
                    Toolset.SendEmail(smtp, null, null, false, subject, body, "FrontierFiOSOps@ftr.com", sendTo.ToArray(), errorLogs.ToArray());
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to send error logs to all recipients. " + ex.Message, ex);
                }
            }
        }

        static void ReportProgress(NgVodPosterProgress value)
        {
            if (value.StopProgress)
                return;

            //Total all processed images and calculate the percentage
            int total = value.Success + value.Failed + value.Skipped + value.Deleted;
            decimal progPerc = (decimal)total / (decimal)value.Total;

            //if cancellation is requested, clear the console line and write that the task was canceled
            if (value.IsCanceled)
            {
                value.StopProgress = true;
                ClearCurrentConsoleLine();
                Console.Write("--------Task Canceled--------");
            }
            //If the progress is 100% and threads are 0, then the task is considered complete
            else if (Math.Ceiling((progPerc * 100)) == 100)
            {
                ClearCurrentConsoleLine();
                Console.Write("-----Task Complete----");
                Console.WriteLine(Environment.NewLine);
                value.StopProgress = true;
            }
            //If the progress is divisible by the provided value, then report progress
            else if (Math.Ceiling((progPerc * 100)) % 1 == 0)
            {
                //Write progress to the same console line
                Console.Write(string.Format("\rP: {0:P1} | OK: {1} | F: {2} | Sk: {3} | T: {4} | R: {5}   ",
                    progPerc, value.Success, value.Failed, value.Skipped, (int)value.Time.Elapsed.TotalMinutes + (value.Time.Elapsed.Seconds > 30 ? 1 : 0), value.Total - total));
                Trace.WriteLine(string.Format("P: {0:P1} | R: {1} | D: {2}", progPerc, value.Total - total, value.Deleted));
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
