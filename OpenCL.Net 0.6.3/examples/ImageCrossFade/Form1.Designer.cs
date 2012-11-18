namespace ImageCrossFade
{
    partial class Form1
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
            this.label1 = new System.Windows.Forms.Label();
            this.hScrollBarRatio = new System.Windows.Forms.HScrollBar();
            this.comboBoxDeviceSelector = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Ratio";
            // 
            // hScrollBarRatio
            // 
            this.hScrollBarRatio.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.hScrollBarRatio.LargeChange = 1;
            this.hScrollBarRatio.Location = new System.Drawing.Point(59, 0);
            this.hScrollBarRatio.Name = "hScrollBarRatio";
            this.hScrollBarRatio.Size = new System.Drawing.Size(480, 21);
            this.hScrollBarRatio.TabIndex = 2;
            this.hScrollBarRatio.ValueChanged += new System.EventHandler(this.hScrollBarRatio_ValueChanged);
            // 
            // comboBoxDeviceSelector
            // 
            this.comboBoxDeviceSelector.FormattingEnabled = true;
            this.comboBoxDeviceSelector.Location = new System.Drawing.Point(59, 24);
            this.comboBoxDeviceSelector.Name = "comboBoxDeviceSelector";
            this.comboBoxDeviceSelector.Size = new System.Drawing.Size(480, 21);
            this.comboBoxDeviceSelector.TabIndex = 3;
            this.comboBoxDeviceSelector.SelectedIndexChanged += new System.EventHandler(this.comboBoxDeviceSelector_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 27);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Device";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(548, 557);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBoxDeviceSelector);
            this.Controls.Add(this.hScrollBarRatio);
            this.Controls.Add(this.label1);
            this.DoubleBuffered = true;
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Form1_Paint);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.HScrollBar hScrollBarRatio;
        private System.Windows.Forms.ComboBox comboBoxDeviceSelector;
        private System.Windows.Forms.Label label2;
    }
}

