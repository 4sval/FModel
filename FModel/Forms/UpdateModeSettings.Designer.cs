namespace FModel.Forms
{
    partial class UpdateModeSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateModeSettings));
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.optionsOKButton = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.checkBox8 = new System.Windows.Forms.CheckBox();
            this.checkBox7 = new System.Windows.Forms.CheckBox();
            this.filenameLabel = new System.Windows.Forms.Label();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.label9 = new System.Windows.Forms.Label();
            this.trackBar2 = new System.Windows.Forms.TrackBar();
            this.label8 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.wPictureBox = new System.Windows.Forms.PictureBox();
            this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
            this.checkedListBox2 = new System.Windows.Forms.CheckedListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.wPictureBox)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.Color.Red;
            this.label2.Location = new System.Drawing.Point(149, 443);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(320, 13);
            this.label2.TabIndex = 20;
            this.label2.Text = "The more Assets you choose, the more time it will take to complete";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(133, 442);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(19, 13);
            this.label1.TabIndex = 19;
            this.label1.Text = "⚠️";
            // 
            // optionsOKButton
            // 
            this.optionsOKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.optionsOKButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.optionsOKButton.Location = new System.Drawing.Point(475, 439);
            this.optionsOKButton.Name = "optionsOKButton";
            this.optionsOKButton.Size = new System.Drawing.Size(97, 21);
            this.optionsOKButton.TabIndex = 18;
            this.optionsOKButton.Text = "OK";
            this.optionsOKButton.UseVisualStyleBackColor = true;
            this.optionsOKButton.Click += new System.EventHandler(this.optionsOKButton_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.checkBox1);
            this.groupBox4.Controls.Add(this.comboBox1);
            this.groupBox4.Controls.Add(this.label10);
            this.groupBox4.Controls.Add(this.checkBox8);
            this.groupBox4.Controls.Add(this.checkBox7);
            this.groupBox4.Controls.Add(this.filenameLabel);
            this.groupBox4.Controls.Add(this.trackBar1);
            this.groupBox4.Controls.Add(this.label9);
            this.groupBox4.Controls.Add(this.trackBar2);
            this.groupBox4.Controls.Add(this.label8);
            this.groupBox4.Controls.Add(this.button1);
            this.groupBox4.Controls.Add(this.wPictureBox);
            this.groupBox4.Location = new System.Drawing.Point(12, 232);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(560, 195);
            this.groupBox4.TabIndex = 21;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Icon Creation";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(244, 54);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(139, 17);
            this.checkBox1.TabIndex = 30;
            this.checkBox1.Text = "New Rarity Background";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.CheckBox1_CheckedChanged);
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "English",
            "French",
            "German",
            "Italian",
            "Spanish",
            "Spanish (LA)",
            "Arabic",
            "Japanese",
            "Korean",
            "Polish",
            "Portuguese (Brazil)",
            "Russian",
            "Turkish",
            "Chinese (S)",
            "Traditional Chinese"});
            this.comboBox1.Location = new System.Drawing.Point(70, 19);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(309, 21);
            this.comboBox1.TabIndex = 29;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 22);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(58, 13);
            this.label10.TabIndex = 28;
            this.label10.Text = "Language:";
            // 
            // checkBox8
            // 
            this.checkBox8.AutoSize = true;
            this.checkBox8.Location = new System.Drawing.Point(6, 54);
            this.checkBox8.Name = "checkBox8";
            this.checkBox8.Size = new System.Drawing.Size(177, 17);
            this.checkBox8.TabIndex = 26;
            this.checkBox8.Text = "Use Featured Image If Available";
            this.checkBox8.UseVisualStyleBackColor = true;
            this.checkBox8.CheckedChanged += new System.EventHandler(this.checkBox8_CheckedChanged);
            // 
            // checkBox7
            // 
            this.checkBox7.AutoSize = true;
            this.checkBox7.Location = new System.Drawing.Point(6, 90);
            this.checkBox7.Name = "checkBox7";
            this.checkBox7.Size = new System.Drawing.Size(15, 14);
            this.checkBox7.TabIndex = 25;
            this.checkBox7.UseVisualStyleBackColor = true;
            this.checkBox7.CheckedChanged += new System.EventHandler(this.checkBox7_CheckedChanged);
            // 
            // filenameLabel
            // 
            this.filenameLabel.AutoEllipsis = true;
            this.filenameLabel.AutoSize = true;
            this.filenameLabel.Location = new System.Drawing.Point(115, 91);
            this.filenameLabel.MaximumSize = new System.Drawing.Size(260, 13);
            this.filenameLabel.Name = "filenameLabel";
            this.filenameLabel.Size = new System.Drawing.Size(60, 13);
            this.filenameLabel.TabIndex = 24;
            this.filenameLabel.Text = "File Name: ";
            // 
            // trackBar1
            // 
            this.trackBar1.Location = new System.Drawing.Point(58, 144);
            this.trackBar1.Maximum = 100;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(325, 45);
            this.trackBar1.TabIndex = 20;
            this.trackBar1.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackBar1.ValueChanged += new System.EventHandler(this.trackBar1_ValueChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(22, 115);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(30, 13);
            this.label9.TabIndex = 23;
            this.label9.Text = "Size:";
            // 
            // trackBar2
            // 
            this.trackBar2.Location = new System.Drawing.Point(58, 115);
            this.trackBar2.Maximum = 1000;
            this.trackBar2.Minimum = 1;
            this.trackBar2.Name = "trackBar2";
            this.trackBar2.Size = new System.Drawing.Size(325, 45);
            this.trackBar2.TabIndex = 22;
            this.trackBar2.TickFrequency = 25;
            this.trackBar2.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackBar2.Value = 1;
            this.trackBar2.ValueChanged += new System.EventHandler(this.trackBar2_ValueChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 147);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(46, 13);
            this.label8.TabIndex = 21;
            this.label8.Text = "Opacity:";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(27, 84);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(82, 24);
            this.button1.TabIndex = 19;
            this.button1.Text = "Watermark";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // wPictureBox
            // 
            this.wPictureBox.Image = global::FModel.Properties.Resources.wTemplate;
            this.wPictureBox.Location = new System.Drawing.Point(389, 19);
            this.wPictureBox.Name = "wPictureBox";
            this.wPictureBox.Size = new System.Drawing.Size(165, 165);
            this.wPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.wPictureBox.TabIndex = 18;
            this.wPictureBox.TabStop = false;
            // 
            // checkedListBox1
            // 
            this.checkedListBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkedListBox1.CheckOnClick = true;
            this.checkedListBox1.FormattingEnabled = true;
            this.checkedListBox1.Items.AddRange(new object[] {
            "[BR] Cosmetics",
            "[BR] Cosmetics Variants",
            "[BR] Banners",
            "[BR] Challenges",
            "[BR] Consumables",
            "[BR] Gadgets",
            "[BR] Traps",
            "[BR] Weapons",
            "[STW] Heroes",
            "[STW] Defenders",
            "[STW] Workers",
            "[STW] Schematics",
            "[STW] Traps",
            "[STW] Weapons",
            "[STW] Ingredients",
            "[STW] Persistent Resources",
            "[STW] CardPacks",
            "Tokens"});
            this.checkedListBox1.Location = new System.Drawing.Point(6, 22);
            this.checkedListBox1.Name = "checkedListBox1";
            this.checkedListBox1.Size = new System.Drawing.Size(270, 184);
            this.checkedListBox1.TabIndex = 22;
            // 
            // checkedListBox2
            // 
            this.checkedListBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkedListBox2.CheckOnClick = true;
            this.checkedListBox2.FormattingEnabled = true;
            this.checkedListBox2.Items.AddRange(new object[] {
            "[BR] 2D Assets",
            "[BR] Featured Images",
            "Icons",
            "Additional Banners",
            "Additional Loading Screens"});
            this.checkedListBox2.Location = new System.Drawing.Point(284, 22);
            this.checkedListBox2.Name = "checkedListBox2";
            this.checkedListBox2.Size = new System.Drawing.Size(270, 184);
            this.checkedListBox2.TabIndex = 23;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkedListBox2);
            this.groupBox1.Controls.Add(this.checkedListBox1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(560, 214);
            this.groupBox1.TabIndex = 24;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Icon Creation                                                                    " +
    "   Texture";
            // 
            // UpdateModeSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 472);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.optionsOKButton);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateModeSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Update Mode Settings";
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.wPictureBox)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button optionsOKButton;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckBox checkBox8;
        private System.Windows.Forms.CheckBox checkBox7;
        private System.Windows.Forms.Label filenameLabel;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TrackBar trackBar2;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.PictureBox wPictureBox;
        private System.Windows.Forms.CheckedListBox checkedListBox1;
        private System.Windows.Forms.CheckedListBox checkedListBox2;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}
