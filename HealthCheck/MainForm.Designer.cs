﻿namespace HealthCheck
{
    partial class mainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.statusStrip_Main = new System.Windows.Forms.StatusStrip();
            this.menuStrip_Main = new System.Windows.Forms.MenuStrip();
            this.tableLayoutPanel_Main = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel_Left = new System.Windows.Forms.TableLayoutPanel();
            this.button_Begin = new System.Windows.Forms.Button();
            this.button_Email = new System.Windows.Forms.Button();
            this.tableLayoutPanel_Results = new System.Windows.Forms.TableLayoutPanel();
            this.objectListView_Results = new BrightIdeasSoftware.ObjectListView();
            this.olvCol_Servers = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvColumn_Status = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvCol_ErrCount = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.webBrowser_Results = new System.Windows.Forms.WebBrowser();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.statusStrip_Main.SuspendLayout();
            this.tableLayoutPanel_Main.SuspendLayout();
            this.tableLayoutPanel_Left.SuspendLayout();
            this.tableLayoutPanel_Results.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.objectListView_Results)).BeginInit();
            this.SuspendLayout();
            // 
            // statusStrip_Main
            // 
            this.statusStrip_Main.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripProgressBar1});
            this.statusStrip_Main.Location = new System.Drawing.Point(0, 792);
            this.statusStrip_Main.Name = "statusStrip_Main";
            this.statusStrip_Main.Size = new System.Drawing.Size(1505, 22);
            this.statusStrip_Main.TabIndex = 0;
            this.statusStrip_Main.Text = "statusStrip_Main";
            // 
            // menuStrip_Main
            // 
            this.menuStrip_Main.Location = new System.Drawing.Point(0, 0);
            this.menuStrip_Main.Name = "menuStrip_Main";
            this.menuStrip_Main.Size = new System.Drawing.Size(1505, 24);
            this.menuStrip_Main.TabIndex = 1;
            this.menuStrip_Main.Text = "menuStrip_Main";
            // 
            // tableLayoutPanel_Main
            // 
            this.tableLayoutPanel_Main.ColumnCount = 2;
            this.tableLayoutPanel_Main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel_Main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 85F));
            this.tableLayoutPanel_Main.Controls.Add(this.tableLayoutPanel_Left, 0, 0);
            this.tableLayoutPanel_Main.Controls.Add(this.tableLayoutPanel_Results, 1, 0);
            this.tableLayoutPanel_Main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel_Main.Location = new System.Drawing.Point(0, 24);
            this.tableLayoutPanel_Main.Name = "tableLayoutPanel_Main";
            this.tableLayoutPanel_Main.RowCount = 1;
            this.tableLayoutPanel_Main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 768F));
            this.tableLayoutPanel_Main.Size = new System.Drawing.Size(1505, 768);
            this.tableLayoutPanel_Main.TabIndex = 2;
            // 
            // tableLayoutPanel_Left
            // 
            this.tableLayoutPanel_Left.ColumnCount = 2;
            this.tableLayoutPanel_Left.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tableLayoutPanel_Left.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 90F));
            this.tableLayoutPanel_Left.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tableLayoutPanel_Left.Controls.Add(this.button_Begin, 1, 1);
            this.tableLayoutPanel_Left.Controls.Add(this.button_Email, 1, 2);
            this.tableLayoutPanel_Left.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel_Left.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel_Left.Name = "tableLayoutPanel_Left";
            this.tableLayoutPanel_Left.RowCount = 4;
            this.tableLayoutPanel_Left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel_Left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel_Left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel_Left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel_Left.Size = new System.Drawing.Size(219, 762);
            this.tableLayoutPanel_Left.TabIndex = 0;
            // 
            // button_Begin
            // 
            this.button_Begin.BackColor = System.Drawing.Color.DarkGreen;
            this.button_Begin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button_Begin.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_Begin.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.button_Begin.Location = new System.Drawing.Point(16, 238);
            this.button_Begin.Margin = new System.Windows.Forms.Padding(5, 10, 5, 10);
            this.button_Begin.Name = "button_Begin";
            this.button_Begin.Size = new System.Drawing.Size(198, 94);
            this.button_Begin.TabIndex = 0;
            this.button_Begin.Text = "Begin";
            this.button_Begin.UseVisualStyleBackColor = false;
            this.button_Begin.Click += new System.EventHandler(this.button_Begin_Click);
            // 
            // button_Email
            // 
            this.button_Email.BackColor = System.Drawing.Color.LightGray;
            this.button_Email.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button_Email.Enabled = false;
            this.button_Email.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_Email.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.button_Email.Location = new System.Drawing.Point(16, 352);
            this.button_Email.Margin = new System.Windows.Forms.Padding(5, 10, 5, 10);
            this.button_Email.Name = "button_Email";
            this.button_Email.Size = new System.Drawing.Size(198, 94);
            this.button_Email.TabIndex = 1;
            this.button_Email.Text = "Email Results";
            this.button_Email.UseVisualStyleBackColor = false;
            // 
            // tableLayoutPanel_Results
            // 
            this.tableLayoutPanel_Results.ColumnCount = 1;
            this.tableLayoutPanel_Results.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 1274F));
            this.tableLayoutPanel_Results.Controls.Add(this.objectListView_Results, 0, 0);
            this.tableLayoutPanel_Results.Controls.Add(this.webBrowser_Results, 0, 1);
            this.tableLayoutPanel_Results.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel_Results.Location = new System.Drawing.Point(228, 3);
            this.tableLayoutPanel_Results.Name = "tableLayoutPanel_Results";
            this.tableLayoutPanel_Results.RowCount = 2;
            this.tableLayoutPanel_Results.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.tableLayoutPanel_Results.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel_Results.Size = new System.Drawing.Size(1274, 762);
            this.tableLayoutPanel_Results.TabIndex = 1;
            // 
            // objectListView_Results
            // 
            this.objectListView_Results.AllColumns.Add(this.olvCol_Servers);
            this.objectListView_Results.AllColumns.Add(this.olvColumn_Status);
            this.objectListView_Results.AllColumns.Add(this.olvCol_ErrCount);
            this.objectListView_Results.CellEditUseWholeCell = false;
            this.objectListView_Results.Cursor = System.Windows.Forms.Cursors.Default;
            this.objectListView_Results.Dock = System.Windows.Forms.DockStyle.Fill;
            this.objectListView_Results.FullRowSelect = true;
            this.objectListView_Results.Location = new System.Drawing.Point(3, 3);
            this.objectListView_Results.Name = "objectListView_Results";
            this.objectListView_Results.Size = new System.Drawing.Size(1268, 527);
            this.objectListView_Results.TabIndex = 0;
            this.objectListView_Results.UseCompatibleStateImageBehavior = false;
            this.objectListView_Results.View = System.Windows.Forms.View.Details;
            // 
            // olvCol_Servers
            // 
            this.olvCol_Servers.AspectName = "Server";
            this.olvCol_Servers.FillsFreeSpace = true;
            this.olvCol_Servers.Text = "Servers";
            // 
            // olvColumn_Status
            // 
            this.olvColumn_Status.AspectName = "Result";
            this.olvColumn_Status.FillsFreeSpace = true;
            this.olvColumn_Status.Text = "Status";
            // 
            // olvCol_ErrCount
            // 
            this.olvCol_ErrCount.AspectName = "Errors";
            this.olvCol_ErrCount.FillsFreeSpace = true;
            this.olvCol_ErrCount.Text = "Error Count";
            // 
            // webBrowser_Results
            // 
            this.webBrowser_Results.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser_Results.Location = new System.Drawing.Point(3, 536);
            this.webBrowser_Results.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser_Results.Name = "webBrowser_Results";
            this.webBrowser_Results.Size = new System.Drawing.Size(1268, 223);
            this.webBrowser_Results.TabIndex = 1;
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(200, 16);
            this.toolStripProgressBar1.Visible = false;
            // 
            // mainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1505, 814);
            this.Controls.Add(this.tableLayoutPanel_Main);
            this.Controls.Add(this.statusStrip_Main);
            this.Controls.Add(this.menuStrip_Main);
            this.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MainMenuStrip = this.menuStrip_Main;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "mainForm";
            this.Text = "FiOS Health Check";
            this.Load += new System.EventHandler(this.mainForm_Load);
            this.statusStrip_Main.ResumeLayout(false);
            this.statusStrip_Main.PerformLayout();
            this.tableLayoutPanel_Main.ResumeLayout(false);
            this.tableLayoutPanel_Left.ResumeLayout(false);
            this.tableLayoutPanel_Results.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.objectListView_Results)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip_Main;
        private System.Windows.Forms.MenuStrip menuStrip_Main;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel_Main;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel_Left;
        private System.Windows.Forms.Button button_Begin;
        private System.Windows.Forms.Button button_Email;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel_Results;
        private BrightIdeasSoftware.ObjectListView objectListView_Results;
        private BrightIdeasSoftware.OLVColumn olvCol_Servers;
        private BrightIdeasSoftware.OLVColumn olvColumn_Status;
        private BrightIdeasSoftware.OLVColumn olvCol_ErrCount;
        private System.Windows.Forms.WebBrowser webBrowser_Results;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
    }
}

