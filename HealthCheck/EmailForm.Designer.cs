namespace HealthCheck
{
    partial class EmailForm
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_SendTo = new System.Windows.Forms.TextBox();
            this.panel_Buttons = new System.Windows.Forms.Panel();
            this.button_Send = new System.Windows.Forms.Button();
            this.button_Cancel = new System.Windows.Forms.Button();
            this.label_Status = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.panel_Buttons.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.textBox_SendTo);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(40, 30);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(396, 62);
            this.panel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Gainsboro;
            this.label1.Location = new System.Drawing.Point(12, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 19);
            this.label1.TabIndex = 0;
            this.label1.Text = "Send To:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // textBox_SendTo
            // 
            this.textBox_SendTo.AutoCompleteCustomSource.AddRange(new string[] {
            "FiOS.Operations@ftr.com",
            "IMGTeam@ftr.com",
            "cameron.yoder@ftr.com"});
            this.textBox_SendTo.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.textBox_SendTo.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.HistoryList;
            this.textBox_SendTo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_SendTo.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_SendTo.ForeColor = System.Drawing.Color.Green;
            this.textBox_SendTo.Location = new System.Drawing.Point(96, 18);
            this.textBox_SendTo.Name = "textBox_SendTo";
            this.textBox_SendTo.Size = new System.Drawing.Size(283, 26);
            this.textBox_SendTo.TabIndex = 1;
            this.textBox_SendTo.Text = "FiOS.Operations@ftr.com";
            this.textBox_SendTo.WordWrap = false;
            // 
            // panel_Buttons
            // 
            this.panel_Buttons.Controls.Add(this.button_Cancel);
            this.panel_Buttons.Controls.Add(this.button_Send);
            this.panel_Buttons.Location = new System.Drawing.Point(12, 98);
            this.panel_Buttons.Name = "panel_Buttons";
            this.panel_Buttons.Size = new System.Drawing.Size(456, 83);
            this.panel_Buttons.TabIndex = 1;
            // 
            // button_Send
            // 
            this.button_Send.BackColor = System.Drawing.Color.DarkSlateGray;
            this.button_Send.Font = new System.Drawing.Font("Arial", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_Send.ForeColor = System.Drawing.Color.Ivory;
            this.button_Send.Location = new System.Drawing.Point(167, 3);
            this.button_Send.Name = "button_Send";
            this.button_Send.Size = new System.Drawing.Size(115, 41);
            this.button_Send.TabIndex = 0;
            this.button_Send.Text = "Send";
            this.button_Send.UseVisualStyleBackColor = false;
            // 
            // button_Cancel
            // 
            this.button_Cancel.BackColor = System.Drawing.Color.Maroon;
            this.button_Cancel.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_Cancel.ForeColor = System.Drawing.Color.Ivory;
            this.button_Cancel.Location = new System.Drawing.Point(182, 50);
            this.button_Cancel.Name = "button_Cancel";
            this.button_Cancel.Size = new System.Drawing.Size(81, 30);
            this.button_Cancel.TabIndex = 1;
            this.button_Cancel.Text = "Cancel";
            this.button_Cancel.UseVisualStyleBackColor = false;
            // 
            // label_Status
            // 
            this.label_Status.AutoSize = true;
            this.label_Status.Font = new System.Drawing.Font("Arial Narrow", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Status.ForeColor = System.Drawing.Color.Ivory;
            this.label_Status.Location = new System.Drawing.Point(236, 11);
            this.label_Status.Name = "label_Status";
            this.label_Status.Size = new System.Drawing.Size(0, 16);
            this.label_Status.TabIndex = 2;
            this.label_Status.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // EmailForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.SteelBlue;
            this.ClientSize = new System.Drawing.Size(480, 204);
            this.ControlBox = false;
            this.Controls.Add(this.label_Status);
            this.Controls.Add(this.panel_Buttons);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "EmailForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Send Email";
            this.TopMost = true;
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel_Buttons.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox textBox_SendTo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel_Buttons;
        private System.Windows.Forms.Button button_Cancel;
        private System.Windows.Forms.Button button_Send;
        private System.Windows.Forms.Label label_Status;
    }
}