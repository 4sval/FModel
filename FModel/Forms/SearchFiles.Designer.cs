namespace FModel.Forms
{
    partial class SearchFiles
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SearchFiles));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panel1 = new System.Windows.Forms.Panel();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyFilePathToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyFileNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyFilePathWithoutExtensionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyFileNameWithoutExtensionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.listView1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5.545769F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 94.45423F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(798, 652);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView1.FullRowSelect = true;
            this.listView1.Location = new System.Drawing.Point(3, 39);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(792, 610);
            this.listView1.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.VirtualMode = true;
            this.listView1.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.listView1_RetrieveVirtualItem);
            this.listView1.SelectedIndexChanged += new System.EventHandler(this.listView1_SelectedIndexChanged);
            this.listView1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ListView1_MouseDown);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Files";
            this.columnHeader1.Width = 569;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "PAKs";
            this.columnHeader2.Width = 219;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.textBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(792, 30);
            this.panel1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Enabled = false;
            this.button2.Location = new System.Drawing.Point(708, 4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 3;
            this.button2.Text = "Extract";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.Button2_Click);
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Enabled = false;
            this.button1.Location = new System.Drawing.Point(627, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Go To";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Search:";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(59, 6);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(438, 20);
            this.textBox1.TabIndex = 0;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyFilePathToolStripMenuItem,
            this.copyFileNameToolStripMenuItem,
            this.copyFilePathWithoutExtensionToolStripMenuItem,
            this.copyFileNameWithoutExtensionToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(256, 114);
            // 
            // copyFilePathToolStripMenuItem
            // 
            this.copyFilePathToolStripMenuItem.Name = "copyFilePathToolStripMenuItem";
            this.copyFilePathToolStripMenuItem.Size = new System.Drawing.Size(255, 22);
            this.copyFilePathToolStripMenuItem.Text = "Copy File Path";
            this.copyFilePathToolStripMenuItem.Click += new System.EventHandler(this.CopyFilePathToolStripMenuItem_Click);
            // 
            // copyFileNameToolStripMenuItem
            // 
            this.copyFileNameToolStripMenuItem.Name = "copyFileNameToolStripMenuItem";
            this.copyFileNameToolStripMenuItem.Size = new System.Drawing.Size(255, 22);
            this.copyFileNameToolStripMenuItem.Text = "Copy File Name";
            this.copyFileNameToolStripMenuItem.Click += new System.EventHandler(this.CopyFileNameToolStripMenuItem_Click);
            // 
            // copyFilePathWithoutExtensionToolStripMenuItem
            // 
            this.copyFilePathWithoutExtensionToolStripMenuItem.Name = "copyFilePathWithoutExtensionToolStripMenuItem";
            this.copyFilePathWithoutExtensionToolStripMenuItem.Size = new System.Drawing.Size(255, 22);
            this.copyFilePathWithoutExtensionToolStripMenuItem.Text = "Copy File Path without Extension";
            this.copyFilePathWithoutExtensionToolStripMenuItem.Click += new System.EventHandler(this.CopyFilePathWithoutExtensionToolStripMenuItem_Click);
            // 
            // copyFileNameWithoutExtensionToolStripMenuItem
            // 
            this.copyFileNameWithoutExtensionToolStripMenuItem.Name = "copyFileNameWithoutExtensionToolStripMenuItem";
            this.copyFileNameWithoutExtensionToolStripMenuItem.Size = new System.Drawing.Size(255, 22);
            this.copyFileNameWithoutExtensionToolStripMenuItem.Text = "Copy File Name without Extension";
            this.copyFileNameWithoutExtensionToolStripMenuItem.Click += new System.EventHandler(this.CopyFileNameWithoutExtensionToolStripMenuItem_Click);
            // 
            // SearchFiles
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(798, 652);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(814, 691);
            this.Name = "SearchFiles";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Search Files";
            this.Load += new System.EventHandler(this.SearchFiles_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem copyFilePathToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyFileNameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyFilePathWithoutExtensionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyFileNameWithoutExtensionToolStripMenuItem;
    }
}