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
            string outputLog = Path.Combine(Directory.GetCurrentDirectory(), "Output.log");
            NGVodPosterConfig config = null;   
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            //Reset the output log
            try
            {
                if (File.Exists(outputLog))
                    File.Delete(outputLog);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to reset log. {0}", ex.Message);
            }

            //Create trace listener for output log
            using (TextWriterTraceListener twtl = new TextWriterTraceListener(outputLog))
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

                //Create the controller
                var ctrl = new NGVodPosterController(token);

                //Inline cancel key press handler
                Console.CancelKeyPress += (sender, e) =>
                {
                    if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                    {
                        e.Cancel = true;

                        if (!tokenSource.IsCancellationRequested)
                        {
                            tokenSource.Cancel();
                        }
                    }
                };

                try
                {
                    //Create a separate task for each VHO listed in the config file, and run asyncronously
                    var tskList = new List<Task>();
                    foreach (var vho in config.Vhos)
                    {
                        tskList.Add(ctrl.BeginProcess(vho.Key, maxImages, config, token));
                    }

                    try
                    {
                        //Wait for all tasks to complete
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
                        //Creating missing poster logs
                        WriteToMissPosterLog(aex.Flatten(), config.EmailTo, ConfigurationManager.AppSettings["ErrorLogDir"]);
                    }
                    catch (OperationCanceledException)
                    {

                    }

                    //If all tasks ran to completion, then begin cleaning up the source folder
                    if (tskList.All(x => x.Status == TaskStatus.RanToCompletion))
                    {
                        var cleanupSrcTsk = ctrl.CleanupSource(ctrl.AllVAssets, config);
                        cleanupSrcTsk.Wait();
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Trace.WriteLine("ERROR: Application Error -- {0}", ex.Message);
                    Console.ResetColor();
                }
                finally
                {
                    tokenSource.Dispose();
                }
            }
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
    }
}
