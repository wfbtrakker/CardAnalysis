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
            _overlaySave                    = settings.OverlaySave;
            txtRotateStep.Text              = settings.RotationStep.ToString("F1") + "°";

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
            toolStrip.Renderer   = new ModernRenderer();
            toolStrip.BackColor  = Color.FromArgb(30, 30, 30);
            toolStrip.Font       = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            toolStrip.AutoSize   = true;
            toolStrip.LayoutStyle = ToolStripLayoutStyle.Flow;
            toolStrip.Padding    = new Padding(4, 2, 4, 2);

            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (item is ToolStripButton btn)
                {
                    btn.DisplayStyle = ToolStripItemDisplayStyle.Text;
                    btn.ForeColor    = Color.FromArgb(230, 230, 230);
                    btn.Padding      = new Padding(10, 0, 10, 0);
                    btn.AutoSize     = true;
                }
            }


            // Rotation arrows get tighter padding
            btnRotateLeft.Padding  = new Padding(6, 0, 6, 0);
            btnRotateRight.Padding = new Padding(6, 0, 6, 0);

            txtRotateStep.ForeColor = Color.FromArgb(160, 160, 160);
            txtRotateStep.Font      = new Font("Segoe UI", 9.5f);

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

        // ── File open ────────────────────────────────────────────────────

        private void BtnOpen_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Open Sports Card Image",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.tif;*.webp|All Files|*.*"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                _loadedFilePath = dialog.FileName;
                imageCanvas.LoadImage(dialog.FileName);
                string fileName = Path.GetFileName(dialog.FileName);
                lblFileName.Text = fileName;
                Text = $"Card Analysis  —  {fileName}";
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
            lblFileName.Text = "No file loaded";
            lblMeasure.Text = "";
            Text = "Card Analysis";
            SyncGuideButtons();
        }

        // ── Save ─────────────────────────────────────────────────────────

        private string? _loadedFilePath;
        private bool _overlaySave = true;

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
                InitialDirectory = _loadedFilePath is not null
                    ? Path.GetDirectoryName(_loadedFilePath)
                    : null
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
                btnCrop.Text = "✕  Cancel Crop";
                btnCrop.ToolTipText = "Cancel crop mode (Escape)";
                lblFileName.Text = "Drag to select crop area  —  Enter to apply, Escape to cancel";
            }
            else
            {
                btnCrop.Text = "✂  Crop";
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
                imageCanvas.PixelWhiteThreshold, imageCanvas.LineWhiteThreshold, imageCanvas.BorderTolerancePct);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            imageCanvas.RotationStep        = dlg.RotationStep;
            imageCanvas.PixelWhiteThreshold = dlg.PixelWhiteThreshold;
            imageCanvas.LineWhiteThreshold  = dlg.LineWhiteThreshold;
            imageCanvas.BorderTolerancePct  = dlg.BorderTolerancePct;
            txtRotateStep.Text              = dlg.RotationStep.ToString("F1") + "°";
            _overlaySave                    = dlg.OverlaySave;

            new AppSettings
            {
                RotationStep        = dlg.RotationStep,
                OverlaySave         = dlg.OverlaySave,
                PixelWhiteThreshold = dlg.PixelWhiteThreshold,
                LineWhiteThreshold  = dlg.LineWhiteThreshold,
                BorderTolerancePct  = dlg.BorderTolerancePct,
            }.Save();
        }

        private void BtnRotateLeft_Click(object? sender, EventArgs e)  => ApplyRotation(-imageCanvas.RotationStep);
        private void BtnRotateRight_Click(object? sender, EventArgs e) => ApplyRotation(imageCanvas.RotationStep);

        private void ApplyRotation(float degrees)
        {
            imageCanvas.RotateBy(degrees);
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
            btnGuides.Text = imageCanvas.GuidesVisible ? "⊹  Guidelines  ✓" : "⊹  Guidelines";
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
