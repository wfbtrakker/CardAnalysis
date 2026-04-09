namespace CardAnalysis
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            menuStrip        = new MenuStrip();
            fileMenu         = new ToolStripMenuItem();
            optionsMenu      = new ToolStripMenuItem();
            menuOpen             = new ToolStripMenuItem();
            menuSave             = new ToolStripMenuItem();
            menuSeparator        = new ToolStripSeparator();
            menuSaveTemplate     = new ToolStripMenuItem();
            menuLoadTemplate     = new ToolStripMenuItem();
            menuSeparator2       = new ToolStripSeparator();
            menuClear            = new ToolStripMenuItem();
            toolStrip        = new ToolStrip();
            btnZoomIn        = new ToolStripButton();
            btnZoomOut       = new ToolStripButton();
            btnFit           = new ToolStripButton();
            sep2             = new ToolStripSeparator();
            btnCrop          = new ToolStripButton();
            sep3             = new ToolStripSeparator();
            btnGuides        = new ToolStripButton();
            btnAutoDetect    = new ToolStripButton();
            btnCropToBorders = new ToolStripButton();
            sep4             = new ToolStripSeparator();
            btnRotateLeft    = new ToolStripButton();
            txtRotateStep    = new ToolStripLabel();
            btnRotateRight   = new ToolStripButton();
            lblRotationTotal = new ToolStripLabel();
            lblZoom          = new ToolStripLabel();
            imageCanvas      = new ImageCanvas();
            statusStrip      = new StatusStrip();
            lblFileName      = new ToolStripStatusLabel();
            lblSpring        = new ToolStripStatusLabel();
            lblMeasure       = new ToolStripStatusLabel();

            // ── MenuStrip ────────────────────────────────────────────────
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(optionsMenu);

            optionsMenu.Text   = "Options";
            optionsMenu.Click += BtnOptions_Click;

            fileMenu.Text = "File";
            fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                menuOpen, menuSave, menuSeparator,
                menuSaveTemplate, menuLoadTemplate, menuSeparator2,
                menuClear
            });

            menuOpen.Text         = "Open…";
            menuOpen.ShortcutKeys = Keys.Control | Keys.O;
            menuOpen.Click       += BtnOpen_Click;

            menuSave.Text         = "Save…";
            menuSave.ShortcutKeys = Keys.Control | Keys.S;
            menuSave.Click       += BtnSave_Click;

            menuSaveTemplate.Text   = "Save Template…";
            menuSaveTemplate.Click += BtnSaveTemplate_Click;

            menuLoadTemplate.Text   = "Load Template…";
            menuLoadTemplate.Click += BtnLoadTemplate_Click;

            menuClear.Text   = "Clear";
            menuClear.Click += BtnClear_Click;

            // ── ToolStrip ────────────────────────────────────────────────
            toolStrip.SuspendLayout();
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                btnZoomIn, lblZoom, btnZoomOut, btnFit,
                sep2, btnCrop, sep3, btnGuides, btnAutoDetect, btnCropToBorders,
                sep4, btnRotateLeft, txtRotateStep, btnRotateRight, lblRotationTotal
            });

            btnZoomIn.Text = "＋";
            btnZoomIn.ToolTipText = "Zoom in (+)";
            btnZoomIn.Click += BtnZoomIn_Click;

            btnZoomOut.Text = "－";
            btnZoomOut.ToolTipText = "Zoom out (-)";
            btnZoomOut.Click += BtnZoomOut_Click;

            btnFit.Text = "⊡";
            btnFit.ToolTipText = "Fit image to window (F)";
            btnFit.Click += BtnFit_Click;

            btnCrop.Text = "✂  Crop";
            btnCrop.ToolTipText = "Enter crop mode (C)";
            btnCrop.Click += BtnCrop_Click;

            btnGuides.Text = "⊹  Guidelines";
            btnGuides.ToolTipText = "Toggle border guidelines (G)";
            btnGuides.Click += BtnGuides_Click;

            btnAutoDetect.Text = "⊛  Auto-Detect";
            btnAutoDetect.ToolTipText = "Auto-detect white borders and place guidelines";
            btnAutoDetect.Click += BtnAutoDetect_Click;

            btnCropToBorders.Text = "⬚  Crop to Borders";
            btnCropToBorders.ToolTipText = "Crop image to the four outer border guidelines";
            btnCropToBorders.Enabled = false;
            btnCropToBorders.Click += BtnCropToBorders_Click;

            btnRotateLeft.Text = " ↺ ";
            btnRotateLeft.ToolTipText = "Rotate left by step amount";
            btnRotateLeft.Click += BtnRotateLeft_Click;

            txtRotateStep.ToolTipText = "Current rotation step (set via Options)";

            btnRotateRight.Text = " ↻ ";
            btnRotateRight.ToolTipText = "Rotate right by step amount";
            btnRotateRight.Click += BtnRotateRight_Click;

            lblRotationTotal.Text         = "";
            lblRotationTotal.ToolTipText  = "Total rotation applied to current image";

            lblZoom.Text = "";
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();

            // ── StatusStrip ──────────────────────────────────────────────
            lblFileName.Text = "No file loaded";
            lblFileName.TextAlign = ContentAlignment.MiddleLeft;

            lblSpring.Spring = true;

            lblMeasure.Text = "";
            lblMeasure.TextAlign = ContentAlignment.MiddleRight;

            statusStrip.Items.AddRange(new ToolStripItem[]
            {
                lblFileName, lblSpring, lblMeasure
            });

            // ── ImageCanvas ──────────────────────────────────────────────
            imageCanvas.Dock = DockStyle.Fill;

            // ── Form ─────────────────────────────────────────────────────
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize    = new Size(1024, 768);
            Text          = "Card Analysis";
            MainMenuStrip = menuStrip;
            Controls.Add(imageCanvas);
            Controls.Add(toolStrip);
            Controls.Add(menuStrip);
            Controls.Add(statusStrip);
        }

        #endregion

        private MenuStrip menuStrip;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem optionsMenu;
        private ToolStripMenuItem menuOpen;
        private ToolStripMenuItem menuSave;
        private ToolStripSeparator menuSeparator;
        private ToolStripMenuItem menuSaveTemplate;
        private ToolStripMenuItem menuLoadTemplate;
        private ToolStripSeparator menuSeparator2;
        private ToolStripMenuItem menuClear;
        private ToolStrip toolStrip;
        private ToolStripButton btnZoomIn;
        private ToolStripButton btnZoomOut;
        private ToolStripButton btnFit;
        private ToolStripSeparator sep2;
        private ToolStripButton btnCrop;
        private ToolStripSeparator sep3;
        private ToolStripButton btnGuides;
        private ToolStripButton btnAutoDetect;
        private ToolStripButton btnCropToBorders;
        private ToolStripSeparator sep4;
        private ToolStripButton btnRotateLeft;
        private ToolStripLabel txtRotateStep;
        private ToolStripButton btnRotateRight;
        private ToolStripLabel lblRotationTotal;
        private ToolStripLabel lblZoom;
        private ImageCanvas imageCanvas;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblFileName;
        private ToolStripStatusLabel lblSpring;
        private ToolStripStatusLabel lblMeasure;
    }
}
