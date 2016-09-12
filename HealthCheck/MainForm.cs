using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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
            InitializeComponent();
            this.bw = new BackgroundWorker();
            this.bw.WorkerSupportsCancellation = true;
            this.bw.WorkerReportsProgress = true;
            this.bw.DoWork += this.bw_DoWork;
            this.bw.ProgressChanged += this.bw_ProgressChanged;
            this.bw.RunWorkerCompleted += this.bw_Complete;
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
            //var servers = new HealthRollupCollection();
            //servers.Add(new HealthRollup { Server = new FiOSServer() { HostName = "NSPTXW01P01", HostRole = ServerRole.NSP, HostFunction = ServerFunction.Web }, Result = StatusResult.Error, Errors = new List<string>() { "Error 1", "Error 2" } });
            //servers.Add(new HealthRollup { Server = new FiOSServer() { HostName = "NSPTXW02P02", HostRole = ServerRole.NSP, HostFunction = ServerFunction.Web }, Result = StatusResult.Ok, Errors = new List<string>() });

            //this.objectListView_Results.SetObjects(servers);

            this.olvCol_Servers.GroupKeyGetter = (rowObj) =>
            {
                var hru = rowObj as HealthRollup;
                return hru.Server.HostRole;
            };

            this.olvCol_Servers.AspectGetter = (obj) =>
            {
                var hru = obj as HealthRollup;

                return hru.Server.HostName;
            };

            this.olvCol_ErrCount.AspectGetter = (obj) =>
            {
                var hru = obj as HealthRollup;
                var critErrors = hru.Errors.SelectMany(y => y.Error.Where(x => x.ToLower().Contains("critical"))).Count();
                var errors = hru.Errors.SelectMany(y => y.Error.Where(x => x.ToLower().Contains("error"))).Count();
                var warnings = hru.Errors.SelectMany(y => y.Error.Where(x => x.ToLower().Contains("warning"))).Count();
                return string.Format("Crit: {0} - Err: {1} - Warn: {2}", critErrors, errors, warnings);
            };

            this.olvCol_ErrCount.GroupKeyGetter = (rowObj) =>
            {
                var hru = rowObj as HealthRollup;

                return hru.Server.HostFunction;
            };

            this.objectListView_Results.RowFormatter = (lvi) =>
                {
                    var hru = lvi.RowObject as HealthRollup;
                    if (hru.Result == StatusResult.Critical)
                    {
                        lvi.BackColor = System.Drawing.Color.DarkRed;
                        lvi.Font = new Font("Arial", 12F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
                    }
                    else if (hru.Result == StatusResult.Error)
                    {
                        lvi.BackColor = System.Drawing.Color.Red;
                    }
                    else if (hru.Result == StatusResult.Warning)
                    {
                        lvi.BackColor = System.Drawing.Color.Yellow;
                    }
                    else
                    {
                        lvi.BackColor = System.Drawing.Color.Green;
                    }
                };

            this.objectListView_Results.SelectionChanged += (send, ev) =>
                {
                    var olv = send as BrightIdeasSoftware.ObjectListView;

                    if (olv == null || olv.SelectedItem == null)
                        return;

                    var hru = olv.SelectedItem.RowObject as HealthRollup;

                    var htmlFormatter = new HTMLFormatter();

                    htmlFormatter.BeginTable(hru.Server.HostName);
                    htmlFormatter.SetRole(hru.Server.HostRole.ToString());

                    foreach (var err in hru.Errors)
                    {
                        htmlFormatter.AddStatusRow(err.HCType.ToString(), hru.Result);
                        htmlFormatter.AddErrorDescriptionRows(err.Error);
                    }
                    htmlFormatter.EndTable();

                    this.webBrowser_Results.DocumentText = htmlFormatter.ToString();
                };

            this.objectListView_Results.RebuildColumns();
            
        }

        private void button_Begin_Click(object sender, EventArgs e)
        {
            if (!this.bw.IsBusy)
            {
                var btn = sender as Button;
                disableButton(ref btn);
                this.toolStripProgressBar1.Visible = true;
                this.bw.RunWorkerAsync();
            }
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;

            var hruCol = new HealthRollupCollection(() => new HealthRollup());
            var servers = ServerConfigMgr.GetServers();

            int index = 0;
            int iServerCount = servers.Count();

            try
            {
                Parallel.ForEach(servers.ToList(), (server) =>
                {
                    GenericChecks genChecks = new GenericChecks();

                    var tskGeneric = genChecks.PerformServerCheck(server);

                    tskGeneric.Wait();
                    try
                    {
                        hruCol.PutObject(tskGeneric.Result);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Failed to add health check for {0} to collection. {1}", server.HostName, ex.Message));
                    }

                    if (server is FiOSWebServer)
                    {
                        IISChecks iisChecks = new IISChecks();
                        var tskIISChecks = iisChecks.CheckWebServer(server as FiOSWebServer);

                        tskIISChecks.Wait();

                        try
                        {
                            hruCol.PutObject(tskIISChecks.Result);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("Failed to add web health check for {0} to collection. {1}", server.HostName, ex.Message));
                        }
                    }

                    bw.ReportProgress((int)(((decimal)++index / (decimal)iServerCount) * 100), hruCol);
                });
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.InnerExceptions)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                }
            }
            e.Result = hruCol;
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.toolStripProgressBar1.ProgressBar.Value = e.ProgressPercentage;
            this.toolStripProgressBar1.ProgressBar.Style = ProgressBarStyle.Continuous;
            try
            {
                if (e.ProgressPercentage % 25 == 0)
                {
                    lock (_lockObj)
                    {
                        var hruCol = e.UserState as HealthRollupCollection;
                        hruCol.ConcurrentToList();
                        if (this.objectListView_Results.Items.Count == 0)
                            this.objectListView_Results.SetObjects(hruCol);
                        else
                        {
                            this.objectListView_Results.SetObjects(hruCol as ICollection);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error in progress changed. {0}", ex.Message));
            }
        }

        private void bw_Complete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                this.statusStrip_Main.Text = string.Format("Error - {0}", e.Error.Message);
            else if (e.Cancelled)
                this.statusStrip_Main.Text = "Cancelled";
            else
            {
                this.statusStrip_Main.Text = "Complete!";
                var hru = e.Result as HealthRollupCollection;
                hru.ConcurrentToList();
                this.objectListView_Results.SetObjects(hru);
                this.enableButton(ref this.button_Email, Color.DarkSlateBlue, Color.WhiteSmoke);
            }

            this.toolStripProgressBar1.Visible = false;
            this.toolStripProgressBar1.Value = 0;
            enableButton(ref this.button_Begin, Color.DarkGreen, Color.WhiteSmoke);
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
    }
}
