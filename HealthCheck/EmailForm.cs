using System;
using System.Collections.Generic;
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
            this.textBox_SendTo.TextChanged += textBox_SendTo_TextChanged;
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
            try
            {
                Toolset.SendEmail("smtp.VHE.FiOSProd.Net", null, null, false, "FiOS Health Check", this._htmlFormatter, "HealthCheck@FiOSProd.net", new string[1] { this.textBox_SendTo.Text }, null);
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

        private void form_Paint(object sender, PaintEventArgs e)
        {
            Form frm = sender as Form;
            ControlPaint.DrawBorder(e.Graphics, frm.ClientRectangle,
                Color.Black, 5, ButtonBorderStyle.Inset,
                Color.Black, 5, ButtonBorderStyle.Inset,
                Color.Black, 5, ButtonBorderStyle.Inset,
                Color.Black, 5, ButtonBorderStyle.Inset);
        }
    }
}
