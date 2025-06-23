namespace BrokenStatsFrontendWinForms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DateTimePicker startPicker;
        private System.Windows.Forms.DateTimePicker endPicker;
        private System.Windows.Forms.Button loadButton;
        private System.Windows.Forms.DataGridView instancesGrid;
        private System.Windows.Forms.DataGridView fightsGrid;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.startPicker = new System.Windows.Forms.DateTimePicker();
            this.endPicker = new System.Windows.Forms.DateTimePicker();
            this.loadButton = new System.Windows.Forms.Button();
            this.instancesGrid = new System.Windows.Forms.DataGridView();
            this.fightsGrid = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.instancesGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fightsGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // startPicker
            // 
            this.startPicker.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.startPicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.startPicker.Location = new System.Drawing.Point(12, 12);
            this.startPicker.Size = new System.Drawing.Size(200, 23);
            // 
            // endPicker
            // 
            this.endPicker.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.endPicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.endPicker.Location = new System.Drawing.Point(218, 12);
            this.endPicker.Size = new System.Drawing.Size(200, 23);
            // 
            // loadButton
            // 
            this.loadButton.Location = new System.Drawing.Point(424, 12);
            this.loadButton.Size = new System.Drawing.Size(75, 23);
            this.loadButton.Text = "Load";
            this.loadButton.Click += new System.EventHandler(this.loadButton_Click);
            // 
            // instancesGrid
            // 
            this.instancesGrid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.instancesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.instancesGrid.Location = new System.Drawing.Point(12, 41);
            this.instancesGrid.MultiSelect = false;
            this.instancesGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.instancesGrid.Size = new System.Drawing.Size(760, 200);
            this.instancesGrid.SelectionChanged += new System.EventHandler(this.instancesGrid_SelectionChanged);
            // 
            // fightsGrid
            // 
            this.fightsGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fightsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.fightsGrid.Location = new System.Drawing.Point(12, 247);
            this.fightsGrid.MultiSelect = false;
            this.fightsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.fightsGrid.Size = new System.Drawing.Size(760, 200);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(784, 461);
            this.Controls.Add(this.startPicker);
            this.Controls.Add(this.endPicker);
            this.Controls.Add(this.loadButton);
            this.Controls.Add(this.instancesGrid);
            this.Controls.Add(this.fightsGrid);
            this.Name = "MainForm";
            this.Text = "BrokenStats Dashboard";
            ((System.ComponentModel.ISupportInitialize)(this.instancesGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fightsGrid)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
