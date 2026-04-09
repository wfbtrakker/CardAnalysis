namespace CardAnalysis
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            ApplyStyling();

            var settings = AppSettings.Load();
            imageCanvas.RotationStep        = settings.RotationStep;
            imageCanvas.PixelWhiteThreshold = settings.PixelWhiteThreshold;
            imageCanvas.LineWhiteThreshold  = settings.LineWhiteThreshold;
            imageCanvas.BorderTolerancePct  = settings.BorderTolerancePct;
            _overlaySave          = settings.OverlaySave;
            _rawImageFolder       = settings.RawImageFolder;
            _processedImageFolder = settings.ProcessedImageFolder;
            txtRotateStep.Text    = settings.RotationStep.ToString("F1") + "°";

            imageCanvas.ZoomChanged     += OnZoomChanged;
            imageCanvas.CropModeChanged += OnCropModeChanged;
            imageCanvas.GuidesChanged   += OnGuidesChanged;
            KeyPreview = true;
        }

        private void ApplyStyling()
        {
            // ── Menu strip ───────────────────────────────────────────────
            var menuRenderer = new ToolStripProfessionalRenderer(new DarkMenuColorTable());
            menuStrip.Renderer  = menuRenderer;
            menuStrip.BackColor = Color.FromArgb(30, 30, 30);
            menuStrip.ForeColor = Color.FromArgb(230, 230, 230);
            menuStrip.Font      = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            menuStrip.Padding   = new Padding(4, 2, 4, 2);

            foreach (ToolStripMenuItem topItem in menuStrip.Items.OfType<ToolStripMenuItem>())
            {
                topItem.ForeColor = Color.FromArgb(230, 230, 230);
                foreach (ToolStripItem drop in topItem.DropDownItems)
                    drop.ForeColor = Color.FromArgb(230, 230, 230);
            }

            // ── Toolbar ──────────────────────────────────────────────────
            toolStrip.Renderer         = new ModernRenderer();
            toolStrip.BackColor        = Color.FromArgb(30, 30, 30);
            toolStrip.Font             = new Font("Segoe UI", 9f, FontStyle.Regular);
            toolStrip.AutoSize         = true;
            toolStrip.LayoutStyle      = ToolStripLayoutStyle.Flow;
            toolStrip.Padding          = new Padding(4, 4, 4, 4);
            toolStrip.ImageScalingSize = new Size(20, 20);

            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (item is ToolStripButton btn)
                {
                    btn.ForeColor = Color.FromArgb(230, 230, 230);
                    btn.AutoSize  = true;
                }
            }

            // Toolbar icons (Segoe Fluent Icons / MDL2 Assets glyphs)
            var ic = Color.FromArgb(215, 215, 215);
            const int S = 20;

            // Zoom group — icon only
            btnZoomIn.Image        = MakeIconBitmap("\uE8A3", S, ic);
            btnZoomIn.Text         = "";
            btnZoomIn.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnZoomIn.Padding      = new Padding(9, 3, 9, 3);

            btnZoomOut.Image        = MakeIconBitmap("\uE71F", S, ic);
            btnZoomOut.Text         = "";
            btnZoomOut.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnZoomOut.Padding      = new Padding(9, 3, 9, 3);

            btnFit.Image        = MakeIconBitmap("\uE8B3", S, ic);
            btnFit.Text         = "";
            btnFit.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnFit.Padding      = new Padding(9, 3, 9, 3);

            // Crop — icon + text
            btnCrop.Image             = MakeIconBitmap("\uE7A8", S, ic);
            btnCrop.Text              = "Crop";
            btnCrop.DisplayStyle      = ToolStripItemDisplayStyle.ImageAndText;
            btnCrop.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnCrop.Padding           = new Padding(9, 3, 11, 3);

            // Guidelines group — icon + text
            btnGuides.Image             = MakeIconBitmap("\uE81E", S, ic);
            btnGuides.Text              = "Guidelines";
            btnGuides.DisplayStyle      = ToolStripItemDisplayStyle.ImageAndText;
            btnGuides.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnGuides.Padding           = new Padding(9, 3, 11, 3);

            btnAutoDetect.Image             = MakeIconBitmap("\uE8D2", S, ic);
            btnAutoDetect.Text              = "Auto-Detect";
            btnAutoDetect.DisplayStyle      = ToolStripItemDisplayStyle.ImageAndText;
            btnAutoDetect.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnAutoDetect.Padding           = new Padding(9, 3, 11, 3);

            btnCropToBorders.Image             = MakeIconBitmap("\uE7C5", S, ic);
            btnCropToBorders.Text              = "Crop to Borders";
            btnCropToBorders.DisplayStyle      = ToolStripItemDisplayStyle.ImageAndText;
            btnCropToBorders.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnCropToBorders.Padding           = new Padding(9, 3, 11, 3);

            // Rotate — icon only (Unicode arrows via Segoe UI Symbol for correct directionality)
            btnRotateLeft.Image        = MakeIconBitmap("\u21BA", S, ic, "Segoe UI Symbol");
            btnRotateLeft.Text         = "";
            btnRotateLeft.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnRotateLeft.Padding      = new Padding(9, 3, 9, 3);

            btnRotateRight.Image        = MakeIconBitmap("\u21BB", S, ic, "Segoe UI Symbol");
            btnRotateRight.Text         = "";
            btnRotateRight.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnRotateRight.Padding      = new Padding(9, 3, 9, 3);

            txtRotateStep.ForeColor = Color.FromArgb(160, 160, 160);
            txtRotateStep.Font      = new Font("Segoe UI", 9f);
            txtRotateStep.Padding   = new Padding(2, 0, 6, 0);

            lblRotationTotal.ForeColor = Color.FromArgb(160, 160, 160);
            lblRotationTotal.Font      = new Font("Segoe UI", 9f);
            lblRotationTotal.Padding   = new Padding(0, 0, 6, 0);

            lblZoom.ForeColor = Color.FromArgb(160, 160, 160);
            lblZoom.Font      = new Font("Segoe UI", 9f);

            // ── Status strip ─────────────────────────────────────────────
            statusStrip.Renderer   = new ToolStripProfessionalRenderer(new DarkStatusColorTable());
            statusStrip.BackColor  = Color.FromArgb(22, 22, 22);
            statusStrip.ForeColor  = Color.FromArgb(170, 170, 170);
            statusStrip.Font       = new Font("Segoe UI", 8.5f);
            statusStrip.SizingGrip = false;

            lblFileName.ForeColor = Color.FromArgb(170, 170, 170);
            lblMeasure.ForeColor  = Color.FromArgb(130, 180, 255);
            lblMeasure.Font       = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        }

        private static Bitmap MakeIconBitmap(string glyph, int size, Color color, string? fontOverride = null)
        {
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);
            string fontName = fontOverride
                ?? (FontFamily.Families.Any(f => f.Name == "Segoe Fluent Icons")
                    ? "Segoe Fluent Icons" : "Segoe MDL2 Assets");
            using var font  = new Font(fontName, size * 0.78f, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(color);
            using var sf    = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(glyph, font, brush, new RectangleF(0, 0, size, size), sf);
            return bmp;
        }

        // ── File open ────────────────────────────────────────────────────

        private void BtnOpen_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title            = "Open Sports Card Image",
                Filter           = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.tif;*.webp|All Files|*.*",
                InitialDirectory = Directory.Exists(_rawImageFolder) ? _rawImageFolder : ""
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                _loadedFilePath = dialog.FileName;
                imageCanvas.LoadImage(dialog.FileName);
                string fileName = Path.GetFileName(dialog.FileName);
                lblFileName.Text = fileName;
                Text = $"Card Analysis  —  {fileName}";
                _totalRotation = 0f;
                UpdateRotationLabel();
                UpdateMeasureLabel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open image:\n{ex.Message}",
                    "Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            imageCanvas.ClearImage();
            _loadedFilePath = null;
            _totalRotation  = 0f;
            lblFileName.Text = "No file loaded";
            lblMeasure.Text  = "";
            lblRotationTotal.Text = "";
            Text = "Card Analysis";
            SyncGuideButtons();
        }

        // ── Save ─────────────────────────────────────────────────────────

        private string? _loadedFilePath;
        private bool   _overlaySave          = true;
        private string _rawImageFolder       = "";
        private string _processedImageFolder = "";
        private float  _totalRotation        = 0f;

        private void BtnSave_Click(object? sender, EventArgs e)
            => RunSaveDialog();

        private void RunSaveDialog()
        {
            if (imageCanvas.ImageSize is null)
            {
                MessageBox.Show("No image loaded.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string defaultName = _loadedFilePath is not null
                ? Path.GetFileNameWithoutExtension(_loadedFilePath) + "_measured"
                : "image_measured";

            using var dlg = new SaveFileDialog
            {
                Title            = "Save Image",
                Filter           = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|Bitmap|*.bmp",
                FilterIndex      = 2,
                DefaultExt       = "jpg",
                FileName         = defaultName,
                InitialDirectory = Directory.Exists(_processedImageFolder)
                    ? _processedImageFolder
                    : _loadedFilePath is not null
                        ? Path.GetDirectoryName(_loadedFilePath)
                        : ""
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var fmt = dlg.FilterIndex switch
                {
                    2 => System.Drawing.Imaging.ImageFormat.Jpeg,
                    3 => System.Drawing.Imaging.ImageFormat.Bmp,
                    _ => System.Drawing.Imaging.ImageFormat.Png
                };

                if (_overlaySave)
                    imageCanvas.SaveWithOverlay(dlg.FileName, fmt);
                else
                    imageCanvas.SaveImage(dlg.FileName, fmt);

                lblFileName.Text = $"Saved: {Path.GetFileName(dlg.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save image:\n{ex.Message}",
                    "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Zoom ─────────────────────────────────────────────────────────

        private void OnZoomChanged(object? sender, ZoomChangedEventArgs e)
        {
            lblZoom.Text = $"  {e.ZoomFactor * 100:F0}%  ";
        }

        private void BtnZoomIn_Click(object? sender, EventArgs e)  => imageCanvas.ZoomIn();
        private void BtnZoomOut_Click(object? sender, EventArgs e) => imageCanvas.ZoomOut();
        private void BtnFit_Click(object? sender, EventArgs e)     => imageCanvas.FitToWindow();

        // ── Crop ─────────────────────────────────────────────────────────

        private void BtnCrop_Click(object? sender, EventArgs e)
        {
            if (imageCanvas.IsCropMode)
                imageCanvas.CancelCrop();
            else
                imageCanvas.BeginCropMode();
        }

        private void OnCropModeChanged(object? sender, EventArgs e)
        {
            if (imageCanvas.IsCropMode)
            {
                btnCrop.Text        = "Cancel Crop";
                btnCrop.Checked     = true;
                btnCrop.ToolTipText = "Cancel crop mode (Escape)";
                lblFileName.Text    = "Drag to select crop area  —  Enter to apply, Escape to cancel";
            }
            else
            {
                btnCrop.Text        = "Crop";
                btnCrop.Checked     = false;
                btnCrop.ToolTipText = "Enter crop mode (C)";
                RestoreFileNameLabel();
                UpdateMeasureLabel();
            }
        }

        // ── Rotation ─────────────────────────────────────────────────────

        private void BtnOptions_Click(object? sender, EventArgs e)
        {
            using var dlg = new OptionsDialog(
                imageCanvas.RotationStep, _overlaySave,
                imageCanvas.PixelWhiteThreshold, imageCanvas.LineWhiteThreshold, imageCanvas.BorderTolerancePct,
                _rawImageFolder, _processedImageFolder);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            imageCanvas.RotationStep        = dlg.RotationStep;
            imageCanvas.PixelWhiteThreshold = dlg.PixelWhiteThreshold;
            imageCanvas.LineWhiteThreshold  = dlg.LineWhiteThreshold;
            imageCanvas.BorderTolerancePct  = dlg.BorderTolerancePct;
            txtRotateStep.Text              = dlg.RotationStep.ToString("F1") + "°";
            _overlaySave          = dlg.OverlaySave;
            _rawImageFolder       = dlg.RawImageFolder;
            _processedImageFolder = dlg.ProcessedImageFolder;

            new AppSettings
            {
                RotationStep         = dlg.RotationStep,
                OverlaySave          = dlg.OverlaySave,
                PixelWhiteThreshold  = dlg.PixelWhiteThreshold,
                LineWhiteThreshold   = dlg.LineWhiteThreshold,
                BorderTolerancePct   = dlg.BorderTolerancePct,
                RawImageFolder       = dlg.RawImageFolder,
                ProcessedImageFolder = dlg.ProcessedImageFolder,
            }.Save();
        }

        private void BtnRotateLeft_Click(object? sender, EventArgs e)  => ApplyRotation(-imageCanvas.RotationStep);
        private void BtnRotateRight_Click(object? sender, EventArgs e) => ApplyRotation(imageCanvas.RotationStep);

        private void ApplyRotation(float degrees)
        {
            if (imageCanvas.ImageSize is null) return;
            imageCanvas.RotateBy(degrees);
            _totalRotation += degrees;
            UpdateRotationLabel();
        }

        private void UpdateRotationLabel()
        {
            if (imageCanvas.ImageSize is null)
            {
                lblRotationTotal.Text = "";
                return;
            }
            string sign = _totalRotation >= 0 ? "+" : "";
            lblRotationTotal.Text = $"{sign}{_totalRotation:F1}°";
        }

        // ── Guidelines ───────────────────────────────────────────────────

        private void BtnGuides_Click(object? sender, EventArgs e)
        {
            imageCanvas.ToggleGuides();
            SyncGuideButtons();
            UpdateMeasureLabel();
        }

        private void BtnAutoDetect_Click(object? sender, EventArgs e)
        {
            imageCanvas.AutoDetectBorders();
            SyncGuideButtons();
            UpdateMeasureLabel();
        }

        private void SyncGuideButtons()
        {
            btnGuides.Checked        = imageCanvas.GuidesVisible;
            btnCropToBorders.Enabled = imageCanvas.GuidesVisible;
        }

        private void BtnCropToBorders_Click(object? sender, EventArgs e)
        {
            imageCanvas.CropToOuterBorders();
            UpdateMeasureLabel();

            if (imageCanvas.ImageSize is Size sz)
                lblMeasure.Text = imageCanvas.GuidesVisible
                    ? lblMeasure.Text   // measurements already updated by guides
                    : $"{sz.Width} × {sz.Height} px";
        }

        private void OnGuidesChanged(object? sender, EventArgs e)
        {
            SyncGuideButtons();
            UpdateMeasureLabel();
        }

        // ── Guideline templates ───────────────────────────────────────────

        private void BtnSaveTemplate_Click(object? sender, EventArgs e)
        {
            if (imageCanvas.ImageSize is null)
            {
                MessageBox.Show("No image loaded.", "Save Template", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!imageCanvas.GuidesVisible)
            {
                MessageBox.Show("Turn on guidelines before saving a template.",
                    "Save Template", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Directory.CreateDirectory(GuidelineTemplate.DefaultFolder);

            using var dlg = new SaveFileDialog
            {
                Title            = "Save Guideline Template",
                Filter           = "Guideline Template|*.json",
                DefaultExt       = "json",
                InitialDirectory = GuidelineTemplate.DefaultFolder,
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                string name = Path.GetFileNameWithoutExtension(dlg.FileName);
                imageCanvas.GetGuideTemplate(name).Save(dlg.FileName);
                lblFileName.Text = $"Template saved: {Path.GetFileName(dlg.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save template:\n{ex.Message}",
                    "Save Template", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLoadTemplate_Click(object? sender, EventArgs e)
        {
            if (imageCanvas.ImageSize is null)
            {
                MessageBox.Show("Open an image before loading a template.",
                    "Load Template", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Directory.CreateDirectory(GuidelineTemplate.DefaultFolder);

            using var dlg = new OpenFileDialog
            {
                Title            = "Load Guideline Template",
                Filter           = "Guideline Template|*.json|All Files|*.*",
                InitialDirectory = GuidelineTemplate.DefaultFolder,
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var template = GuidelineTemplate.Load(dlg.FileName)
                    ?? throw new InvalidDataException("File could not be parsed.");
                imageCanvas.ApplyGuideTemplate(template);
                SyncGuideButtons();
                UpdateMeasureLabel();
                lblFileName.Text = $"Template loaded: {template.Name}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load template:\n{ex.Message}",
                    "Load Template", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateMeasureLabel()
        {
            if (!imageCanvas.GuidesVisible || imageCanvas.ImageSize is null)
            {
                lblMeasure.Text = imageCanvas.ImageSize is Size sz
                    ? $"{sz.Width} × {sz.Height} px"
                    : "";
                return;
            }

            var m = imageCanvas.GetGuideMeasurements();
            float lrSum = m.Left + m.Right;
            float tbSum = m.Top  + m.Bottom;
            int lPct = lrSum > 0 ? (int)Math.Round(m.Left / lrSum * 100) : 0;
            int tPct = tbSum > 0 ? (int)Math.Round(m.Top  / tbSum * 100) : 0;
            lblMeasure.Text = $"L: {lPct}  R: {100 - lPct}  T: {tPct}  B: {100 - tPct}";
        }

        private void RestoreFileNameLabel()
        {
            if (imageCanvas.ImageSize is null)
                lblFileName.Text = "No file loaded";
            // else leave the last filename visible
        }

        // ── Keyboard ─────────────────────────────────────────────────────

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (imageCanvas.IsCropMode)
            {
                switch (e.KeyCode)
                {
                    case Keys.Enter:
                        imageCanvas.ApplyCrop();
                        UpdateMeasureLabel();
                        e.Handled = true;
                        break;
                    case Keys.Escape:
                        imageCanvas.CancelCrop();
                        e.Handled = true;
                        break;
                }
                base.OnKeyDown(e);
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.S when e.Control:
                    RunSaveDialog();
                    e.Handled = true;
                    break;
                case Keys.Oemplus:
                case Keys.Add:
                    imageCanvas.ZoomIn();
                    e.Handled = true;
                    break;
                case Keys.OemMinus:
                case Keys.Subtract:
                    imageCanvas.ZoomOut();
                    e.Handled = true;
                    break;
                case Keys.D0 when e.Control:
                case Keys.NumPad0 when e.Control:
                    imageCanvas.SetZoom(1.0);
                    e.Handled = true;
                    break;
                case Keys.F:
                    imageCanvas.FitToWindow();
                    e.Handled = true;
                    break;
                case Keys.C:
                    imageCanvas.BeginCropMode();
                    e.Handled = true;
                    break;
                case Keys.G:
                    BtnGuides_Click(this, EventArgs.Empty);
                    e.Handled = true;
                    break;
            }

            base.OnKeyDown(e);
        }
    }
}
