using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
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
            
            string sourcePath = string.Empty;
            string destPath = string.Empty;
            int? maxImages = null;
            string dbName = string.Empty;
            string dbInstance = string.Empty;
            string userName = string.Empty;
            CancellationTokenSource cancelTokenSrc = new CancellationTokenSource();

            try
            {
                Console.ForegroundColor = ConsoleColor.White;

                //Read Config
                dbName = ConfigurationManager.AppSettings["DBName"];
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Unable to process app.config file app settings -- {0}", ex.Message);
                Environment.Exit(500);
            }
            
            for (int i = 0; i < args.Length; i++)
            {
                try
                {
                    if (args[i].ToUpper().Equals("-D"))
                    {
                        destPath = args[i + 1];
                        i++;
                    }
                    else if (args[i].ToUpper().Equals("-S"))
                    {
                        sourcePath = args[i + 1];
                        i++;
                    }
                    else if (args[i].ToUpper().Equals("-N"))
                    {
                        maxImages = int.Parse(args[i + 1]);
                        i++;
                    }
                    else if (args[i].ToUpper().Equals("-I"))
                    {
                        dbInstance = args[i + 1];
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

            try
            {
                var sqlDB = new SqlDb();
                sqlDB.DatabaseName = dbName;
                sqlDB.DataSource = dbInstance;
                string connectionStr = sqlDB.CreateConnectionString();
                var ctrl = new NGVodPosterController(sourcePath, destPath);
                ctrl.MaxImages = maxImages;

                var mainTsk = ctrl.BeginProcess(connectionStr, cancelTokenSrc.Token);

                try
                {
                    mainTsk.Wait();
                }
                catch (AggregateException aex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n\n****ERRORS****\n");
                    foreach (var ex in aex.Flatten().InnerExceptions)
                    {
                        if (ex is TaskCanceledException)
                            continue;
                        else
                            Console.WriteLine(ex.Message + "\n");
                    }
                    Console.ResetColor();
                }
                finally
                {
                    cancelTokenSrc.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Application Error -- {0}", ex.Message);
                Console.ResetColor();
            }
        }

        static void DisplayHelp()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n\n******HELP MENU*******\n");
            Console.WriteLine("-d = Image Final Destination Path");
            Console.WriteLine("-s = Image Source Path");
            Console.WriteLine("-n = Max number of images to process");
            Console.WriteLine("-i = Database Instance Name or IP");
            Console.WriteLine("? or -h = Help Menu");
            Console.ResetColor();
            System.Environment.Exit(0);
        }
    }
}
