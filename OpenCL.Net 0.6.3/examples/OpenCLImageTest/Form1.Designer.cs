namespace OpenCLImageTest
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
            this.buttonScaleImage = new System.Windows.Forms.Button();
            this.groupBoxScaled = new System.Windows.Forms.GroupBox();
            this.panelScaled = new System.Windows.Forms.Panel();
            this.groupBoxOriginal = new System.Windows.Forms.GroupBox();
            this.panelOriginal = new System.Windows.Forms.Panel();
            this.comboBoxOpenCLDevices = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.labelImageSupportIndicator = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBoxOpenCLPlatforms = new System.Windows.Forms.ComboBox();
            this.groupBoxCallBackEvents = new System.Windows.Forms.GroupBox();
            this.textBoxCallBackEvents = new System.Windows.Forms.TextBox();
            this.groupBoxScaled.SuspendLayout();
            this.groupBoxOriginal.SuspendLayout();
            this.groupBoxCallBackEvents.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonScaleImage
            // 
            this.buttonScaleImage.Location = new System.Drawing.Point(753, 455);
            this.buttonScaleImage.Name = "buttonScaleImage";
            this.buttonScaleImage.Size = new System.Drawing.Size(93, 24);
            this.buttonScaleImage.TabIndex = 0;
            this.buttonScaleImage.Text = "Scale Image";
            this.buttonScaleImage.UseVisualStyleBackColor = true;
            this.buttonScaleImage.Click += new System.EventHandler(this.buttonScaleImage_Click);
            // 
            // groupBoxScaled
            // 
            this.groupBoxScaled.Controls.Add(this.panelScaled);
            this.groupBoxScaled.Location = new System.Drawing.Point(262, 0);
            this.groupBoxScaled.Name = "groupBoxScaled";
            this.groupBoxScaled.Size = new System.Drawing.Size(587, 385);
            this.groupBoxScaled.TabIndex = 1;
            this.groupBoxScaled.TabStop = false;
            this.groupBoxScaled.Text = "Scaled";
            // 
            // panelScaled
            // 
            this.panelScaled.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelScaled.Location = new System.Drawing.Point(3, 16);
            this.panelScaled.Name = "panelScaled";
            this.panelScaled.Size = new System.Drawing.Size(581, 366);
            this.panelScaled.TabIndex = 0;
            this.panelScaled.Paint += new System.Windows.Forms.PaintEventHandler(this.panelScaled_Paint);
            // 
            // groupBoxOriginal
            // 
            this.groupBoxOriginal.Controls.Add(this.panelOriginal);
            this.groupBoxOriginal.Location = new System.Drawing.Point(0, 0);
            this.groupBoxOriginal.Name = "groupBoxOriginal";
            this.groupBoxOriginal.Size = new System.Drawing.Size(256, 256);
            this.groupBoxOriginal.TabIndex = 2;
            this.groupBoxOriginal.TabStop = false;
            this.groupBoxOriginal.Text = "Original";
            // 
            // panelOriginal
            // 
            this.panelOriginal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelOriginal.Location = new System.Drawing.Point(3, 16);
            this.panelOriginal.Name = "panelOriginal";
            this.panelOriginal.Size = new System.Drawing.Size(250, 237);
            this.panelOriginal.TabIndex = 0;
            this.panelOriginal.Paint += new System.Windows.Forms.PaintEventHandler(this.panelOriginal_Paint);
            // 
            // comboBoxOpenCLDevices
            // 
            this.comboBoxOpenCLDevices.FormattingEnabled = true;
            this.comboBoxOpenCLDevices.Location = new System.Drawing.Point(86, 429);
            this.comboBoxOpenCLDevices.Name = "comboBoxOpenCLDevices";
            this.comboBoxOpenCLDevices.Size = new System.Drawing.Size(763, 21);
            this.comboBoxOpenCLDevices.TabIndex = 3;
            this.comboBoxOpenCLDevices.SelectedIndexChanged += new System.EventHandler(this.comboBoxOpenCLDevices_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(-3, 432);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "OpenCL Device:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(-3, 461);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Image support:";
            // 
            // labelImageSupportIndicator
            // 
            this.labelImageSupportIndicator.AutoSize = true;
            this.labelImageSupportIndicator.Location = new System.Drawing.Point(83, 461);
            this.labelImageSupportIndicator.Name = "labelImageSupportIndicator";
            this.labelImageSupportIndicator.Size = new System.Drawing.Size(0, 13);
            this.labelImageSupportIndicator.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(-3, 405);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "OpenCL Platform:";
            // 
            // comboBoxOpenCLPlatforms
            // 
            this.comboBoxOpenCLPlatforms.FormattingEnabled = true;
            this.comboBoxOpenCLPlatforms.Location = new System.Drawing.Point(86, 402);
            this.comboBoxOpenCLPlatforms.Name = "comboBoxOpenCLPlatforms";
            this.comboBoxOpenCLPlatforms.Size = new System.Drawing.Size(763, 21);
            this.comboBoxOpenCLPlatforms.TabIndex = 7;
            this.comboBoxOpenCLPlatforms.SelectedIndexChanged += new System.EventHandler(this.comboBoxOpenCLPlatforms_SelectedIndexChanged);
            // 
            // groupBoxCallBackEvents
            // 
            this.groupBoxCallBackEvents.Controls.Add(this.textBoxCallBackEvents);
            this.groupBoxCallBackEvents.Location = new System.Drawing.Point(3, 263);
            this.groupBoxCallBackEvents.Name = "groupBoxCallBackEvents";
            this.groupBoxCallBackEvents.Size = new System.Drawing.Size(249, 118);
            this.groupBoxCallBackEvents.TabIndex = 9;
            this.groupBoxCallBackEvents.TabStop = false;
            this.groupBoxCallBackEvents.Text = "CallBack Events";
            // 
            // textBoxCallBackEvents
            // 
            this.textBoxCallBackEvents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxCallBackEvents.Location = new System.Drawing.Point(3, 16);
            this.textBoxCallBackEvents.Multiline = true;
            this.textBoxCallBackEvents.Name = "textBoxCallBackEvents";
            this.textBoxCallBackEvents.ReadOnly = true;
            this.textBoxCallBackEvents.Size = new System.Drawing.Size(243, 99);
            this.textBoxCallBackEvents.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(853, 485);
            this.Controls.Add(this.groupBoxCallBackEvents);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboBoxOpenCLPlatforms);
            this.Controls.Add(this.labelImageSupportIndicator);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBoxOpenCLDevices);
            this.Controls.Add(this.groupBoxScaled);
            this.Controls.Add(this.buttonScaleImage);
            this.Controls.Add(this.groupBoxOriginal);
            this.Name = "Form1";
            this.Text = "OpenCL Image Test";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBoxScaled.ResumeLayout(false);
            this.groupBoxOriginal.ResumeLayout(false);
            this.groupBoxCallBackEvents.ResumeLayout(false);
            this.groupBoxCallBackEvents.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonScaleImage;
        private System.Windows.Forms.GroupBox groupBoxScaled;
        private System.Windows.Forms.GroupBox groupBoxOriginal;
        private System.Windows.Forms.ComboBox comboBoxOpenCLDevices;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelImageSupportIndicator;
        private System.Windows.Forms.Panel panelScaled;
        private System.Windows.Forms.Panel panelOriginal;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBoxOpenCLPlatforms;
        private System.Windows.Forms.GroupBox groupBoxCallBackEvents;
        private System.Windows.Forms.TextBox textBoxCallBackEvents;
    }
}

