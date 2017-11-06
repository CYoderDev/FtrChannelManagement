using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FrontierVOps.Common.FiOS;
using FrontierVOps.FiOS.HealthCheck.Controllers;
using FrontierVOps.FiOS.HealthCheck.DataObjects;
using FrontierVOps.FiOS.Servers.Controllers;
using FrontierVOps.FiOS.Servers.Objects;
using FrontierVOps.FiOS.Servers.Components;

namespace HealthCheck
{
    public partial class ServerCheckControl : UserControl, iCheck
    {
        public string HTMLResults { get { return this._htmlResults; } set { this._htmlResults = value; } }
        private string _htmlResults;

        private readonly object _lockObj = new object();

        public BackgroundWorker bw { get; set; }

        public ServerCheckControl()
        {
            InitializeComponent();
            this.bw = new BackgroundWorker();
            this.bw.WorkerSupportsCancellation = true;
            this.bw.WorkerReportsProgress = true;
            this.bw.ProgressChanged += this.bw_ProgressChanged;
            this.bw.RunWorkerCompleted += this.bw_WorkerComplete;
            this._htmlResults = setHTMLSelectView(null, false);
        }

        public void BeginCheck(object sender, DoWorkEventArgs e)
        {
            mainForm.writeVerbose("Beginning Server Check...");
            var hruCol = new HealthRollupCollection(() => new HealthRollup());
            var servers = ServerConfigMgr.GetServers();

            int index = 0;
            int iServerCount = servers.Count();

            var checkedGroupServices = new ConcurrentBag<Tuple<FiOSRole,HCWinService>>();


            try
            {
                //Perform health check on each server
                Parallel.ForEach(servers.ToList(), (server) =>
                {
                    if (bw.CancellationPending || e.Cancel)
                    {
                        mainForm.writeVerbose("Background worker is cancelling.");
                        e.Result = hruCol;
                        e.Cancel = true;
                        bw.ReportProgress(0, hruCol);
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
                            mainForm.writeVerbose(string.Format("Checking group service {0} on {1}.", grpWinService.Name, server.HostName));

                            //Skip if we already have already calculated which servers belong to the group for this service
                            if (checkedGroupServices.Where(x => x.Item1 == server.HostRole).Select(x => x.Item2).Contains(grpWinService))
                            {
                                mainForm.writeVerbose(string.Format("Already added group for service {0} for cluster containing {1}.", grpWinService.Name, server.HostName));
                                continue;
                            }

                            List<FiOSServer> grpServers = new List<FiOSServer>();

                            //if there are roles, add the groups of servers with the same roles, functions, locations, and servers that start with the same first 4 letters.
                            if (grpWinService.Roles.Count > 0)
                            {
                                mainForm.writeVerbose(string.Format("First grpWinService.Roles.Count > 0 true for {0} on {1}", grpWinService.Name, server.HostName));
                                grpWinService.Roles.Where(x => x.Item2 == server.HostRole).ToList().ForEach((role) =>
                                    {
                                        grpServers.AddRange(servers.Where(x => (x.HostRole == role.Item2 && x.HostFunction == role.Item3) && role.Item1 && x.HostLocationName == server.HostLocationName
                                            && x.HostName.StartsWith(server.HostName.Substring(0, 4))).ToList());
                                    });
                            }
                            //get server groups by host function and host name
                            else
                            {
                                mainForm.writeVerbose(string.Format("First grpWinService.Roles.Count > 0 false for {0} on {1}", grpWinService.Name, server.HostName));
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

                            mainForm.writeVerbose(string.Format("grpServers count = {0} for {1} on {2}", grpServers.Where(x => x.IsActive && x.IsOnline).Count(), grpWinService.Name, server.HostName));

                            //Do not check service if the server's name does not start with any of the group server's names
                            if (grpServers.Count > 0 && !grpServers.Any(x => x.HostName.ToUpper().StartsWith(server.HostName.ToUpper().Substring(0, 4))))
                            {
                                mainForm.writeVerbose(string.Format("{0} is not a member of the server group for {1}.", server, grpWinService.Name));
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
                            checkedGroupServices.Add(new Tuple<FiOSRole, HCWinService>(server.HostRole, grpWinService));
                        }
                    }

                    //Run general server checks
                    try
                    {
                        mainForm.writeVerbose(string.Format("Beginning general server check task for {0}.", server.HostName));
                        tskGeneric.Wait();
                    }
                    catch (AggregateException aex)
                    {
                        foreach (var ex in aex.Flatten().InnerExceptions)
                        {
                            mainForm.writeEvent(string.Format("Error while performing general server checks on {0}. {1}", server.HostFullName, ex.Message), 10801, System.Diagnostics.TraceEventType.Error, true);
                        }
                    }


                    //Run group windows service checks
                    if (tskListGrpWinServices.Count > 0)
                    {
                        try
                        {
                            mainForm.writeVerbose(string.Format("Beginning group tasks for {0}.", server.HostName));
                            Task.WaitAll(tskListGrpWinServices.ToArray());
                        }
                        catch (AggregateException aex)
                        {
                            foreach (var ex in aex.Flatten().InnerExceptions)
                            {
                                mainForm.writeEvent(string.Format("Error while performing windows service check on server group containing {0}", server.HostFullName), 10801, System.Diagnostics.TraceEventType.Error, true);
                            }
                        }
                    }

                    //Run windows service checks
                    try
                    {
                        mainForm.writeVerbose(string.Format("Beginning windows service check task for {0}.", server.HostName));
                        tskWinServices.Wait();
                    }
                    catch (AggregateException aex)
                    {
                        foreach (var ex in aex.Flatten().InnerExceptions)
                        {
                            mainForm.writeEvent(string.Format("Error while performing windows service check on {0}. {1}", server.HostFullName, ex.Message), 10802, System.Diagnostics.TraceEventType.Error, true);
                        }
                    }

                    //Get results for all tasks
                    try
                    {
                        if (tskGeneric.Status == TaskStatus.RanToCompletion)
                            hruCol.PutObject(tskGeneric.Result);
                        mainForm.writeVerbose(string.Format("Finished general server check task for {0}.", server.HostName));
                        if (tskWinServices.Status == TaskStatus.RanToCompletion)
                        {
                            foreach (var result in tskWinServices.Result)
                            {
                                hruCol.PutObject(result);
                            }
                        }
                        mainForm.writeVerbose(string.Format("Finished windows service check task for {0}.", server.HostName));

                        //Group services results
                        foreach (Task<List<HealthCheckError>> tsk in tskListGrpWinServices)
                        {
                            if (tsk.Status == TaskStatus.RanToCompletion)
                            {
                                hruCol.PutObject(new HealthRollup() { Server = server, Errors = tsk.Result });
                            }
                        }
                        mainForm.writeVerbose(string.Format("Finished group tasks for {0}.", server.HostName));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Failed to add health check for {0} to collection. {1}", server.HostName, ex.Message));
                    }

                    //Web server checks
                    if (server is FiOSWebServer)
                    {
                        mainForm.writeVerbose(string.Format("Beginning web server checks task for {0}.", server.HostName));
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
                                mainForm.writeEvent(string.Format("Error while performing IIS checks on {0}. {1}.", server.HostFullName, ex.Message), 10801, System.Diagnostics.TraceEventType.Error, true);
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
                        mainForm.writeVerbose(string.Format("Finished web server checks task for {0}.", server.HostName));
                    }

                    bw.ReportProgress((int)(((decimal)++index / (decimal)iServerCount) * 100), hruCol);
                    mainForm.writeVerbose(string.Format("Finished reporting progress for {0}. {1} / {2}", server.HostName, index, iServerCount));
                });
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.Flatten().InnerExceptions)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                    mainForm.writeError(string.Format("Error in server checks. {0}", ex.Message), 10805, System.Diagnostics.TraceEventType.Error);
                }
            }
            catch (Exception ex)
            {
                mainForm.writeError(string.Format("Error while performing checks.", ex.Message), 10800, System.Diagnostics.TraceEventType.Error);
            }
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.TSProgressBar.ProgressBar.Value = e.ProgressPercentage;
            this.TSProgressBar.ProgressBar.Style = ProgressBarStyle.Continuous;
            try
            {
                var bw = sender as BackgroundWorker;
                if (bw.CancellationPending)
                {
                    this.TSProgressBar.Text = "Cancelling...";
                    if (this.TSProgressBar.Visible)
                        this.TSProgressBar.Visible = false;
                    return;
                }
                else
                {
                    this.TSProgressBar.Text = ((decimal)e.ProgressPercentage / 100m).ToString("P0");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error in progress changed. {0}", ex.Message));
                mainForm.writeError(string.Format("Error in progress changed. {0}", ex.Message), null, System.Diagnostics.TraceEventType.Error, false);
            }

            if (e.ProgressPercentage % 5 == 0 || e.ProgressPercentage > 96)
            {
                lock (_lockObj)
                {
                    var selectedSvrs = new List<string>();

                    foreach (BrightIdeasSoftware.OLVListItem olvLI in this.objectListView_Results.SelectedItems)
                    {
                        var hru = olvLI.RowObject as HealthRollup;
                        mainForm.writeVerbose(string.Format("Adding {0} to selected objects.", hru.Server.HostName));
                        selectedSvrs.Add(hru.Server.HostName);
                    }

                    //var selectedSvrs = saveSelections(this.objectListView_Results);
                    var hruCol = e.UserState as HealthRollupCollection;
                    if (hruCol == null)
                    {
                        mainForm.writeError("hruCol is null in progress changed handler in UC.", 18020, System.Diagnostics.TraceEventType.Error, false);
                        return;
                    }
                    hruCol.ConcurrentToList();

                    mainForm.writeVerbose("Setting objects...");
                    this.objectListView_Results.SetObjects(hruCol, true);

                    mainForm.restoreSelections(this.objectListView_Results, selectedSvrs);
                }
            }
        }

        private void bw_WorkerComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!(e.Cancelled || e.Error == null))
            {
                //Gather final results
                var hruCol = e.Result as HealthRollupCollection;
                hruCol.ConcurrentToList();

                //Save selected server state
                var selectedSvrs = new List<string>();
                foreach (BrightIdeasSoftware.OLVListItem olvLI in this.objectListView_Results.SelectedItems)
                {
                    var hru = olvLI.RowObject as HealthRollup;
                    mainForm.writeVerbose(string.Format("Adding {0} to selected objects.", hru.Server.HostName));
                    selectedSvrs.Add(hru.Server.HostName);
                }

                //Set objects final
                this.objectListView_Results.SetObjects(hruCol);

                mainForm.restoreSelections(this.objectListView_Results, selectedSvrs);
            }
        }

        private void performOLVLayout()
        {
            mainForm.writeVerbose("Performing OLV layout...");
            var hfs = new BrightIdeasSoftware.HeaderFormatStyle();

            hfs.SetFont(Font = new Font("Arial", 11F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0))));
            hfs.Normal = new BrightIdeasSoftware.HeaderStateStyle() { BackColor = Color.FromArgb(76, 74, 132), ForeColor = Color.WhiteSmoke, FrameColor = Color.Black, FrameWidth = 1F };
            hfs.Hot = new BrightIdeasSoftware.HeaderStateStyle() { BackColor = Color.FromArgb(76, 74, 132), ForeColor = Color.WhiteSmoke, FrameColor = Color.Yellow, FrameWidth = 2F };

            try
            {
                this.objectListView_Results.HeaderFormatStyle = hfs;
            }
            catch (Exception ex)
            {
                mainForm.writeError(string.Format("Failed to set header format style on OLV. {0}", ex.Message), 18022, System.Diagnostics.TraceEventType.Error, false);
            }

            var rbd = new BrightIdeasSoftware.RowBorderDecoration();
            rbd.BorderPen = new Pen(Color.FromArgb(128, Color.LightSeaGreen), 2);
            rbd.BoundsPadding = new Size(1, 1);
            rbd.CornerRounding = 4.0f;

            try
            {
                this.objectListView_Results.HotItemStyle = new BrightIdeasSoftware.HotItemStyle();
                this.objectListView_Results.HotItemStyle.Decoration = rbd;
            }
            catch (Exception ex)
            {
                mainForm.writeError(string.Format("Failed to set hot item decoration style on OLV. {0}", ex.Message), 18022, System.Diagnostics.TraceEventType.Error, false);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            mainForm.writeVerbose("ServerCheckControl form loading...");
            base.OnLoad(e);
            performOLVLayout();
            #region OLV Delegates
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

            this.objectListView_Results.SelectedRowDecoration = new BrightIdeasSoftware.RowBorderDecoration() { CornerRounding = 0, BorderPen = Pens.Yellow };

            //delegate for when the selection changed to display it in the html panel
            this.objectListView_Results.SelectionChanged += (send, ev) =>
            {
                this._htmlResults = setHTMLSelectView(send, false);
                OnHTMLChanged(EventArgs.Empty);
            };

            this.objectListView_Results.RebuildColumns();
            #endregion
            
        }

        public string setHTMLSelectView()
        {
            return setHTMLSelectView(this.objectListView_Results, true);
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
                            mainForm.writeVerbose(string.Format("{0} has {1} errors for check type {2}. Result: {3}", svr.HostName, err.Error.Count, err.HCType, err.Result));
                            htmlFormatter.AddStatusRow(err.HCType.ToString(), err.Result);
                            if (err.Error.Count > 0)
                            {
                                mainForm.writeVerbose(string.Format("{0} - Check Type {1} - Errors: {2}", svr.HostName, err.HCType, string.Join(System.Environment.NewLine, err.Error)));
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
                mainForm.writeError(string.Format("Error in selection changed delegate. {0}", ex.Message), 10900, System.Diagnostics.TraceEventType.Error);
                return string.Empty;
            }
        }

        protected virtual void OnHTMLChanged(EventArgs e)
        {
            EventHandler handler = HTMLChanged;
            if (handler != null)
                handler(this, e);
        }
        public event EventHandler HTMLChanged;
    }
}
