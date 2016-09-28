using System;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using FrontierVOps.Common;
using FrontierVOps.FiOS.HealthCheck.DataObjects;

namespace HealthCheck
{
    public partial class EmailForm : Form
    {
        public string HTMLFormat { get { return this._htmlFormatter; } internal set { this._htmlFormatter = value; } }
        private string _htmlFormatter;

        public event EventHandler EmailSent;

        private mainForm _parentForm;
        private Timer _timer;

        public EmailForm(string htmlDoc, Form parentForm)
            :this(parentForm)
        {          
            this._htmlFormatter = htmlDoc;
        }

        public EmailForm(Form parentForm)
        {
            try
            {
                InitializeComponent();
                this._timer = new Timer();

                this._parentForm = parentForm as mainForm;

                if (this._parentForm == null)
                    throw new ArgumentException("Incorrect parent form type.");

                this.textBox_SendTo.Text = ConfigurationManager.AppSettings.GetValues("DefaultEmail").FirstOrDefault();
                this.textBox_SendTo.TextChanged += new EventHandler(textBox_SendTo_TextChanged);
                this.button_Cancel.Click += new EventHandler(button_Cancel_Click);
                this.button_Send.Click += new EventHandler(button_Send_Click);
                if (this._parentForm.bw.IsBusy)
                {
                    this.label_Status.Text = "Will send once running health check is complete.";
                    this.button_Send.Text = "Ok";
                }
            }
            catch (Exception ex)
            {
                mainForm.writeError(string.Format("Failed during email form initialization. {0}", ex.Message), 10, System.Diagnostics.TraceEventType.Error);
            }
        }

        private void textBox_SendTo_TextChanged(object sender, EventArgs e)
        {
            var tb = sender as TextBox;
            if (Regex.IsMatch(tb.Text, @"^([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,5})$", RegexOptions.IgnoreCase))
            {
                tb.ForeColor = Color.Green;
                this.button_Send.BackColor = Color.DarkSlateGray;
                this.button_Send.Enabled = true;
            }
            else
            {
                tb.ForeColor = Color.Red;
                this.button_Send.BackColor = Color.LightGray;
                this.button_Send.Enabled = false;
            }
        }

        private void button_Send_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            btn.Enabled = false;
            if (this._parentForm.bw.IsBusy)
            {
                this.Visible = false;
                beginTimer();
                return;
            }
            this.button_Cancel.Enabled = false;
            this.label_Status.Text = "Sending...";
            var strSMTPServer = ConfigurationManager.AppSettings.GetValues("SMTPServer").FirstOrDefault();
            try
            {
                Toolset.SendEmail(strSMTPServer, null, null, false, "FiOS Health Check", this._htmlFormatter, "HealthCheck@FiOSProd.net", new string[1] { this.textBox_SendTo.Text }, null);
                this.label_Status.Text += "Sent!";
                OnEmailSent(EventArgs.Empty);
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                this.label_Status.Text += string.Format("Failed! - {0}{1}Please try again...", ex.Message, Environment.NewLine);
            }
            btn.Enabled = true;
            this.button_Cancel.Enabled = true;
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle,
                Color.Black, 5, ButtonBorderStyle.Inset,
                Color.Black, 5, ButtonBorderStyle.Inset,
                Color.Black, 5, ButtonBorderStyle.Inset,
                Color.Black, 5, ButtonBorderStyle.Inset);
        }

        private void beginTimer()
        {
            _timer.Enabled = true;
            _timer.Interval = 3000;
            _timer.Tick += (obj, sender) =>
                {
                    if (!this._parentForm.bw.IsBusy)
                    {
                        if (this._parentForm.healthCheckComplete)
                        {
                            this.HTMLFormat = this._parentForm.setHTMLSelectView();
                            this.button_Send_Click(this.button_Send, null);
                        }
                        _timer.Enabled = false;
                    }
                };
        }

        protected virtual void OnEmailSent(EventArgs e)
        {
            EventHandler handler = EmailSent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

    }
}
