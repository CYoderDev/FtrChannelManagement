using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using FrontierVOps.Common.WinForms;
using FrontierVOps.FiOS.HealthCheck.Controllers;
using FrontierVOps.FiOS.HealthCheck.DataObjects;
using FrontierVOps.FiOS.Servers.Controllers;
using FrontierVOps.FiOS.Servers.Objects;

namespace HealthCheck
{
    public partial class mainForm : Form
    {
        BackgroundWorker bw { get; set; }
        private readonly object _lockObj = new object();

        public mainForm()
        {
            try
            {
                IConfigurationSource configSource = ConfigurationSourceFactory.Create();
                LogWriterFactory lwFactory = new LogWriterFactory(configSource);
                Logger.SetLogWriter(lwFactory.Create(), false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Failed to set logger. {0}", ex.Message));
            }
            InitializeComponent();

            //Initialize background worker
            this.bw = new BackgroundWorker();
            this.bw.WorkerSupportsCancellation = true;
            this.bw.WorkerReportsProgress = true;
            this.bw.DoWork += this.bw_DoWork;
            this.bw.ProgressChanged += this.bw_ProgressChanged;
            this.bw.RunWorkerCompleted += this.bw_Complete;

            //Create menu strip gradient fill
            this.menuStrip_Main.Renderer = new TStripRenderer(new ProColorsTable() { MStripGradientBegin = Color.LightSkyBlue, MStripGradientEnd = Color.DarkSlateBlue }) { RoundedEdges = false };

            //Set objectlistview header style and hot item style
            var hfs = new BrightIdeasSoftware.HeaderFormatStyle();

            hfs.SetFont(Font = new Font("Arial", 11F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0))));
            hfs.Normal = new BrightIdeasSoftware.HeaderStateStyle() { BackColor = Color.FromArgb(76, 74, 132), ForeColor = Color.WhiteSmoke, FrameColor = Color.Black, FrameWidth = 1F };
            hfs.Hot = new BrightIdeasSoftware.HeaderStateStyle() { BackColor = Color.FromArgb(76, 74, 132), ForeColor = Color.WhiteSmoke, FrameColor = Color.Yellow, FrameWidth = 2F };

            this.objectListView_Results.HeaderFormatStyle = hfs;

            var rbd = new BrightIdeasSoftware.RowBorderDecoration();
            rbd.BorderPen = new Pen(Color.FromArgb(128, Color.LightSeaGreen), 2);
            rbd.BoundsPadding = new Size(1, 1);
            rbd.CornerRounding = 4.0f;

            this.objectListView_Results.HotItemStyle.Decoration = rbd;
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
            writeVerbose("Main Form Loading");
            this.webBrowser_Results.DocumentText = "<body style = \"background-color: #2b2b2b\" />";

            #region ObjectListView Delegates
            //Servers column grouping key
            this.olvCol_Servers.GroupKeyGetter = (rowObj) =>
            {
                var hru = rowObj as HealthRollup;
                return hru.Server.HostRole;
            };

            //Servers column data
            this.olvCol_Servers.AspectGetter = (obj) =>
            {
                var hru = obj as HealthRollup;

                return hru.Server.HostName;
            };

            //Status column data
            this.olvColumn_Status.AspectGetter = (obj) =>
                {
                    var hru = obj as HealthRollup;

                    if (hru.Errors.All(x => x.Result.Equals(StatusResult.Skipped)))
                        return StatusResult.Skipped;

                    return hru.Errors.Select(x => x.Result).Max();
                };

            //Function column data
            this.olvCol_Function.AspectGetter = (obj) =>
                {
                    var hru = obj as HealthRollup;
                    return hru.Server.HostFunction;
                };

            //Errors column data
            this.olvCol_ErrCount.AspectGetter = (obj) =>
            {
                var hru = obj as HealthRollup;
                var critErrors = hru.Errors.SelectMany(y => y.Error.Where(x => x.ToLower().Contains("critical"))).Count();
                var errors = hru.Errors.SelectMany(y => y.Error.Where(x => x.ToLower().Contains("error"))).Count();
                var warnings = hru.Errors.SelectMany(y => y.Error.Where(x => x.ToLower().Contains("warning"))).Count();
                return string.Format("Crit: {0} - Err: {1} - Warn: {2}", critErrors, errors, warnings);
            };

            //Errors column group on host function
            this.olvCol_ErrCount.GroupKeyGetter = (rowObj) =>
            {
                var hru = rowObj as HealthRollup;

                return hru.Server.HostFunction;
            };


            //Color format for rows based on status result of health check
            this.objectListView_Results.RowFormatter = (lvi) =>
                {
                    var hru = lvi.RowObject as HealthRollup;
                    lvi.Font = new Font("Arial", 10F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));

                    lvi.SelectedBackColor = Color.White;
                    if (hru.Errors.Select(x => x.Result).All(x => x.Equals(StatusResult.Skipped)))
                    {
                        lvi.BackColor = Color.MediumSlateBlue;
                        lvi.SelectedForeColor = Color.MediumSlateBlue;
                    }
                    else if (hru.Errors.Select(x => x.Result).Max() == StatusResult.Critical)
                    {
                        lvi.BackColor = System.Drawing.Color.DarkRed;
                        lvi.ForeColor = System.Drawing.Color.WhiteSmoke;
                        lvi.SelectedForeColor = Color.DarkRed;
                    }
                    else if (hru.Errors.Select(x => x.Result).Max() == StatusResult.Error)
                    {
                        lvi.BackColor = System.Drawing.Color.Red;
                        lvi.SelectedForeColor = Color.Red;
                    }
                    else if (hru.Errors.Select(x => x.Result).Max() == StatusResult.Warning)
                    {
                        lvi.BackColor = System.Drawing.Color.Yellow;
                        lvi.SelectedForeColor = Color.DarkGoldenrod;
                    }
                    else
                    {
                        lvi.BackColor = System.Drawing.Color.Green;
                        lvi.SelectedForeColor = Color.Green;
                    }
                };

            //delegate for when the selection changed to display it in the html panel
            this.objectListView_Results.SelectionChanged += (send, ev) =>
                {
                    this.webBrowser_Results.DocumentText = setHTMLSelectView(send, false);
                };

            this.objectListView_Results.SelectedRowDecoration = new BrightIdeasSoftware.RowBorderDecoration() { CornerRounding = 0, BorderPen = Pens.Yellow };
            //this.objectListView_Results.SelectedBackColor = Color.FromArgb(255, 182, 183, 181);
            //this.objectListView_Results.SelectedForeColor = Color.Black;

            this.objectListView_Results.RebuildColumns();
            #endregion ObjectListView Delegates
        }

        #region UserControlActions
        private void button_Begin_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (!this.bw.IsBusy)
            {
                //disableButton(ref btn);
                this.toolStripProgressBar1.Visible = true;
                this.toolStripStatusLabel_Progress.Visible = true;
                this.bw.RunWorkerAsync();
                btn.Text = "Cancel";
                btn.BackColor = Color.Red;
            }
            else
            {
                this.bw.CancelAsync();
                disableButton(ref btn);
            }
        }

        private void button_Email_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            using (EmailForm efrm = new EmailForm(setHTMLSelectView(this.objectListView_Results, true)))
            {
                if (efrm.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    this.label_Info.Text = "Email sent!";
                }
            }
        }

        private void menuItem_Exit_Click(object sender, EventArgs e)
        {
            if (this.bw.IsBusy)
            {
                bw.CancelAsync();
            }

            for (int i = 0; i < this.OwnedForms.Count(); i++)
            {
                this.OwnedForms[i].Close();
            }

            this.Close();
        }
        #endregion UserControlActions

        #region BackgroundWorker
        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;

            var hruCol = new HealthRollupCollection(() => new HealthRollup());
            var servers = ServerConfigMgr.GetServers();

            int index = 0;
            int iServerCount = servers.Count();

            var checkedGroupServices = new ConcurrentBag<Tuple<ServerRole,HCWinService>>();
            
            try
            {
                //Perform health check on each server
                Parallel.ForEach(servers.ToList(), (server) =>
                {
                    if (worker.CancellationPending || e.Cancel)
                    {
                        writeVerbose("Background worker is cancelling.");
                        e.Result = hruCol;
                        e.Cancel = true;
                        worker.ReportProgress(0, hruCol);
                        return;
                    }

                    server.IsOnline = GenericChecks.getIsOnline(server);
                    GenericChecks genChecks = new GenericChecks();
                    var winServicesToCheck = ServerHealthCheckConfigMgr.GetWindowsServicesToCheck()
                        .Where
                            (
                                x => x.Servers.Where(y => y.Item1 && y.Item2.HostName == server.HostName).Count() > 0 && x.Roles.Where(y => !y.Item1 && y.Item2 == server.HostRole && y.Item3 == server.HostFunction).Count() == 0
                                    || x.Roles.Where(y => y.Item1 && y.Item2 == server.HostRole && y.Item3 == server.HostFunction).Count() > 0 && x.Servers.Where(y => !y.Item1 && y.Item2.HostName == server.HostName).Count() == 0
                                    || !x.Function.Equals(ServerFunction.Unknown) && (x.Function.Equals(server.HostFunction) && x.Servers.Where(y => !y.Item1 && y.Item2.HostName == server.HostName).Count() + x.Roles.Where(y => !y.Item1 && y.Item2 == server.HostRole && y.Item3 == server.HostFunction).Count() == 0)
                                    || ((x.Roles.Count + x.Servers.Count == 0) && (x.Function.Equals(ServerFunction.Unknown)))
                            ).ToArray();

                    var tskGeneric = genChecks.PerformServerCheck(server);                  
                    var tskWinServices = GenericChecks.CheckWindowsServices(server, winServicesToCheck);
                    var tskListGrpWinServices = new List<Task>();

                    if (server.IsOnline && server.IsActive)
                    {
                        //For the services that have onepergroup set, calculate the servers that belong to the group
                        foreach (var grpWinService in winServicesToCheck.Where(x => x.OnePerGroup))
                        {
                            writeVerbose(string.Format("Checking group service {0} on {1}.", grpWinService.Name, server.HostName));

                            //Skip if we already have already calculated which servers belong to the group for this service
                            if (checkedGroupServices.Where(x => x.Item1 == server.HostRole).Select(x => x.Item2).Contains(grpWinService))
                            {
                                writeVerbose(string.Format("Already added group for service {0} for cluster containing {1}.", grpWinService.Name, server.HostName));
                                continue;
                            }

                            List<FiOSServer> grpServers = new List<FiOSServer>();

                            //if there are roles, add the groups of servers with the same roles, functions, locations, and servers that start with the same first 4 letters.
                            if (grpWinService.Roles.Count > 0)
                            {
                                writeVerbose(string.Format("First grpWinService.Roles.Count > 0 true for {0} on {1}", grpWinService.Name, server.HostName));
                                grpWinService.Roles.Where(x => x.Item2 == server.HostRole).ToList().ForEach((role) =>
                                    {
                                        grpServers.AddRange(servers.Where(x => (x.HostRole == role.Item2 && x.HostFunction == role.Item3) && role.Item1 && x.HostLocationName == server.HostLocationName
                                            && x.HostName.StartsWith(server.HostName.Substring(0, 4))).ToList());
                                    });
                            }
                            //get server groups by host function and host name
                            else
                            {
                                writeVerbose(string.Format("First grpWinService.Roles.Count > 0 false for {0} on {1}", grpWinService.Name, server.HostName));
                                grpServers.AddRange(servers.Where(x => x.HostFunction == grpWinService.Function && x.HostName.StartsWith(server.HostName.Substring(0, 4))).ToList());
                            }

                            //Add directly included servers
                            if (grpWinService.Servers.Count > 0)
                            {
                                grpWinService.Servers.ForEach((svr) =>
                                    {
                                        grpServers.AddRange(servers.Where(x => (x.HostFullName == svr.Item2.HostFullName) && svr.Item1));
                                    });
                            }

                            writeVerbose(string.Format("grpServers count = {0} for {1} on {2}", grpServers.Where(x => x.IsActive && x.IsOnline).Count(), grpWinService.Name, server.HostName));

                            //Do not check service if the server's name does not start with any of the group server's names
                            if (grpServers.Count > 0 && !grpServers.Any(x => x.HostName.ToUpper().StartsWith(server.HostName.ToUpper().Substring(0, 4))))
                            {
                                writeVerbose(string.Format("{0} is not a member of the server group for {1}.", server, grpWinService.Name));
                                continue;
                            }

                            //If there are multiple roles, add a task for each one so that they don't all group up under the same role in the results
                            if (grpWinService.Roles.Count > 1)
                            {
                                grpWinService.Roles.ForEach((role) =>
                                    {
                                        tskListGrpWinServices.Add(GenericChecks.CheckWindowsServices(grpServers.Where(x => x.HostRole == role.Item2 && x.IsActive && x.IsOnline).Select(x => x.HostName).ToArray(), grpWinService));
                                    });
                            }
                            else
                            {
                                tskListGrpWinServices.Add(GenericChecks.CheckWindowsServices(grpServers.Where(x => x.IsActive && x.IsOnline).Select(x => x.HostFullName).ToArray(), grpWinService));
                            }

                            //Add to list to avoid grouping servers for the same service
                            checkedGroupServices.Add(new Tuple<ServerRole, HCWinService>(server.HostRole, grpWinService));
                        }
                    }

                    //Run general server checks
                    try
                    {
                        writeVerbose(string.Format("Beginning general server check task for {0}.", server.HostName));
                        tskGeneric.Wait();
                    }
                    catch (AggregateException aex)
                    {
                        foreach (var ex in aex.Flatten().InnerExceptions)
                        {
                            writeEvent(string.Format("Error while performing general server checks on {0}. {1}", server.HostFullName, ex.Message), 10801, System.Diagnostics.TraceEventType.Error, true);
                        }
                    }

                    //Run group windows service checks
                    if (tskListGrpWinServices.Count > 0)
                    {
                        try
                        {
                            writeVerbose(string.Format("Beginning group tasks for {0}.", server.HostName));
                            Task.WaitAll(tskListGrpWinServices.ToArray());
                        }
                        catch (AggregateException aex)
                        {
                            foreach(var ex in aex.Flatten().InnerExceptions)
                            {
                                writeEvent(string.Format("Error while performing windows service check on server group containing {0}", server.HostFullName), 10801, System.Diagnostics.TraceEventType.Error, true);
                            }
                        }
                    }

                    //Run windows service checks
                    try
                    {
                        writeVerbose(string.Format("Beginning windows service check task for {0}.", server.HostName));
                        tskWinServices.Wait();
                    }
                    catch (AggregateException aex)
                    {
                        foreach(var ex in aex.Flatten().InnerExceptions)
                        {
                            writeEvent(string.Format("Error while performing windows service check on {0}. {1}", server.HostFullName, ex.Message), 10802, System.Diagnostics.TraceEventType.Error, true);
                        }
                    }

                    //Get results for all tasks
                    try
                    {
                        if (tskGeneric.Status == TaskStatus.RanToCompletion)
                            hruCol.PutObject(tskGeneric.Result);
                        writeVerbose(string.Format("Finished general server check task for {0}.", server.HostName));
                        if (tskWinServices.Status == TaskStatus.RanToCompletion)
                        {
                            foreach (var result in tskWinServices.Result)
                            {
                                hruCol.PutObject(result);
                            }
                        }
                        writeVerbose(string.Format("Finished windows service check task for {0}.", server.HostName));

                        //Group services results
                        foreach (Task<List<HealthCheckError>> tsk in tskListGrpWinServices)
                        {
                            if (tsk.Status == TaskStatus.RanToCompletion)
                            {
                                hruCol.PutObject(new HealthRollup() { Server = server, Errors = tsk.Result });
                            }
                        }
                        writeVerbose(string.Format("Finished group tasks for {0}.", server.HostName));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Failed to add health check for {0} to collection. {1}", server.HostName, ex.Message));
                    }

                    //Web server checks
                    if (server is FiOSWebServer)
                    {
                        writeVerbose(string.Format("Beginning web server checks task for {0}.", server.HostName));
                        IISChecks iisChecks = new IISChecks();
                        var tskIISChecks = iisChecks.CheckWebServer(server as FiOSWebServer);

                        try
                        {
                            tskIISChecks.Wait();
                        }
                        catch (AggregateException aex)
                        {
                            foreach (var ex in aex.Flatten().InnerExceptions)
                            {
                                writeEvent(string.Format("Error while performing IIS checks on {0}. {1}.", server.HostFullName, ex.Message), 10801, System.Diagnostics.TraceEventType.Error, true);
                            }
                        }

                        try
                        {
                            if (tskIISChecks.Status == TaskStatus.RanToCompletion)
                                hruCol.PutObject(tskIISChecks.Result);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("Failed to add web health check for {0} to collection. {1}", server.HostName, ex.Message));
                        }
                        writeVerbose(string.Format("Finished web server checks task for {0}.", server.HostName));
                    }

                    bw.ReportProgress((int)(((decimal)++index / (decimal)iServerCount) * 100), hruCol);
                    writeVerbose(string.Format("Finished reporting progress for {0}. {1} / {2}", server.HostName, index, iServerCount));
                });
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.Flatten().InnerExceptions)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                    writeError(string.Format("Error in server checks. {0}", ex.Message), 10805, System.Diagnostics.TraceEventType.Error);
                }
            }
            catch(Exception ex)
            {
                writeError(string.Format("Error while performing checks.", ex.Message), 10800, System.Diagnostics.TraceEventType.Error);
            }

            e.Result = hruCol;
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.toolStripProgressBar1.ProgressBar.Value = e.ProgressPercentage;
            this.toolStripProgressBar1.ProgressBar.Style = ProgressBarStyle.Continuous;          
            try
            {
                var bw = sender as BackgroundWorker;
                if (bw.CancellationPending)
                {
                    this.toolStripStatusLabel_Progress.Text = "Cancelling...";
                    if (this.toolStripProgressBar1.Visible)
                        this.toolStripProgressBar1.Visible = false;
                    return;
                }
                else
                {
                    this.toolStripStatusLabel_Progress.Text = ((decimal)e.ProgressPercentage / 100m).ToString("P0");
                }

                if (e.ProgressPercentage % 5 == 0 || e.ProgressPercentage > 96)
                {
                    lock (_lockObj)
                    {
                        var selectedSvrs = new List<string>();

                        foreach (BrightIdeasSoftware.OLVListItem olvLI in this.objectListView_Results.SelectedItems)
                        {
                            var hru = olvLI.RowObject as HealthRollup;
                            writeVerbose(string.Format("Adding {0} to selected objects.", hru.Server.HostName));
                            selectedSvrs.Add(hru.Server.HostName);
                        }

                        //var selectedSvrs = saveSelections(this.objectListView_Results);
                        var hruCol = e.UserState as HealthRollupCollection;
                        hruCol.ConcurrentToList();

                        writeVerbose("Setting objects...");
                        this.objectListView_Results.SetObjects(hruCol, true);

                        restoreSelections(selectedSvrs);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error in progress changed. {0}", ex.Message));
                writeError(string.Format("Error in progress changed. {0}", ex.Message), null, System.Diagnostics.TraceEventType.Error, false);
            }
        }

        private void bw_Complete(object sender, RunWorkerCompletedEventArgs e)
        {
            //If error, display it in progress status label
            if (e.Error != null)
                this.toolStripStatusLabel_Progress.Text = string.Format("Error - {0}", e.Error.Message);
            //If cancelled, display cancelled in progress tatus label but still enable the emamil button
            else if (e.Cancelled)
            {
                this.toolStripStatusLabel_Progress.Text = "Cancelled";
                this.enableButton(ref this.button_Email, Color.DarkSlateBlue, Color.WhiteSmoke);
                writeVerbose("Checks cancelled!");
            }
            else
            {
                //Gather final results
                var hruCol = e.Result as HealthRollupCollection;
                hruCol.ConcurrentToList();

                //Save selected server state
                var selectedSvrs = new List<string>();
                foreach (BrightIdeasSoftware.OLVListItem olvLI in this.objectListView_Results.SelectedItems)
                {
                    var hru = olvLI.RowObject as HealthRollup;
                    writeVerbose(string.Format("Adding {0} to selected objects.", hru.Server.HostName));
                    selectedSvrs.Add(hru.Server.HostName);
                }

                //Set objects final
                this.objectListView_Results.SetObjects(hruCol);

                //Restore previous selections
                restoreSelections(selectedSvrs);

                //Display complete in progress status label and enable email button
                this.toolStripStatusLabel_Progress.Text = "Complete!";
                this.enableButton(ref this.button_Email, Color.DarkSlateBlue, Color.WhiteSmoke);
                writeVerbose("Checks complete!");
            }

            this.toolStripProgressBar1.Visible = false;
            this.toolStripProgressBar1.Value = 0;
            enableButton(ref this.button_Begin, Color.DarkGreen, Color.WhiteSmoke);
            this.button_Begin.Text = "Begin";
        }
        #endregion BackgroundWorker

        #region PrivateMethods
        /// <summary>
        /// Re-selects the items that were selected before SetObjects was called
        /// </summary>
        /// <param name="selectedSvrs">Names of the servers that were selected</param>
        private void restoreSelections(IEnumerable<string> selectedSvrs)
        {
            if (selectedSvrs.ToList().Count > 0)
            {
                for (int i = 0; i < this.objectListView_Results.Items.Count; i++)
                {
                    var item = (BrightIdeasSoftware.OLVListItem)this.objectListView_Results.Items[i];
                    string svr = ((HealthRollup)item.RowObject).Server.HostName;
                    writeVerbose(string.Format("svr = {0}", svr));
                    if (selectedSvrs.Contains(svr))
                    {
                        writeVerbose(string.Format("Selecting {0} in OLV.", svr));
                        item.Selected = true;
                    }
                    else
                    {
                        writeVerbose(string.Format("{0} not in list: {1}", svr, string.Join(",", selectedSvrs.ToArray())));
                    }
                }
            }
        }

        private string setHTMLSelectView(object sender, bool isEmail)
        {
            try
            {
                var olv = sender as BrightIdeasSoftware.ObjectListView;

                //If nothing is selected, show a dark empty background
                if (olv == null || (!isEmail && olv.SelectedItems.Count < 1))
                {
                    return "<body style = \"background-color: #2b2b2b\" />";
                }

                int itemCount = 0;
                var htmlFormatter = new HTMLFormatter();

                if (isEmail)
                {
                    itemCount = olv.Items.Count;
                    htmlFormatter.SetBody(null, ("FiOS Health Check - " + DateTime.Today.ToString("MM/dd/yyyy")));
                }
                else
                {
                    itemCount = olv.SelectedItems.Count;
                    htmlFormatter.SetBody("#777677");
                }

                //Create an array since OLV Item collections does not have an enumerator for linq
                BrightIdeasSoftware.OLVListItem[] selectedOlvLis = new BrightIdeasSoftware.OLVListItem[itemCount];

                //If email, copy all items in OLV to array. Otherwise copy only selected items.
                if (isEmail)
                    olv.Items.CopyTo(selectedOlvLis, 0);
                else
                    olv.SelectedItems.CopyTo(selectedOlvLis, 0);

                //Group health rollups on the server role to be able to display the results under each role
                var roles = selectedOlvLis.Select(x => x.RowObject as HealthRollup).GroupBy(x => x.Server.HostRole)
                    .Select(x => new { x.Key, HostNames = x.Select(y => y.Server) });

                //loop through each role, creating the selected servers and displaying their result and any errors if they exist
                foreach (var role in roles.OrderBy(x => x.Key))
                {
                    htmlFormatter.SetRole(role.Key.ToString());

                    foreach (var svr in role.HostNames.OrderBy(x => !x.IsActive).ThenBy(x => x.HostName))
                    {
                        if (!svr.IsActive)
                            htmlFormatter.BeginTable(string.Format("{0} (Inactive)", svr.HostName), "#2f285b");
                        else
                            htmlFormatter.BeginTable(svr.HostName);

                        var hru = selectedOlvLis.Select(x => x.RowObject as HealthRollup).Where(x => x.Server.HostName.Equals(svr.HostName)).FirstOrDefault();
                        
                        foreach (var err in hru.Errors)
                        {
                            writeVerbose(string.Format("{0} has {1} errors for check type {2}. Result: {3}", svr.HostName, err.Error.Count, err.HCType, err.Result));
                            htmlFormatter.AddStatusRow(err.HCType.ToString(), err.Result);
                            if (err.Error.Count > 0)
                            {
                                writeVerbose(string.Format("{0} - Check Type {1} - Errors: {2}", svr.HostName, err.HCType, string.Join(System.Environment.NewLine, err.Error)));
                                htmlFormatter.AddErrorDescriptionRows(err.Error);
                            }
                        }

                        htmlFormatter.EndTable();
                    }
                }
                return htmlFormatter.ToString();
            }
            catch (Exception ex)
            {
                writeError(string.Format("Error in selection changed delegate. {0}", ex.Message), 10900, System.Diagnostics.TraceEventType.Error);
                return string.Empty;
            }
        }

        private void disableButton(ref Button btn)
        {
            btn.BackColor = System.Drawing.Color.LightGray;
            btn.Enabled = false;
        }

        private void enableButton(ref Button btn, Color backColor, Color foreColor)
        {
            btn.BackColor = backColor;
            btn.ForeColor = foreColor;
            btn.Enabled = true;
        }

        private void writeVerbose(string message)
        {
#if DEBUG
            if (Logger.IsLoggingEnabled())
            {
                Logger.Write(message, "Verbose", 0, 0, System.Diagnostics.TraceEventType.Verbose);
            }
#endif
        }

        private void writeError(string message, int? eventId, System.Diagnostics.TraceEventType eventType, bool writeToEventLog = false, string title = "FiOS Health Check Application")
        {
            if (eventType != System.Diagnostics.TraceEventType.Error && eventType != System.Diagnostics.TraceEventType.Critical)
                throw new ArgumentException("Event type must be an error or critical to write to the error log");
            if (Logger.IsLoggingEnabled())
            {
                if (!eventId.HasValue)
                    eventId = 10800;

                Logger.Write(message, "ErrorLog", 10, eventId.Value, eventType, title);

                if (writeToEventLog)
                    writeEvent(message, eventId, eventType, false, title);
            }
        }

        private void writeEvent(string message, int? eventId, System.Diagnostics.TraceEventType eventType, bool writeToErrorLog = false, string title = "FiOS Health Check Application")
        {
            if (eventType != System.Diagnostics.TraceEventType.Error && eventType != System.Diagnostics.TraceEventType.Critical)
                throw new ArgumentException("Event type must be an error or critical to write to the event log");

            if (Logger.IsLoggingEnabled())
            {
                if (!eventId.HasValue)
                    eventId = 10800;

                Logger.Write(message, "EventLog", 20, eventId.Value, eventType, title);

                if (writeToErrorLog)
                    writeError(message, eventId, eventType, false, title);
            }
        }
        #endregion PrivateMethods
    }
}
