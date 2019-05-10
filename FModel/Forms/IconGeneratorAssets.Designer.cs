namespace FModel.Forms
{
    partial class IconGeneratorAssets
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IconGeneratorAssets));
            this.OKButton = new System.Windows.Forms.Button();
            this.checkedAssets = new System.Windows.Forms.CheckedListBox();
            this.SuspendLayout();
            // 
            // OKButton
            // 
            this.OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OKButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OKButton.Location = new System.Drawing.Point(222, 300);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(97, 21);
            this.OKButton.TabIndex = 15;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // checkedAssets
            // 
            this.checkedAssets.CheckOnClick = true;
            this.checkedAssets.FormattingEnabled = true;
            this.checkedAssets.Items.AddRange(new object[] {
            "Challenges",
            "Consumables & Weapons",
            "Cosmetics",
            "Traps",
            "Variants"});
            this.checkedAssets.Location = new System.Drawing.Point(12, 12);
            this.checkedAssets.Name = "checkedAssets";
            this.checkedAssets.Size = new System.Drawing.Size(307, 274);
            this.checkedAssets.TabIndex = 16;
            // 
            // IconGeneratorAssets
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(331, 333);
            this.Controls.Add(this.checkedAssets);
            this.Controls.Add(this.OKButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "IconGeneratorAssets";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FModel Settings - Icon Assets";
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.CheckedListBox checkedAssets;
    }
}