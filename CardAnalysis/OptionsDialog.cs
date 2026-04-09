namespace CardAnalysis
{
    public class OptionsDialog : Form
    {
        // ── Returned settings ────────────────────────────────────────────
        public float RotationStep        => (float)nudRotationStep.Value;
        public bool  OverlaySave         => chkOverlaySave.Checked;
        public float PixelWhiteThreshold => (float)nudPixelWhite.Value;
        public float LineWhiteThreshold  => (float)nudLineWhite.Value;
        public float BorderTolerancePct  => (float)nudTolerance.Value;

        // ── Controls ─────────────────────────────────────────────────────
        private GroupBox      grpRotation     = null!;
        private Label         lblRotationStep = null!;
        private NumericUpDown nudRotationStep = null!;
        private Label         lblDegrees      = null!;
        private GroupBox      grpSave         = null!;
        private CheckBox      chkOverlaySave  = null!;
        private GroupBox      grpDetect       = null!;
        private Label         lblPixelWhite   = null!;
        private NumericUpDown nudPixelWhite   = null!;
        private Label         lblLineWhite    = null!;
        private NumericUpDown nudLineWhite    = null!;
        private Label         lblTolerance    = null!;
        private NumericUpDown nudTolerance    = null!;
        private Button        btnOk           = null!;
        private Button        btnCancel       = null!;

        public OptionsDialog(float currentRotationStep, bool currentOverlaySave,
            float currentPixelWhite, float currentLineWhite, float currentTolerance)
        {
            InitializeComponent();
            nudRotationStep.Value = Math.Clamp((decimal)currentRotationStep,
                nudRotationStep.Minimum, nudRotationStep.Maximum);
            chkOverlaySave.Checked = currentOverlaySave;
            nudPixelWhite.Value = Math.Clamp((decimal)currentPixelWhite,
                nudPixelWhite.Minimum, nudPixelWhite.Maximum);
            nudLineWhite.Value = Math.Clamp((decimal)currentLineWhite,
                nudLineWhite.Minimum, nudLineWhite.Maximum);
            nudTolerance.Value = Math.Clamp((decimal)currentTolerance,
                nudTolerance.Minimum, nudTolerance.Maximum);
        }

        private void InitializeComponent()
        {
            grpRotation     = new GroupBox();
            lblRotationStep = new Label();
            nudRotationStep = new NumericUpDown();
            lblDegrees      = new Label();
            grpSave         = new GroupBox();
            chkOverlaySave  = new CheckBox();
            grpDetect       = new GroupBox();
            lblPixelWhite   = new Label();
            nudPixelWhite   = new NumericUpDown();
            lblLineWhite    = new Label();
            nudLineWhite    = new NumericUpDown();
            lblTolerance    = new Label();
            nudTolerance    = new NumericUpDown();
            btnOk           = new Button();
            btnCancel       = new Button();

            // ── Rotation group ────────────────────────────────────────────
            grpRotation.Text     = "Rotation";
            grpRotation.Location = new Point(12, 12);
            grpRotation.Size     = new Size(380, 64);

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

            lblDegrees.Text      = "degrees  (0.1 – 45.0)";
            lblDegrees.Location  = new Point(172, 28);
            lblDegrees.Size      = new Size(194, 23);
            lblDegrees.TextAlign = ContentAlignment.MiddleLeft;
            lblDegrees.ForeColor = SystemColors.GrayText;

            grpRotation.Controls.AddRange(new Control[]
                { lblRotationStep, nudRotationStep, lblDegrees });

            // ── Save group ────────────────────────────────────────────────
            grpSave.Text     = "Save";
            grpSave.Location = new Point(12, 88);
            grpSave.Size     = new Size(380, 52);

            chkOverlaySave.Text     = "Overlay guidelines and metrics on saved image";
            chkOverlaySave.Location = new Point(12, 20);
            chkOverlaySave.Size     = new Size(356, 22);
            chkOverlaySave.Checked  = true;

            grpSave.Controls.Add(chkOverlaySave);

            // ── Auto-Detect group ─────────────────────────────────────────
            grpDetect.Text     = "Auto-Detect Thresholds";
            grpDetect.Location = new Point(12, 152);
            grpDetect.Size     = new Size(380, 120);

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

            var lblPixelHint = new Label
            {
                Text      = "min brightness to count as white  (0.50 – 1.00)",
                Location  = new Point(207, 28),
                Size      = new Size(162, 23),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = SystemColors.GrayText
            };

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

            var lblLineHint = new Label
            {
                Text      = "fraction of white pixels per row/col  (0.20 – 1.00)",
                Location  = new Point(207, 58),
                Size      = new Size(162, 23),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = SystemColors.GrayText
            };

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

            var lblTolHint = new Label
            {
                Text      = "outward buffer as fraction of image size",
                Location  = new Point(207, 88),
                Size      = new Size(162, 23),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = SystemColors.GrayText
            };

            grpDetect.Controls.AddRange(new Control[]
            {
                lblPixelWhite, nudPixelWhite, lblPixelHint,
                lblLineWhite,  nudLineWhite,  lblLineHint,
                lblTolerance,  nudTolerance,  lblTolHint
            });

            // ── OK / Cancel ───────────────────────────────────────────────
            btnOk.Text         = "OK";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location     = new Point(226, 288);
            btnOk.Size         = new Size(75, 28);
            btnOk.Click       += (s, e) => Close();

            btnCancel.Text         = "Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location     = new Point(317, 288);
            btnCancel.Size         = new Size(75, 28);
            btnCancel.Click       += (s, e) => Close();

            // ── Form ──────────────────────────────────────────────────────
            AcceptButton      = btnOk;
            CancelButton      = btnCancel;
            AutoScaleMode     = AutoScaleMode.Font;
            ClientSize        = new Size(404, 328);
            FormBorderStyle   = FormBorderStyle.FixedDialog;
            MaximizeBox       = false;
            MinimizeBox       = false;
            ShowInTaskbar     = false;
            StartPosition     = FormStartPosition.CenterParent;
            Text              = "Options";

            Controls.AddRange(new Control[] { grpRotation, grpSave, grpDetect, btnOk, btnCancel });
        }
    }
}
