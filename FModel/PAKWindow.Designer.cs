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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PAKWindow));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.LoadButton = new FModel.SplitButton();
            this.LoadContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.loadAllPAKsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.backupCurrentPAKsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enablePAKsDiffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AESKeyLabel = new System.Windows.Forms.Label();
            this.AESKeyTextBox = new System.Windows.Forms.TextBox();
            this.PAKsComboBox = new System.Windows.Forms.ComboBox();
            this.ItemsListBox = new System.Windows.Forms.ListBox();
            this.PAKTreeView = new System.Windows.Forms.TreeView();
            this.TreeViewImageList = new System.Windows.Forms.ImageList(this.components);
            this.ItemIconPictureBox = new System.Windows.Forms.PictureBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.scintilla1 = new ScintillaNET.Scintilla();
            this.OpenImageButton = new FModel.SplitButton();
            this.ImageContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ImageSaveTS = new System.Windows.Forms.ToolStripMenuItem();
            this.ExtractAssetButton = new FModel.SplitButton();
            this.ExtractAsset = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.LoadDataTS = new System.Windows.Forms.ToolStripMenuItem();
            this.SaveImageTS = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.mergeGeneratedImagesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ConsoleRichTextBox = new System.Windows.Forms.RichTextBox();
            this.FilterLabel = new System.Windows.Forms.Label();
            this.FilterTextBox = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.LoadContext.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ItemIconPictureBox)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.ImageContext.SuspendLayout();
            this.ExtractAsset.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.LoadButton);
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
            // LoadButton
            // 
            this.LoadButton.Location = new System.Drawing.Point(296, 18);
            this.LoadButton.Menu = this.LoadContext;
            this.LoadButton.Name = "LoadButton";
            this.LoadButton.Size = new System.Drawing.Size(106, 23);
            this.LoadButton.TabIndex = 12;
            this.LoadButton.Text = "         Load";
            this.LoadButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.LoadButton.UseVisualStyleBackColor = true;
            this.LoadButton.Click += new System.EventHandler(this.LoadButton_Click);
            // 
            // LoadContext
            // 
            this.LoadContext.ImageScalingSize = new System.Drawing.Size(18, 18);
            this.LoadContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadAllPAKsToolStripMenuItem,
            this.backupCurrentPAKsToolStripMenuItem,
            this.enablePAKsDiffToolStripMenuItem,
            this.toolStripSeparator3,
            this.optionsToolStripMenuItem});
            this.LoadContext.Name = "LoadContext";
            this.LoadContext.Size = new System.Drawing.Size(186, 98);
            // 
            // loadAllPAKsToolStripMenuItem
            // 
            this.loadAllPAKsToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.loadAllPAKsToolStripMenuItem.Name = "loadAllPAKsToolStripMenuItem";
            this.loadAllPAKsToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.loadAllPAKsToolStripMenuItem.Text = "Load All PAKs";
            this.loadAllPAKsToolStripMenuItem.Click += new System.EventHandler(this.loadAllPAKsToolStripMenuItem_Click);
            // 
            // backupCurrentPAKsToolStripMenuItem
            // 
            this.backupCurrentPAKsToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.backupCurrentPAKsToolStripMenuItem.Name = "backupCurrentPAKsToolStripMenuItem";
            this.backupCurrentPAKsToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.backupCurrentPAKsToolStripMenuItem.Text = "Backup Current PAKs";
            this.backupCurrentPAKsToolStripMenuItem.Click += new System.EventHandler(this.backupCurrentPAKsToolStripMenuItem_Click);
            // 
            // enablePAKsDiffToolStripMenuItem
            // 
            this.enablePAKsDiffToolStripMenuItem.CheckOnClick = true;
            this.enablePAKsDiffToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.enablePAKsDiffToolStripMenuItem.Name = "enablePAKsDiffToolStripMenuItem";
            this.enablePAKsDiffToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.enablePAKsDiffToolStripMenuItem.Text = "Enable PAKs Diff";
            this.enablePAKsDiffToolStripMenuItem.CheckedChanged += new System.EventHandler(this.enablePAKsDiffToolStripMenuItem_CheckedChanged);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(182, 6);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.optionsToolStripMenuItem.Text = "Options";
            this.optionsToolStripMenuItem.Click += new System.EventHandler(this.optionsToolStripMenuItem_Click);
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
            this.PAKTreeView.ImageIndex = 0;
            this.PAKTreeView.ImageList = this.TreeViewImageList;
            this.PAKTreeView.Location = new System.Drawing.Point(12, 93);
            this.PAKTreeView.Name = "PAKTreeView";
            this.PAKTreeView.SelectedImageIndex = 1;
            this.PAKTreeView.ShowLines = false;
            this.PAKTreeView.Size = new System.Drawing.Size(408, 290);
            this.PAKTreeView.TabIndex = 2;
            this.PAKTreeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.PAKTreeView_NodeMouseClick);
            // 
            // TreeViewImageList
            // 
            this.TreeViewImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("TreeViewImageList.ImageStream")));
            this.TreeViewImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.TreeViewImageList.Images.SetKeyName(0, "folder.png");
            this.TreeViewImageList.Images.SetKeyName(1, "folder_open.png");
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
            this.groupBox2.Controls.Add(this.scintilla1);
            this.groupBox2.Controls.Add(this.OpenImageButton);
            this.groupBox2.Controls.Add(this.ExtractAssetButton);
            this.groupBox2.Controls.Add(this.ConsoleRichTextBox);
            this.groupBox2.Controls.Add(this.ItemIconPictureBox);
            this.groupBox2.Location = new System.Drawing.Point(426, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(928, 693);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            // 
            // scintilla1
            // 
            this.scintilla1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.scintilla1.IndentationGuides = ScintillaNET.IndentView.Real;
            this.scintilla1.Location = new System.Drawing.Point(6, 18);
            this.scintilla1.Name = "scintilla1";
            this.scintilla1.ScrollWidth = 539;
            this.scintilla1.Size = new System.Drawing.Size(560, 350);
            this.scintilla1.TabIndex = 12;
            // 
            // OpenImageButton
            // 
            this.OpenImageButton.Location = new System.Drawing.Point(810, 635);
            this.OpenImageButton.Menu = this.ImageContext;
            this.OpenImageButton.Name = "OpenImageButton";
            this.OpenImageButton.Size = new System.Drawing.Size(112, 23);
            this.OpenImageButton.TabIndex = 11;
            this.OpenImageButton.Text = "    Open Image";
            this.OpenImageButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.OpenImageButton.UseVisualStyleBackColor = true;
            this.OpenImageButton.Click += new System.EventHandler(this.OpenImageButton_Click);
            // 
            // ImageContext
            // 
            this.ImageContext.ImageScalingSize = new System.Drawing.Size(18, 18);
            this.ImageContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ImageSaveTS});
            this.ImageContext.Name = "ImageContext";
            this.ImageContext.Size = new System.Drawing.Size(181, 48);
            // 
            // ImageSaveTS
            // 
            this.ImageSaveTS.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ImageSaveTS.Name = "ImageSaveTS";
            this.ImageSaveTS.Size = new System.Drawing.Size(180, 22);
            this.ImageSaveTS.Text = "Save Image";
            this.ImageSaveTS.Click += new System.EventHandler(this.ImageSaveTS_Click);
            // 
            // ExtractAssetButton
            // 
            this.ExtractAssetButton.Location = new System.Drawing.Point(810, 664);
            this.ExtractAssetButton.Menu = this.ExtractAsset;
            this.ExtractAssetButton.Name = "ExtractAssetButton";
            this.ExtractAssetButton.Size = new System.Drawing.Size(112, 23);
            this.ExtractAssetButton.TabIndex = 10;
            this.ExtractAssetButton.Text = "   Extract Asset";
            this.ExtractAssetButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ExtractAssetButton.UseVisualStyleBackColor = true;
            this.ExtractAssetButton.Click += new System.EventHandler(this.ExtractAssetButton_Click);
            // 
            // ExtractAsset
            // 
            this.ExtractAsset.ImageScalingSize = new System.Drawing.Size(18, 18);
            this.ExtractAsset.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LoadDataTS,
            this.SaveImageTS,
            this.toolStripSeparator1,
            this.mergeGeneratedImagesToolStripMenuItem});
            this.ExtractAsset.Name = "ExtractAsset";
            this.ExtractAsset.Size = new System.Drawing.Size(223, 76);
            // 
            // LoadDataTS
            // 
            this.LoadDataTS.Checked = true;
            this.LoadDataTS.CheckOnClick = true;
            this.LoadDataTS.CheckState = System.Windows.Forms.CheckState.Checked;
            this.LoadDataTS.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.LoadDataTS.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.LoadDataTS.Name = "LoadDataTS";
            this.LoadDataTS.Size = new System.Drawing.Size(222, 22);
            this.LoadDataTS.Text = "Load Data After Serialization";
            // 
            // SaveImageTS
            // 
            this.SaveImageTS.CheckOnClick = true;
            this.SaveImageTS.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.SaveImageTS.Name = "SaveImageTS";
            this.SaveImageTS.Size = new System.Drawing.Size(222, 22);
            this.SaveImageTS.Text = "Auto Save Generated Image";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(219, 6);
            // 
            // mergeGeneratedImagesToolStripMenuItem
            // 
            this.mergeGeneratedImagesToolStripMenuItem.Name = "mergeGeneratedImagesToolStripMenuItem";
            this.mergeGeneratedImagesToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.mergeGeneratedImagesToolStripMenuItem.Text = "Merge Generated Images";
            this.mergeGeneratedImagesToolStripMenuItem.Click += new System.EventHandler(this.mergeGeneratedImagesToolStripMenuItem_Click);
            // 
            // ConsoleRichTextBox
            // 
            this.ConsoleRichTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ConsoleRichTextBox.Location = new System.Drawing.Point(6, 374);
            this.ConsoleRichTextBox.Name = "ConsoleRichTextBox";
            this.ConsoleRichTextBox.ReadOnly = true;
            this.ConsoleRichTextBox.Size = new System.Drawing.Size(922, 255);
            this.ConsoleRichTextBox.TabIndex = 6;
            this.ConsoleRichTextBox.Text = "";
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
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PAKWindow_FormClosing);
            this.Load += new System.EventHandler(this.PAKWindow_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.LoadContext.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ItemIconPictureBox)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.ImageContext.ResumeLayout(false);
            this.ExtractAsset.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label AESKeyLabel;
        private System.Windows.Forms.TextBox AESKeyTextBox;
        private System.Windows.Forms.ComboBox PAKsComboBox;
        private System.Windows.Forms.ListBox ItemsListBox;
        private System.Windows.Forms.TreeView PAKTreeView;
        private System.Windows.Forms.PictureBox ItemIconPictureBox;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RichTextBox ConsoleRichTextBox;
        private System.Windows.Forms.Label FilterLabel;
        private System.Windows.Forms.TextBox FilterTextBox;
        private SplitButton ExtractAssetButton;
        private System.Windows.Forms.ContextMenuStrip ExtractAsset;
        private System.Windows.Forms.ToolStripMenuItem LoadDataTS;
        private System.Windows.Forms.ContextMenuStrip ImageContext;
        private System.Windows.Forms.ToolStripMenuItem ImageSaveTS;
        private SplitButton OpenImageButton;
        private System.Windows.Forms.ToolStripMenuItem SaveImageTS;
        private SplitButton LoadButton;
        private System.Windows.Forms.ContextMenuStrip LoadContext;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mergeGeneratedImagesToolStripMenuItem;
        private System.Windows.Forms.ImageList TreeViewImageList;
        private ScintillaNET.Scintilla scintilla1;
        private System.Windows.Forms.ToolStripMenuItem loadAllPAKsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem backupCurrentPAKsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enablePAKsDiffToolStripMenuItem;
    }
}

