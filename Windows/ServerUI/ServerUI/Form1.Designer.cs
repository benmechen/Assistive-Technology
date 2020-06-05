namespace ServerUI
{
    partial class fmServer
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
            this.pnTop = new System.Windows.Forms.Panel();
            this.lblName = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnWASD = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnArrows = new System.Windows.Forms.Button();
            this.lstTranscript = new System.Windows.Forms.ListBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.pnTop.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnTop
            // 
            this.pnTop.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.pnTop.Controls.Add(this.lblName);
            this.pnTop.Controls.Add(this.label1);
            this.pnTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnTop.ForeColor = System.Drawing.SystemColors.ControlText;
            this.pnTop.Location = new System.Drawing.Point(0, 0);
            this.pnTop.Name = "pnTop";
            this.pnTop.Size = new System.Drawing.Size(589, 54);
            this.pnTop.TabIndex = 0;
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblName.Location = new System.Drawing.Point(206, 13);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(0, 16);
            this.lblName.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(22, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(178, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Input device connected:";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.btnWASD);
            this.panel1.Location = new System.Drawing.Point(26, 124);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(141, 171);
            this.panel1.TabIndex = 1;
            // 
            // btnWASD
            // 
            this.btnWASD.Location = new System.Drawing.Point(3, 122);
            this.btnWASD.Name = "btnWASD";
            this.btnWASD.Size = new System.Drawing.Size(133, 44);
            this.btnWASD.TabIndex = 3;
            this.btnWASD.Text = "W, A, S, D";
            this.btnWASD.UseVisualStyleBackColor = true;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.btnArrows);
            this.panel2.Location = new System.Drawing.Point(190, 124);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(141, 171);
            this.panel2.TabIndex = 2;
            // 
            // btnArrows
            // 
            this.btnArrows.Location = new System.Drawing.Point(3, 122);
            this.btnArrows.Name = "btnArrows";
            this.btnArrows.Size = new System.Drawing.Size(133, 44);
            this.btnArrows.TabIndex = 4;
            this.btnArrows.Text = "Arrow Keys";
            this.btnArrows.UseVisualStyleBackColor = true;
            // 
            // lstTranscript
            // 
            this.lstTranscript.Dock = System.Windows.Forms.DockStyle.Right;
            this.lstTranscript.FormattingEnabled = true;
            this.lstTranscript.Location = new System.Drawing.Point(371, 54);
            this.lstTranscript.Name = "lstTranscript";
            this.lstTranscript.Size = new System.Drawing.Size(218, 365);
            this.lstTranscript.TabIndex = 3;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(117, 340);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(132, 53);
            this.btnStart.TabIndex = 4;
            this.btnStart.Text = "Start Services";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(23, 94);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Input Types";
            // 
            // fmServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(589, 419);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.lstTranscript);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.pnTop);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "fmServer";
            this.Text = "Assistive technology";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.fmServer_FormClosing);
            this.Load += new System.EventHandler(this.fmServer_Load);
            this.pnTop.ResumeLayout(false);
            this.pnTop.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnTop;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnWASD;
        private System.Windows.Forms.Button btnArrows;
        private System.Windows.Forms.ListBox lstTranscript;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label label3;
    }
}

