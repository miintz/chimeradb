namespace genericdbgenerator
{
    partial class GenerateDB
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
            this.lblTypeAnalysis = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblNotice = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btn_retrieve = new System.Windows.Forms.Button();
            this.lbl_retrieve = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTypeAnalysis
            // 
            this.lblTypeAnalysis.AutoSize = true;
            this.lblTypeAnalysis.Location = new System.Drawing.Point(164, 30);
            this.lblTypeAnalysis.Name = "lblTypeAnalysis";
            this.lblTypeAnalysis.Size = new System.Drawing.Size(151, 13);
            this.lblTypeAnalysis.TabIndex = 6;
            this.lblTypeAnalysis.Text = "Generate DB with sample data";
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(3, 3);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(140, 42);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Run DB generator";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnStart);
            this.panel1.Location = new System.Drawing.Point(12, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(146, 49);
            this.panel1.TabIndex = 4;
            // 
            // lblNotice
            // 
            this.lblNotice.AutoSize = true;
            this.lblNotice.Location = new System.Drawing.Point(12, 119);
            this.lblNotice.Name = "lblNotice";
            this.lblNotice.Size = new System.Drawing.Size(38, 13);
            this.lblNotice.TabIndex = 7;
            this.lblNotice.Text = "Ready";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btn_retrieve);
            this.panel2.Location = new System.Drawing.Point(12, 67);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(146, 49);
            this.panel2.TabIndex = 5;
            // 
            // btn_retrieve
            // 
            this.btn_retrieve.Enabled = false;
            this.btn_retrieve.Location = new System.Drawing.Point(3, 3);
            this.btn_retrieve.Name = "btn_retrieve";
            this.btn_retrieve.Size = new System.Drawing.Size(140, 42);
            this.btn_retrieve.TabIndex = 1;
            this.btn_retrieve.Text = "Retrieve data";
            this.btn_retrieve.UseVisualStyleBackColor = true;
            this.btn_retrieve.Click += new System.EventHandler(this.btn_retrieve_Click);
            // 
            // lbl_retrieve
            // 
            this.lbl_retrieve.AutoSize = true;
            this.lbl_retrieve.Location = new System.Drawing.Point(164, 85);
            this.lbl_retrieve.Name = "lbl_retrieve";
            this.lbl_retrieve.Size = new System.Drawing.Size(121, 13);
            this.lbl_retrieve.TabIndex = 8;
            this.lbl_retrieve.Text = "Retrieve the saved data";
            // 
            // GenerateDB
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(362, 153);
            this.Controls.Add(this.lbl_retrieve);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.lblNotice);
            this.Controls.Add(this.lblTypeAnalysis);
            this.Controls.Add(this.panel1);
            this.Name = "GenerateDB";
            this.Text = "Generate DB button";
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTypeAnalysis;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblNotice;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btn_retrieve;
        private System.Windows.Forms.Label lbl_retrieve;
    }
}

