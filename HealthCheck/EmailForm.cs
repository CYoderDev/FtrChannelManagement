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
        private string _htmlFormatter;

        public EmailForm(string htmlDoc)
        {
            InitializeComponent();
            this.textBox_SendTo.Text = ConfigurationManager.AppSettings.GetValues("DefaultEmail").FirstOrDefault();
            this.textBox_SendTo.TextChanged += new EventHandler(textBox_SendTo_TextChanged);
            this.button_Cancel.Click += new EventHandler(button_Cancel_Click);
            this.button_Send.Click += new EventHandler(button_Send_Click);
            this._htmlFormatter = htmlDoc;
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
            this.button_Cancel.Enabled = false;
            this.label_Status.Text = "Sending...";
            var strSMTPServer = ConfigurationManager.AppSettings.GetValues("SMTPServer").FirstOrDefault();
            try
            {
                Toolset.SendEmail(strSMTPServer, null, null, false, "FiOS Health Check", this._htmlFormatter, "HealthCheck@FiOSProd.net", new string[1] { this.textBox_SendTo.Text }, null);
                this.label_Status.Text += "Sent!";
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
    }
}
