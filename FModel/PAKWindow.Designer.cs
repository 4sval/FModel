namespace FModel
{
    partial class PAKWindow
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PAKWindow));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.PAKsLoad = new System.Windows.Forms.Button();
            this.AESKeyLabel = new System.Windows.Forms.Label();
            this.AESKeyTextBox = new System.Windows.Forms.TextBox();
            this.PAKsComboBox = new System.Windows.Forms.ComboBox();
            this.ItemsListBox = new System.Windows.Forms.ListBox();
            this.PAKTreeView = new System.Windows.Forms.TreeView();
            this.ItemIconPictureBox = new System.Windows.Forms.PictureBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.SaveImageCheckBox = new System.Windows.Forms.CheckBox();
            this.SaveImageButton = new System.Windows.Forms.Button();
            this.LoadDataCheckBox = new System.Windows.Forms.CheckBox();
            this.ConsoleRichTextBox = new System.Windows.Forms.RichTextBox();
            this.ExtractButton = new System.Windows.Forms.Button();
            this.ItemRichTextBox = new System.Windows.Forms.RichTextBox();
            this.FilterLabel = new System.Windows.Forms.Label();
            this.FilterTextBox = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ItemIconPictureBox)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.PAKsLoad);
            this.groupBox1.Controls.Add(this.AESKeyLabel);
            this.groupBox1.Controls.Add(this.AESKeyTextBox);
            this.groupBox1.Controls.Add(this.PAKsComboBox);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(408, 75);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "PAK";
            // 
            // PAKsLoad
            // 
            this.PAKsLoad.Location = new System.Drawing.Point(296, 18);
            this.PAKsLoad.Name = "PAKsLoad";
            this.PAKsLoad.Size = new System.Drawing.Size(106, 23);
            this.PAKsLoad.TabIndex = 3;
            this.PAKsLoad.Text = "Load";
            this.PAKsLoad.UseVisualStyleBackColor = true;
            this.PAKsLoad.Click += new System.EventHandler(this.PAKsLoad_Click);
            // 
            // AESKeyLabel
            // 
            this.AESKeyLabel.AutoSize = true;
            this.AESKeyLabel.Location = new System.Drawing.Point(6, 51);
            this.AESKeyLabel.Name = "AESKeyLabel";
            this.AESKeyLabel.Size = new System.Drawing.Size(52, 13);
            this.AESKeyLabel.TabIndex = 2;
            this.AESKeyLabel.Text = "AES Key:";
            // 
            // AESKeyTextBox
            // 
            this.AESKeyTextBox.Location = new System.Drawing.Point(64, 47);
            this.AESKeyTextBox.Name = "AESKeyTextBox";
            this.AESKeyTextBox.Size = new System.Drawing.Size(338, 20);
            this.AESKeyTextBox.TabIndex = 1;
            this.AESKeyTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // PAKsComboBox
            // 
            this.PAKsComboBox.FormattingEnabled = true;
            this.PAKsComboBox.Location = new System.Drawing.Point(6, 19);
            this.PAKsComboBox.Name = "PAKsComboBox";
            this.PAKsComboBox.Size = new System.Drawing.Size(284, 21);
            this.PAKsComboBox.TabIndex = 0;
            // 
            // ItemsListBox
            // 
            this.ItemsListBox.FormattingEnabled = true;
            this.ItemsListBox.Location = new System.Drawing.Point(12, 389);
            this.ItemsListBox.Name = "ItemsListBox";
            this.ItemsListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.ItemsListBox.Size = new System.Drawing.Size(408, 290);
            this.ItemsListBox.Sorted = true;
            this.ItemsListBox.TabIndex = 1;
            this.ItemsListBox.SelectedIndexChanged += new System.EventHandler(this.ItemsListBox_SelectedIndexChanged);
            // 
            // PAKTreeView
            // 
            this.PAKTreeView.Location = new System.Drawing.Point(12, 93);
            this.PAKTreeView.Name = "PAKTreeView";
            this.PAKTreeView.Size = new System.Drawing.Size(408, 290);
            this.PAKTreeView.TabIndex = 2;
            this.PAKTreeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.PAKTreeView_NodeMouseClick);
            // 
            // ItemIconPictureBox
            // 
            this.ItemIconPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ItemIconPictureBox.Location = new System.Drawing.Point(572, 18);
            this.ItemIconPictureBox.Name = "ItemIconPictureBox";
            this.ItemIconPictureBox.Size = new System.Drawing.Size(350, 350);
            this.ItemIconPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.ItemIconPictureBox.TabIndex = 3;
            this.ItemIconPictureBox.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.SaveImageCheckBox);
            this.groupBox2.Controls.Add(this.SaveImageButton);
            this.groupBox2.Controls.Add(this.LoadDataCheckBox);
            this.groupBox2.Controls.Add(this.ConsoleRichTextBox);
            this.groupBox2.Controls.Add(this.ExtractButton);
            this.groupBox2.Controls.Add(this.ItemRichTextBox);
            this.groupBox2.Controls.Add(this.ItemIconPictureBox);
            this.groupBox2.Location = new System.Drawing.Point(426, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(928, 693);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            // 
            // SaveImageCheckBox
            // 
            this.SaveImageCheckBox.AutoSize = true;
            this.SaveImageCheckBox.Location = new System.Drawing.Point(703, 639);
            this.SaveImageCheckBox.Name = "SaveImageCheckBox";
            this.SaveImageCheckBox.Size = new System.Drawing.Size(108, 17);
            this.SaveImageCheckBox.TabIndex = 9;
            this.SaveImageCheckBox.Text = "Auto Save Image";
            this.SaveImageCheckBox.UseVisualStyleBackColor = true;
            this.SaveImageCheckBox.CheckedChanged += new System.EventHandler(this.SaveImageCheckBox_CheckedChanged);
            // 
            // SaveImageButton
            // 
            this.SaveImageButton.Location = new System.Drawing.Point(816, 635);
            this.SaveImageButton.Name = "SaveImageButton";
            this.SaveImageButton.Size = new System.Drawing.Size(106, 23);
            this.SaveImageButton.TabIndex = 8;
            this.SaveImageButton.Text = "Save Image";
            this.SaveImageButton.UseVisualStyleBackColor = true;
            this.SaveImageButton.Click += new System.EventHandler(this.SaveImageButton_Click);
            // 
            // LoadDataCheckBox
            // 
            this.LoadDataCheckBox.AutoSize = true;
            this.LoadDataCheckBox.Location = new System.Drawing.Point(703, 668);
            this.LoadDataCheckBox.Name = "LoadDataCheckBox";
            this.LoadDataCheckBox.Size = new System.Drawing.Size(101, 17);
            this.LoadDataCheckBox.TabIndex = 7;
            this.LoadDataCheckBox.Text = "Auto Load Data";
            this.LoadDataCheckBox.UseVisualStyleBackColor = true;
            this.LoadDataCheckBox.CheckedChanged += new System.EventHandler(this.LoadImageCheckBox_CheckedChanged);
            // 
            // ConsoleRichTextBox
            // 
            this.ConsoleRichTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ConsoleRichTextBox.Location = new System.Drawing.Point(6, 374);
            this.ConsoleRichTextBox.Name = "ConsoleRichTextBox";
            this.ConsoleRichTextBox.ReadOnly = true;
            this.ConsoleRichTextBox.Size = new System.Drawing.Size(922, 229);
            this.ConsoleRichTextBox.TabIndex = 6;
            this.ConsoleRichTextBox.Text = "";
            // 
            // ExtractButton
            // 
            this.ExtractButton.Location = new System.Drawing.Point(816, 664);
            this.ExtractButton.Name = "ExtractButton";
            this.ExtractButton.Size = new System.Drawing.Size(106, 23);
            this.ExtractButton.TabIndex = 5;
            this.ExtractButton.Text = "Extract";
            this.ExtractButton.UseVisualStyleBackColor = true;
            this.ExtractButton.Click += new System.EventHandler(this.ExtractButton_Click);
            // 
            // ItemRichTextBox
            // 
            this.ItemRichTextBox.BackColor = System.Drawing.SystemColors.Window;
            this.ItemRichTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ItemRichTextBox.Location = new System.Drawing.Point(6, 18);
            this.ItemRichTextBox.Name = "ItemRichTextBox";
            this.ItemRichTextBox.ReadOnly = true;
            this.ItemRichTextBox.Size = new System.Drawing.Size(560, 350);
            this.ItemRichTextBox.TabIndex = 4;
            this.ItemRichTextBox.Text = "";
            // 
            // FilterLabel
            // 
            this.FilterLabel.AutoSize = true;
            this.FilterLabel.Location = new System.Drawing.Point(18, 688);
            this.FilterLabel.Name = "FilterLabel";
            this.FilterLabel.Size = new System.Drawing.Size(32, 13);
            this.FilterLabel.TabIndex = 6;
            this.FilterLabel.Text = "Filter:";
            // 
            // FilterTextBox
            // 
            this.FilterTextBox.Location = new System.Drawing.Point(56, 685);
            this.FilterTextBox.Name = "FilterTextBox";
            this.FilterTextBox.Size = new System.Drawing.Size(364, 20);
            this.FilterTextBox.TabIndex = 5;
            this.FilterTextBox.TextChanged += new System.EventHandler(this.FilterTextBox_TextChanged);
            // 
            // PAKWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1366, 712);
            this.Controls.Add(this.FilterLabel);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.PAKTreeView);
            this.Controls.Add(this.FilterTextBox);
            this.Controls.Add(this.ItemsListBox);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "PAKWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FModel";
            this.Load += new System.EventHandler(this.PAKWindow_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ItemIconPictureBox)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button PAKsLoad;
        private System.Windows.Forms.Label AESKeyLabel;
        private System.Windows.Forms.TextBox AESKeyTextBox;
        private System.Windows.Forms.ComboBox PAKsComboBox;
        private System.Windows.Forms.ListBox ItemsListBox;
        private System.Windows.Forms.TreeView PAKTreeView;
        private System.Windows.Forms.PictureBox ItemIconPictureBox;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button ExtractButton;
        private System.Windows.Forms.RichTextBox ItemRichTextBox;
        private System.Windows.Forms.RichTextBox ConsoleRichTextBox;
        private System.Windows.Forms.CheckBox LoadDataCheckBox;
        private System.Windows.Forms.CheckBox SaveImageCheckBox;
        private System.Windows.Forms.Button SaveImageButton;
        private System.Windows.Forms.Label FilterLabel;
        private System.Windows.Forms.TextBox FilterTextBox;
    }
}

