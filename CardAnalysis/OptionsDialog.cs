namespace CardAnalysis
{
    public class OptionsDialog : Form
    {
        // ── Returned settings ────────────────────────────────────────────
        public float  RotationStep         => (float)nudRotationStep.Value;
        public bool   OverlaySave          => chkOverlaySave.Checked;
        public float  PixelWhiteThreshold  => (float)nudPixelWhite.Value;
        public float  LineWhiteThreshold   => (float)nudLineWhite.Value;
        public float  BorderTolerancePct   => (float)nudTolerance.Value;
        public string RawImageFolder       => txtRawFolder.Text.Trim();
        public string ProcessedImageFolder => txtProcessedFolder.Text.Trim();

        // ── Controls ─────────────────────────────────────────────────────
        private System.ComponentModel.IContainer? components = null;

        private GroupBox      grpRotation        = null!;
        private Label         lblRotationStep    = null!;
        private NumericUpDown nudRotationStep    = null!;
        private Label         lblDegrees         = null!;
        private GroupBox      grpSave            = null!;
        private CheckBox      chkOverlaySave     = null!;
        private GroupBox      grpDetect          = null!;
        private Label         lblPixelWhite      = null!;
        private NumericUpDown nudPixelWhite      = null!;
        private Label         lblPixelHint       = null!;
        private Label         lblLineWhite       = null!;
        private NumericUpDown nudLineWhite       = null!;
        private Label         lblLineHint        = null!;
        private Label         lblTolerance       = null!;
        private NumericUpDown nudTolerance       = null!;
        private Label         lblTolHint         = null!;
        private GroupBox      grpFolders         = null!;
        private Label         lblRawFolder       = null!;
        private TextBox       txtRawFolder       = null!;
        private Button        btnBrowseRaw       = null!;
        private Label         lblProcessedFolder = null!;
        private TextBox       txtProcessedFolder = null!;
        private Button        btnBrowseProcessed = null!;
        private Button        btnOk              = null!;
        private Button        btnCancel          = null!;

        // Parameterless constructor required by the VS WinForms designer.
        public OptionsDialog() : this(0.1f, true, 0.80f, 0.55f, 0.006f, "", "") { }

        public OptionsDialog(float currentRotationStep, bool currentOverlaySave,
            float currentPixelWhite, float currentLineWhite, float currentTolerance,
            string currentRawFolder, string currentProcessedFolder)
        {
            InitializeComponent();
            nudRotationStep.Value   = Math.Clamp((decimal)currentRotationStep,
                nudRotationStep.Minimum, nudRotationStep.Maximum);
            chkOverlaySave.Checked  = currentOverlaySave;
            nudPixelWhite.Value     = Math.Clamp((decimal)currentPixelWhite,
                nudPixelWhite.Minimum, nudPixelWhite.Maximum);
            nudLineWhite.Value      = Math.Clamp((decimal)currentLineWhite,
                nudLineWhite.Minimum, nudLineWhite.Maximum);
            nudTolerance.Value      = Math.Clamp((decimal)currentTolerance,
                nudTolerance.Minimum, nudTolerance.Maximum);
            txtRawFolder.Text       = currentRawFolder;
            txtProcessedFolder.Text = currentProcessedFolder;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // Instantiate all controls first
            grpRotation        = new GroupBox();
            lblRotationStep    = new Label();
            nudRotationStep    = new NumericUpDown();
            lblDegrees         = new Label();
            grpSave            = new GroupBox();
            chkOverlaySave     = new CheckBox();
            grpDetect          = new GroupBox();
            lblPixelWhite      = new Label();
            nudPixelWhite      = new NumericUpDown();
            lblPixelHint       = new Label();
            lblLineWhite       = new Label();
            nudLineWhite       = new NumericUpDown();
            lblLineHint        = new Label();
            lblTolerance       = new Label();
            nudTolerance       = new NumericUpDown();
            lblTolHint         = new Label();
            grpFolders         = new GroupBox();
            lblRawFolder       = new Label();
            txtRawFolder       = new TextBox();
            btnBrowseRaw       = new Button();
            lblProcessedFolder = new Label();
            txtProcessedFolder = new TextBox();
            btnBrowseProcessed = new Button();
            btnOk              = new Button();
            btnCancel          = new Button();

            // BeginInit on all NumericUpDown controls before touching their properties
            ((System.ComponentModel.ISupportInitialize)nudRotationStep).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudPixelWhite).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudLineWhite).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudTolerance).BeginInit();

            // Suspend layout on all containers and the form
            grpRotation.SuspendLayout();
            grpSave.SuspendLayout();
            grpDetect.SuspendLayout();
            grpFolders.SuspendLayout();
            SuspendLayout();

            // ── Rotation group ────────────────────────────────────────────
            grpRotation.Text     = "Rotation";
            grpRotation.Location = new Point(12, 12);
            grpRotation.Size     = new Size(380, 64);
            grpRotation.TabStop  = false;

            lblRotationStep.Text      = "Increment:";
            lblRotationStep.Location  = new Point(12, 28);
            lblRotationStep.Size      = new Size(70, 23);
            lblRotationStep.TextAlign = ContentAlignment.MiddleLeft;

            nudRotationStep.Location      = new Point(86, 26);
            nudRotationStep.Size          = new Size(80, 23);
            nudRotationStep.Minimum       = 0.1m;
            nudRotationStep.Maximum       = 45.0m;
            nudRotationStep.DecimalPlaces = 1;
            nudRotationStep.Increment     = 0.1m;
            nudRotationStep.Value         = 0.1m;
            nudRotationStep.TabIndex      = 0;

            lblDegrees.Text      = "degrees  (0.1 – 45.0)";
            lblDegrees.Location  = new Point(172, 28);
            lblDegrees.Size      = new Size(194, 23);
            lblDegrees.TextAlign = ContentAlignment.MiddleLeft;
            lblDegrees.ForeColor = SystemColors.GrayText;

            grpRotation.Controls.Add(lblRotationStep);
            grpRotation.Controls.Add(nudRotationStep);
            grpRotation.Controls.Add(lblDegrees);

            // ── Save group ────────────────────────────────────────────────
            grpSave.Text     = "Save";
            grpSave.Location = new Point(12, 88);
            grpSave.Size     = new Size(380, 52);
            grpSave.TabStop  = false;

            chkOverlaySave.Text     = "Overlay guidelines and metrics on saved image";
            chkOverlaySave.Location = new Point(12, 20);
            chkOverlaySave.Size     = new Size(356, 22);
            chkOverlaySave.Checked  = true;
            chkOverlaySave.TabIndex = 0;

            grpSave.Controls.Add(chkOverlaySave);

            // ── Auto-Detect group ─────────────────────────────────────────
            grpDetect.Text     = "Auto-Detect Thresholds";
            grpDetect.Location = new Point(12, 152);
            grpDetect.Size     = new Size(380, 120);
            grpDetect.TabStop  = false;

            lblPixelWhite.Text      = "Pixel brightness:";
            lblPixelWhite.Location  = new Point(12, 28);
            lblPixelWhite.Size      = new Size(110, 23);
            lblPixelWhite.TextAlign = ContentAlignment.MiddleLeft;

            nudPixelWhite.Location      = new Point(126, 26);
            nudPixelWhite.Size          = new Size(75, 23);
            nudPixelWhite.Minimum       = 0.50m;
            nudPixelWhite.Maximum       = 1.00m;
            nudPixelWhite.DecimalPlaces = 2;
            nudPixelWhite.Increment     = 0.01m;
            nudPixelWhite.Value         = 0.80m;
            nudPixelWhite.TabIndex      = 0;

            lblPixelHint.Text      = "min brightness to count as white  (0.50 – 1.00)";
            lblPixelHint.Location  = new Point(207, 28);
            lblPixelHint.Size      = new Size(162, 23);
            lblPixelHint.TextAlign = ContentAlignment.MiddleLeft;
            lblPixelHint.ForeColor = SystemColors.GrayText;

            lblLineWhite.Text      = "Line whiteness:";
            lblLineWhite.Location  = new Point(12, 58);
            lblLineWhite.Size      = new Size(110, 23);
            lblLineWhite.TextAlign = ContentAlignment.MiddleLeft;

            nudLineWhite.Location      = new Point(126, 56);
            nudLineWhite.Size          = new Size(75, 23);
            nudLineWhite.Minimum       = 0.20m;
            nudLineWhite.Maximum       = 1.00m;
            nudLineWhite.DecimalPlaces = 2;
            nudLineWhite.Increment     = 0.01m;
            nudLineWhite.Value         = 0.55m;
            nudLineWhite.TabIndex      = 1;

            lblLineHint.Text      = "fraction of white pixels per row/col  (0.20 – 1.00)";
            lblLineHint.Location  = new Point(207, 58);
            lblLineHint.Size      = new Size(162, 23);
            lblLineHint.TextAlign = ContentAlignment.MiddleLeft;
            lblLineHint.ForeColor = SystemColors.GrayText;

            lblTolerance.Text      = "Edge tolerance:";
            lblTolerance.Location  = new Point(12, 88);
            lblTolerance.Size      = new Size(110, 23);
            lblTolerance.TextAlign = ContentAlignment.MiddleLeft;

            nudTolerance.Location      = new Point(126, 86);
            nudTolerance.Size          = new Size(75, 23);
            nudTolerance.Minimum       = 0.000m;
            nudTolerance.Maximum       = 0.050m;
            nudTolerance.DecimalPlaces = 3;
            nudTolerance.Increment     = 0.001m;
            nudTolerance.Value         = 0.006m;
            nudTolerance.TabIndex      = 2;

            lblTolHint.Text      = "outward buffer as fraction of image size";
            lblTolHint.Location  = new Point(207, 88);
            lblTolHint.Size      = new Size(162, 23);
            lblTolHint.TextAlign = ContentAlignment.MiddleLeft;
            lblTolHint.ForeColor = SystemColors.GrayText;

            grpDetect.Controls.Add(lblPixelWhite);
            grpDetect.Controls.Add(nudPixelWhite);
            grpDetect.Controls.Add(lblPixelHint);
            grpDetect.Controls.Add(lblLineWhite);
            grpDetect.Controls.Add(nudLineWhite);
            grpDetect.Controls.Add(lblLineHint);
            grpDetect.Controls.Add(lblTolerance);
            grpDetect.Controls.Add(nudTolerance);
            grpDetect.Controls.Add(lblTolHint);

            // ── Folders group ─────────────────────────────────────────────
            grpFolders.Text     = "Folders";
            grpFolders.Location = new Point(12, 284);
            grpFolders.Size     = new Size(380, 96);
            grpFolders.TabStop  = false;

            lblRawFolder.Text      = "Raw images:";
            lblRawFolder.Location  = new Point(12, 28);
            lblRawFolder.Size      = new Size(80, 23);
            lblRawFolder.TextAlign = ContentAlignment.MiddleLeft;

            txtRawFolder.Location = new Point(96, 26);
            txtRawFolder.Size     = new Size(212, 23);
            txtRawFolder.TabIndex = 0;

            btnBrowseRaw.Text     = "Browse…";
            btnBrowseRaw.Location = new Point(314, 25);
            btnBrowseRaw.Size     = new Size(54, 25);
            btnBrowseRaw.TabIndex = 1;
            btnBrowseRaw.Click   += BtnBrowseRaw_Click;

            lblProcessedFolder.Text      = "Processed:";
            lblProcessedFolder.Location  = new Point(12, 60);
            lblProcessedFolder.Size      = new Size(80, 23);
            lblProcessedFolder.TextAlign = ContentAlignment.MiddleLeft;

            txtProcessedFolder.Location = new Point(96, 58);
            txtProcessedFolder.Size     = new Size(212, 23);
            txtProcessedFolder.TabIndex = 2;

            btnBrowseProcessed.Text     = "Browse…";
            btnBrowseProcessed.Location = new Point(314, 57);
            btnBrowseProcessed.Size     = new Size(54, 25);
            btnBrowseProcessed.TabIndex = 3;
            btnBrowseProcessed.Click   += BtnBrowseProcessed_Click;

            grpFolders.Controls.Add(lblRawFolder);
            grpFolders.Controls.Add(txtRawFolder);
            grpFolders.Controls.Add(btnBrowseRaw);
            grpFolders.Controls.Add(lblProcessedFolder);
            grpFolders.Controls.Add(txtProcessedFolder);
            grpFolders.Controls.Add(btnBrowseProcessed);

            // ── OK / Cancel ───────────────────────────────────────────────
            btnOk.Text         = "OK";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location     = new Point(226, 396);
            btnOk.Size         = new Size(75, 28);
            btnOk.TabIndex     = 10;

            btnCancel.Text         = "Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location     = new Point(317, 396);
            btnCancel.Size         = new Size(75, 28);
            btnCancel.TabIndex     = 11;

            // ── Form ──────────────────────────────────────────────────────
            AcceptButton           = btnOk;
            CancelButton           = btnCancel;
            AutoScaleDimensions    = new SizeF(7F, 15F);
            AutoScaleMode          = AutoScaleMode.Font;
            ClientSize             = new Size(404, 436);
            FormBorderStyle        = FormBorderStyle.FixedDialog;
            MaximizeBox            = false;
            MinimizeBox            = false;
            ShowInTaskbar          = false;
            StartPosition          = FormStartPosition.CenterParent;
            Text                   = "Options";

            Controls.Add(grpRotation);
            Controls.Add(grpSave);
            Controls.Add(grpDetect);
            Controls.Add(grpFolders);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            // EndInit, then resume layout in reverse order
            ((System.ComponentModel.ISupportInitialize)nudRotationStep).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudPixelWhite).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudLineWhite).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudTolerance).EndInit();

            grpFolders.ResumeLayout(false);
            grpFolders.PerformLayout();
            grpDetect.ResumeLayout(false);
            grpDetect.PerformLayout();
            grpSave.ResumeLayout(false);
            grpSave.PerformLayout();
            grpRotation.ResumeLayout(false);
            grpRotation.PerformLayout();
            ResumeLayout(false);
        }

        // ── Event handlers ────────────────────────────────────────────────
        private void BtnBrowseRaw_Click(object? sender, EventArgs e)       => BrowseFolder(txtRawFolder);
        private void BtnBrowseProcessed_Click(object? sender, EventArgs e) => BrowseFolder(txtProcessedFolder);

        private void BrowseFolder(TextBox target)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description            = "Select Folder",
                UseDescriptionForTitle = true,
                SelectedPath           = Directory.Exists(target.Text) ? target.Text : ""
            };
            if (fbd.ShowDialog(this) == DialogResult.OK)
                target.Text = fbd.SelectedPath;
        }
    }
}
