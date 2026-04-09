using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace CardAnalysis
{
    public class ZoomChangedEventArgs : EventArgs
    {
        public double ZoomFactor { get; }
        public ZoomChangedEventArgs(double zoomFactor) => ZoomFactor = zoomFactor;
    }

    public record GuideMeasurements(float Left, float Right, float Top, float Bottom);

    public class ImageCanvas : Panel
    {
        // ── Image & view state ───────────────────────────────────────────
        private Image? _image;
        private double _zoomFactor = 1.0;
        private PointF _panOffset = PointF.Empty;

        // ── Pan state ────────────────────────────────────────────────────
        private Point _dragStart;
        private PointF _panAtDragStart;
        private bool _isDragging;

        // ── Crop state ───────────────────────────────────────────────────
        private bool _isCropMode;
        private bool _isCropSelecting;   // initial drag in progress
        private bool _hasCropSelection;  // a finalized selection exists
        private Point _cropStart;
        private Point _cropCurrent;

        // Normalized edge positions (canvas pixels) used once selection is finalized
        private int _cropLeft, _cropRight, _cropTop, _cropBottom;

        private enum CropHandle
        {
            None,
            TopLeft, TopCenter, TopRight,
            MiddleLeft, MiddleRight,
            BottomLeft, BottomCenter, BottomRight
        }
        private CropHandle _draggingCropHandle = CropHandle.None;

        // ── Rotation state ───────────────────────────────────────────────
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public float RotationStep { get; set; } = 0.2f;

        // ── Image DPI (captured on load, used for inch conversion) ───────
        private float _dpiX = 96f;

        // ── Guide state ──────────────────────────────────────────────────
        // All positions stored in image-pixel coordinates (floats for sub-pixel accuracy while dragging)
        private bool _guidesVisible;
        private float _guideLeftOuter;
        private float _guideLeftInner;
        private float _guideRightInner;
        private float _guideRightOuter;
        private float _guideTopOuter;
        private float _guideTopInner;
        private float _guideBottomInner;
        private float _guideBottomOuter;

        private enum GuideId
        {
            None,
            LeftOuter, LeftInner,
            RightInner, RightOuter,
            TopOuter, TopInner,
            BottomInner, BottomOuter
        }
        private GuideId _draggingGuide = GuideId.None;
        private const int GuideHitSlop   = 6;
        private const int HandleWidth    = 16;  // pixels perpendicular to guide direction
        private const int HandleLength   = 52;  // pixels along guide direction
        private const int HandleRadius   = 7;   // corner radius of drag handle

        // ── Events ───────────────────────────────────────────────────────
        public event EventHandler<ZoomChangedEventArgs>? ZoomChanged;
        public event EventHandler? CropModeChanged;
        public event EventHandler? GuidesChanged;

        // ── Public state ─────────────────────────────────────────────────
        public Size? ImageSize => _image is null ? null : new Size(_image.Width, _image.Height);
        public double ZoomFactor => _zoomFactor;
        public bool IsCropMode => _isCropMode;
        public bool GuidesVisible => _guidesVisible;

        // Guides are in image-pixel coordinates; gaps are already in image pixels.
        public GuideMeasurements GetGuideMeasurements() => new(
            Left:   _guideLeftInner   - _guideLeftOuter,
            Right:  _guideRightOuter  - _guideRightInner,
            Top:    _guideTopInner    - _guideTopOuter,
            Bottom: _guideBottomOuter - _guideBottomInner);

        public ImageCanvas()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(30, 30, 30);
            Cursor = Cursors.Hand;
        }

        // ── Image loading ────────────────────────────────────────────────

        public void ClearImage()
        {
            _image?.Dispose();
            _image = null;
            _dpiX = 96f;
            ExitCropModeInternal();
            _guidesVisible = false;
            _zoomFactor = 1.0;
            _panOffset = PointF.Empty;
            ZoomChanged?.Invoke(this, new ZoomChangedEventArgs(_zoomFactor));
            GuidesChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void LoadImage(string path)
        {
            // Load via MemoryStream so GDI+ never holds a file lock.
            // A locked file can block cleanup when the app closes.
            Image newImage;
            using (var ms = new System.IO.MemoryStream(System.IO.File.ReadAllBytes(path)))
                newImage = Image.FromStream(ms);

            _image?.Dispose();
            _image = newImage;
            _dpiX = (newImage as Bitmap)?.HorizontalResolution is float dpi and > 0 ? dpi : 96f;
            ExitCropModeInternal();
            FitToWindow();
            InitGuides();
        }

        // ── Zoom ─────────────────────────────────────────────────────────

        public void ZoomIn() => ApplyZoomStep(1.2);
        public void ZoomOut() => ApplyZoomStep(1.0 / 1.2);

        public void SetZoom(double zoom)
        {
            var center = new PointF(ClientSize.Width / 2f, ClientSize.Height / 2f);
            ZoomAroundPoint(center, zoom);
        }

        public void FitToWindow()
        {
            if (_image is null) return;
            float scaleX = (float)ClientSize.Width / _image.Width;
            float scaleY = (float)ClientSize.Height / _image.Height;
            double newZoom = Math.Min(scaleX, scaleY) * 0.97;
            _zoomFactor = Math.Clamp(newZoom, 0.05, 20.0);
            _panOffset = new PointF(
                (ClientSize.Width  - (float)(_image.Width  * _zoomFactor)) / 2f,
                (ClientSize.Height - (float)(_image.Height * _zoomFactor)) / 2f);
            ZoomChanged?.Invoke(this, new ZoomChangedEventArgs(_zoomFactor));
            Invalidate();
        }

        private void ApplyZoomStep(double factor)
        {
            var center = new PointF(ClientSize.Width / 2f, ClientSize.Height / 2f);
            ZoomAroundPoint(center, _zoomFactor * factor);
        }

        private void ZoomAroundPoint(PointF point, double newZoom)
        {
            newZoom = Math.Clamp(newZoom, 0.05, 20.0);
            PointF imagePoint = new(
                (point.X - _panOffset.X) / (float)_zoomFactor,
                (point.Y - _panOffset.Y) / (float)_zoomFactor);
            _zoomFactor = newZoom;
            _panOffset = new PointF(
                point.X - imagePoint.X * (float)_zoomFactor,
                point.Y - imagePoint.Y * (float)_zoomFactor);
            ZoomChanged?.Invoke(this, new ZoomChangedEventArgs(_zoomFactor));
            Invalidate();
        }

        // ── Crop ─────────────────────────────────────────────────────────

        public void BeginCropMode()
        {
            if (_image is null) return;
            _isCropMode = true;
            _isCropSelecting = false;
            Cursor = Cursors.Cross;
            CropModeChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void ApplyCrop()
        {
            if (_image is null || !_isCropMode) return;

            var canvasRect = GetNormalizedCropRect();
            if (canvasRect.Width < 4 || canvasRect.Height < 4) { CancelCrop(); return; }

            var imageRect = CanvasRectToImageRect(canvasRect);
            imageRect = Rectangle.Intersect(imageRect, new Rectangle(0, 0, _image.Width, _image.Height));
            if (imageRect.IsEmpty) { CancelCrop(); return; }

            var cropped = new Bitmap(imageRect.Width, imageRect.Height);
            using (var g = Graphics.FromImage(cropped))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_image,
                    new Rectangle(0, 0, imageRect.Width, imageRect.Height),
                    imageRect, GraphicsUnit.Pixel);
            }

            _image.Dispose();
            _image = cropped;
            ExitCropModeInternal();
            FitToWindow();
            InitGuides();
        }

        public void CancelCrop()
        {
            ExitCropModeInternal();
            Invalidate();
        }

        private void ExitCropModeInternal()
        {
            if (!_isCropMode) return;
            _isCropMode = false;
            _isCropSelecting = false;
            _hasCropSelection = false;
            _draggingCropHandle = CropHandle.None;
            Cursor = Cursors.Hand;
            CropModeChanged?.Invoke(this, EventArgs.Empty);
        }

        private Rectangle GetNormalizedCropRect()
        {
            if (_hasCropSelection)
                return new Rectangle(_cropLeft, _cropTop,
                    _cropRight - _cropLeft, _cropBottom - _cropTop);

            int x = Math.Min(_cropStart.X, _cropCurrent.X);
            int y = Math.Min(_cropStart.Y, _cropCurrent.Y);
            int w = Math.Abs(_cropCurrent.X - _cropStart.X);
            int h = Math.Abs(_cropCurrent.Y - _cropStart.Y);
            return new Rectangle(x, y, w, h);
        }

        private Rectangle CanvasRectToImageRect(Rectangle canvasRect)
        {
            int x = (int)Math.Round((canvasRect.X - _panOffset.X) / _zoomFactor);
            int y = (int)Math.Round((canvasRect.Y - _panOffset.Y) / _zoomFactor);
            int w = (int)Math.Round(canvasRect.Width / _zoomFactor);
            int h = (int)Math.Round(canvasRect.Height / _zoomFactor);
            return new Rectangle(x, y, w, h);
        }

        // ── Crop to outer borders ────────────────────────────────────────

        public void CropToOuterBorders()
        {
            if (_image is null || !_guidesVisible) return;

            float cropX = _guideLeftOuter;
            float cropY = _guideTopOuter;

            var cropRect = Rectangle.Intersect(
                new Rectangle(
                    (int)Math.Round(cropX),
                    (int)Math.Round(cropY),
                    (int)Math.Round(_guideRightOuter  - cropX),
                    (int)Math.Round(_guideBottomOuter - cropY)),
                new Rectangle(0, 0, _image.Width, _image.Height));

            if (cropRect.Width < 1 || cropRect.Height < 1) return;

            var cropped = new Bitmap(cropRect.Width, cropRect.Height);
            using (var g = Graphics.FromImage(cropped))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_image,
                    new Rectangle(0, 0, cropRect.Width, cropRect.Height),
                    cropRect, GraphicsUnit.Pixel);
            }

            _image.Dispose();
            _image = cropped;

            // Shift all guides by the crop origin so they remain at the same
            // spots on the card content.  Outer guides land at 0 / image edge.
            _guideLeftOuter   -= cropX;
            _guideLeftInner   -= cropX;
            _guideRightInner  -= cropX;
            _guideRightOuter  -= cropX;
            _guideTopOuter    -= cropY;
            _guideTopInner    -= cropY;
            _guideBottomInner -= cropY;
            _guideBottomOuter -= cropY;

            FitToWindow();
        }

        // ── Save ─────────────────────────────────────────────────────────

        public void SaveImage(string path, ImageFormat format, long jpegQuality = 95L)
        {
            if (_image is null) return;
            using var bmp = BuildSaveBitmap();
            WriteBitmap(bmp, path, format, jpegQuality);
        }

        public void SaveWithOverlay(string path, ImageFormat format, long jpegQuality = 95L)
        {
            if (_image is null) return;
            using var bmp = BuildSaveBitmap();
            if (_guidesVisible)
            {
                using var g = Graphics.FromImage(bmp);
                g.PageUnit          = GraphicsUnit.Pixel;   // guides are in image-pixel coords
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                RenderOverlayOnto(g, bmp.Width, bmp.Height);
            }
            WriteBitmap(bmp, path, format, jpegQuality);
        }

        // Copies _image into a fresh 32bppArgb bitmap at its native resolution.
        private Bitmap BuildSaveBitmap()
        {
            var bmp = new Bitmap(_image!.Width, _image.Height, PixelFormat.Format32bppArgb);
            bmp.SetResolution(
                (_image as Bitmap)?.HorizontalResolution ?? 96f,
                (_image as Bitmap)?.VerticalResolution   ?? 96f);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(_image, 0, 0, bmp.Width, bmp.Height);
            return bmp;
        }

        // Draws guide lines, shaded regions, measurement labels, and the info panel
        // directly in image-pixel coordinates (no zoom/pan transforms).
        private void RenderOverlayOnto(Graphics g, int imgW, int imgH)
        {
            // Scale all sizes relative to the smaller image dimension so the
            // overlay looks proportionate on both tiny and large images.
            float scale  = Math.Max(1f, Math.Min(imgW, imgH) / 700f);
            float lineW  = Math.Max(1.5f, scale * 1.5f);

            float lox = _guideLeftOuter;
            float lix = _guideLeftInner;
            float rix = _guideRightInner;
            float rox = _guideRightOuter;
            float toy = _guideTopOuter;
            float tiy = _guideTopInner;
            float biy = _guideBottomInner;
            float boy = _guideBottomOuter;

            // Shaded border regions
            using var fillBrush = new SolidBrush(Color.FromArgb(70, 0, 200, 255));
            g.FillRectangle(fillBrush, lox, 0,   lix - lox, imgH);
            g.FillRectangle(fillBrush, rix, 0,   rox - rix, imgH);
            g.FillRectangle(fillBrush, 0,   toy, imgW,      tiy - toy);
            g.FillRectangle(fillBrush, 0,   biy, imgW,      boy - biy);

            // Guide lines
            using var guidePen = new Pen(Color.FromArgb(220, 0, 200, 255), lineW);
            g.DrawLine(guidePen, lox, 0,    lox, imgH);
            g.DrawLine(guidePen, lix, 0,    lix, imgH);
            g.DrawLine(guidePen, rix, 0,    rix, imgH);
            g.DrawLine(guidePen, rox, 0,    rox, imgH);
            g.DrawLine(guidePen, 0,   toy,  imgW, toy);
            g.DrawLine(guidePen, 0,   tiy,  imgW, tiy);
            g.DrawLine(guidePen, 0,   biy,  imgW, biy);
            g.DrawLine(guidePen, 0,   boy,  imgW, boy);

            // Measurement labels in guide gaps
            var m = GetGuideMeasurements();
            using var labelFont = new Font("Segoe UI", 8f * scale, FontStyle.Bold);
            DrawSaveMeasLabel(g, $"L: {(int)Math.Round(m.Left)}px",   (lox + lix) / 2f, imgH / 2f,  labelFont, vertical: true);
            DrawSaveMeasLabel(g, $"R: {(int)Math.Round(m.Right)}px",  (rix + rox) / 2f, imgH / 2f,  labelFont, vertical: true);
            DrawSaveMeasLabel(g, $"T: {(int)Math.Round(m.Top)}px",    imgW / 2f, (toy + tiy) / 2f,  labelFont, vertical: false);
            DrawSaveMeasLabel(g, $"B: {(int)Math.Round(m.Bottom)}px", imgW / 2f, (biy + boy) / 2f,  labelFont, vertical: false);

            // Info panel (right side, top-anchored)
            DrawSaveInfoPanel(g, imgW, imgH, scale);
        }

        private static void DrawSaveMeasLabel(Graphics g, string text,
            float cx, float cy, Font font, bool vertical)
        {
            SizeF sz = g.MeasureString(text, font);
            if (vertical)
            {
                g.TranslateTransform(cx, cy);
                g.RotateTransform(-90f);
                using var bg = new SolidBrush(Color.FromArgb(190, 10, 10, 10));
                using var fg = new SolidBrush(Color.FromArgb(255, 0, 220, 255));
                g.FillRectangle(bg, -sz.Width / 2f - 3, -sz.Height / 2f - 2, sz.Width + 6, sz.Height + 4);
                g.DrawString(text, font, fg, -sz.Width / 2f, -sz.Height / 2f);
                g.ResetTransform();
            }
            else
            {
                float x = cx - sz.Width  / 2f;
                float y = cy - sz.Height / 2f;
                using var bg = new SolidBrush(Color.FromArgb(190, 10, 10, 10));
                using var fg = new SolidBrush(Color.FromArgb(255, 0, 220, 255));
                g.FillRectangle(bg, x - 3, y - 2, sz.Width + 6, sz.Height + 4);
                g.DrawString(text, font, fg, x, y);
            }
        }

        private void DrawSaveInfoPanel(Graphics g, int imgW, int imgH, float scale)
        {
            var m = GetGuideMeasurements();
            float dpi = _dpiX;

            float leftIn   = m.Left   / dpi;
            float rightIn  = m.Right  / dpi;
            float topIn    = m.Top    / dpi;
            float bottomIn = m.Bottom / dpi;

            float lr   = m.Left + m.Right;
            int lPct = lr > 0 ? (int)Math.Round(m.Left / lr * 100) : 0;
            int rPct = 100 - lPct;

            float tb   = m.Top + m.Bottom;
            int tPct = tb > 0 ? (int)Math.Round(m.Top / tb * 100) : 0;
            int bPct = 100 - tPct;

            using var headerFont = new Font("Segoe UI", 8.5f * scale, FontStyle.Bold);
            using var dataFont   = new Font("Segoe UI", 8.5f * scale);
            using var smallFont  = new Font("Segoe UI", 7.5f * scale);

            var rows = new (string Text, Font Font, bool IsHeader)[]
            {
                ("Border Analysis",          headerFont, true),
                ($"@ {dpi:F0} dpi",          smallFont,  false),
                ("",                         dataFont,   false),
                ("Left:",                    headerFont, false),
                ($"  {leftIn:F3}\"",         dataFont,   false),
                ("Right:",                   headerFont, false),
                ($"  {rightIn:F3}\"",        dataFont,   false),
                ("Top:",                     headerFont, false),
                ($"  {topIn:F3}\"",          dataFont,   false),
                ("Bottom:",                  headerFont, false),
                ($"  {bottomIn:F3}\"",       dataFont,   false),
                ("",                         dataFont,   false),
                ("L / R Ratio:",             headerFont, false),
                ($"  {lPct}  /  {rPct}",     dataFont,   false),
                ("",                         dataFont,   false),
                ("T / B Ratio:",             headerFont, false),
                ($"  {tPct}  /  {bPct}",     dataFont,   false),
            };

            float lineH  = headerFont.GetHeight(g) + 3f * scale;
            float panelH = rows.Length * lineH + 14f * scale;
            float panelW = 0f;
            foreach (var (text, font, _) in rows)
                panelW = Math.Max(panelW, g.MeasureString(text.Length > 0 ? text : " ", font).Width);
            panelW += 20f * scale;

            // Position inside the inner guide boundaries with a small inset so the
            // panel sits clear of the guide lines themselves.
            float margin = 10f * scale;
            float px = _guideRightInner - panelW - margin;
            float py = _guideTopInner   + margin;

            using var bgBrush     = new SolidBrush(Color.FromArgb(210, 12, 12, 12));
            using var borderPen   = new Pen(Color.FromArgb(100, 0, 200, 255), Math.Max(1f, scale));
            using var headerBrush = new SolidBrush(Color.FromArgb(0, 220, 255));
            using var textBrush   = new SolidBrush(Color.FromArgb(210, 210, 210));
            using var dimBrush    = new SolidBrush(Color.FromArgb(130, 130, 130));

            var panelRect = new RectangleF(px - 8 * scale, py - 4 * scale, panelW + 16 * scale, panelH);
            g.FillRectangle(bgBrush, panelRect);
            g.DrawRectangle(borderPen, panelRect.X, panelRect.Y, panelRect.Width, panelRect.Height);

            float ty = py + 2f * scale;
            for (int i = 0; i < rows.Length; i++)
            {
                var (text, font, isHeader) = rows[i];
                if (text.Length == 0) { ty += lineH; continue; }

                SizeF sz = g.MeasureString(text, font);
                float tx = px + panelW - sz.Width;

                var brush = i == 0 ? headerBrush
                          : i == 1 ? dimBrush
                          : isHeader ? headerBrush
                          : textBrush;

                g.DrawString(text, font, brush, tx, ty);
                ty += lineH;
            }
        }

        private static void WriteBitmap(Bitmap bmp, string path, ImageFormat format, long quality)
        {
            if (format.Guid == ImageFormat.Jpeg.Guid)
            {
                var codec = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
                    .First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                using var ep = new System.Drawing.Imaging.EncoderParameters(1);
                ep.Param[0] = new System.Drawing.Imaging.EncoderParameter(
                    System.Drawing.Imaging.Encoder.Quality, quality);
                bmp.Save(path, codec, ep);
            }
            else
            {
                bmp.Save(path, format);
            }
        }

        // ── Auto-detect borders ──────────────────────────────────────────

        private const int DetectSamples = 60; // pixels sampled per row/col

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public float PixelWhiteThreshold { get; set; } = 0.80f;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public float LineWhiteThreshold  { get; set; } = 0.55f;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public float BorderTolerancePct  { get; set; } = 0.006f;

        public void AutoDetectBorders()
        {
            if (_image is null) return;

            var prevCursor = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                using var bmp = new Bitmap(_image.Width, _image.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                    g.DrawImage(_image, 0, 0);

                var (pixels, stride) = ReadPixelData(bmp);
                int w = bmp.Width, h = bmp.Height;

                // Build full whiteness profiles for each edge direction
                float[] profL = BuildProfile(pixels, stride, w, h, true,  false, PixelWhiteThreshold);
                float[] profR = BuildProfile(pixels, stride, w, h, true,  true,  PixelWhiteThreshold);
                float[] profT = BuildProfile(pixels, stride, w, h, false, false, PixelWhiteThreshold);
                float[] profB = BuildProfile(pixels, stride, w, h, false, true,  PixelWhiteThreshold);

                int minBorder = Math.Max(2, Math.Min(w, h) / 200);

                // Find sustained white region from each edge; fall back to % defaults
                (int outer, int inner) lEdge = FindWhiteRegion(profL, w / 2, minBorder, LineWhiteThreshold);
                (int outer, int inner) rEdge = FindWhiteRegion(profR, w / 2, minBorder, LineWhiteThreshold);
                (int outer, int inner) tEdge = FindWhiteRegion(profT, h / 2, minBorder, LineWhiteThreshold);
                (int outer, int inner) bEdge = FindWhiteRegion(profB, h / 2, minBorder, LineWhiteThreshold);

                float tolW = w * BorderTolerancePct;
                float tolH = h * BorderTolerancePct;

                // Apply — mirror right/bottom indices back to image space, then add tolerance
                _guideLeftOuter   = lEdge.outer == -1 ? w * 0.05f : Math.Max(0,     lEdge.outer        - tolW);
                _guideLeftInner   = lEdge.inner == -1 ? w * 0.12f :                 lEdge.inner        + tolW;
                _guideRightOuter  = rEdge.outer == -1 ? w * 0.95f : Math.Min(w - 1, w - 1 - rEdge.outer + tolW);
                _guideRightInner  = rEdge.inner == -1 ? w * 0.88f :                 w - 1 - rEdge.inner - tolW;
                _guideTopOuter    = tEdge.outer == -1 ? h * 0.05f : Math.Max(0,     tEdge.outer        - tolH);
                _guideTopInner    = tEdge.inner == -1 ? h * 0.12f :                 tEdge.inner        + tolH;
                _guideBottomOuter = bEdge.outer == -1 ? h * 0.95f : Math.Min(h - 1, h - 1 - bEdge.outer + tolH);
                _guideBottomInner = bEdge.inner == -1 ? h * 0.88f :                 h - 1 - bEdge.inner - tolH;

                // Clamp inner guides so they never cross outer guides
                _guideLeftInner   = Math.Max(_guideLeftInner,   _guideLeftOuter   + 1);
                _guideRightInner  = Math.Min(_guideRightInner,  _guideRightOuter  - 1);
                _guideTopInner    = Math.Max(_guideTopInner,    _guideTopOuter    + 1);
                _guideBottomInner = Math.Min(_guideBottomInner, _guideBottomOuter - 1);
            }
            finally
            {
                Cursor = prevCursor;
            }

            if (!_guidesVisible) _guidesVisible = true;
            GuidesChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        // Build a whiteness profile (0–1 per line) scanning from one edge inward.
        // isColumn=true → profiles columns (left/right borders); false → rows (top/bottom).
        // reversed=true → scan from the far edge.
        private static float[] BuildProfile(byte[] pixels, int stride,
            int w, int h, bool isColumn, bool reversed, float pixelWhiteThreshold)
        {
            int lineCount  = isColumn ? w : h;   // number of lines in scan direction
            int crossCount = isColumn ? h : w;   // pixels across each line
            int step = Math.Max(1, crossCount / DetectSamples);
            var profile = new float[lineCount];

            for (int li = 0; li < lineCount; li++)
            {
                int lineIdx = reversed ? (lineCount - 1 - li) : li;
                int white = 0, total = 0;
                for (int ci = 0; ci < crossCount; ci += step)
                {
                    int idx = isColumn
                        ? ci * stride + lineIdx * 4   // column li: vary row
                        : lineIdx * stride + ci * 4;  // row li: vary col
                    if (idx + 2 >= pixels.Length) break;
                    float brightness = (pixels[idx + 2] + pixels[idx + 1] + pixels[idx]) / (3f * 255f);
                    if (brightness >= pixelWhiteThreshold) white++;
                    total++;
                }
                profile[li] = total > 0 ? (float)white / total : 0f;
            }
            return profile;
        }

        // Smooth profile with a small window then find the whitest sustained white region.
        // All qualifying runs are scored by average whiteness; the highest-scoring one wins.
        // Returns (outerIndex, innerIndex) relative to the scanned edge, or (-1,-1) on failure.
        private static (int outer, int inner) FindWhiteRegion(float[] profile, int limit, int minWidth,
            float lineWhiteThreshold)
        {
            // Smooth with ±2 window
            int n = Math.Min(profile.Length, limit);
            var sm = new float[n];
            for (int i = 0; i < n; i++)
            {
                float sum = 0; int cnt = 0;
                for (int d = -2; d <= 2; d++)
                {
                    int j = i + d;
                    if (j >= 0 && j < n) { sum += profile[j]; cnt++; }
                }
                sm[i] = sum / cnt;
            }

            // Collect all qualifying runs and pick the one with the highest average whiteness
            int bestStart = -1, bestEnd = -1;
            float bestScore = -1f;
            int runStart = -1;

            for (int i = 0; i <= n; i++)
            {
                bool white = i < n && sm[i] >= lineWhiteThreshold;
                if (white)
                {
                    if (runStart < 0) runStart = i;
                }
                else if (runStart >= 0)
                {
                    int runEnd = i - 1;
                    int runLen = runEnd - runStart + 1;
                    if (runLen >= minWidth)
                    {
                        float whiteSum = 0f;
                        for (int j = runStart; j <= runEnd; j++) whiteSum += sm[j];
                        float score = whiteSum / runLen; // average whiteness
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestStart = runStart;
                            bestEnd   = runEnd;
                        }
                    }
                    runStart = -1;
                }
            }

            return bestStart >= 0 ? (bestStart, bestEnd) : (-1, -1);
        }

        // Read all pixel bytes once — far faster than repeated GetPixel calls.
        private static (byte[] pixels, int stride) ReadPixelData(Bitmap bmp)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var pixels = new byte[Math.Abs(data.Stride) * bmp.Height];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            bmp.UnlockBits(data);
            return (pixels, data.Stride);
        }

        // ── Rotation ─────────────────────────────────────────────────────

        public void RotateLeft()  => RotateBy(-RotationStep);
        public void RotateRight() => RotateBy(RotationStep);

        public void RotateBy(float degrees)
        {
            if (_image is null) return;

            double rad = Math.Abs(degrees) * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            int newW = (int)Math.Ceiling(_image.Width  * cos + _image.Height * sin);
            int newH = (int)Math.Ceiling(_image.Width  * sin + _image.Height * cos);

            var rotated = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);
            rotated.SetResolution(
                (_image as Bitmap)?.HorizontalResolution ?? 96f,
                (_image as Bitmap)?.VerticalResolution   ?? 96f);

            using (var g = Graphics.FromImage(rotated))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode   = PixelOffsetMode.HighQuality;
                g.SmoothingMode     = SmoothingMode.HighQuality;
                g.TranslateTransform(newW / 2f, newH / 2f);
                g.RotateTransform(degrees);
                g.TranslateTransform(-_image.Width / 2f, -_image.Height / 2f);
                g.DrawImage(_image, 0, 0, _image.Width, _image.Height);
            }

            _image.Dispose();
            _image = rotated;
            Invalidate();
        }

        // ── Guidelines ───────────────────────────────────────────────────

        public void ToggleGuides()
        {
            _guidesVisible = !_guidesVisible;
            Invalidate();
        }

        public GuidelineTemplate GetGuideTemplate(string name = "Template")
        {
            if (_image is null) throw new InvalidOperationException("No image loaded.");
            float w = _image.Width, h = _image.Height;
            return new GuidelineTemplate
            {
                Name        = name,
                ZoomFactor  = _zoomFactor,
                LeftOuter   = _guideLeftOuter   / w,
                LeftInner   = _guideLeftInner   / w,
                RightInner  = _guideRightInner  / w,
                RightOuter  = _guideRightOuter  / w,
                TopOuter    = _guideTopOuter    / h,
                TopInner    = _guideTopInner    / h,
                BottomInner = _guideBottomInner / h,
                BottomOuter = _guideBottomOuter / h,
            };
        }

        public void ApplyGuideTemplate(GuidelineTemplate t)
        {
            if (_image is null) throw new InvalidOperationException("No image loaded.");
            float w = _image.Width, h = _image.Height;
            _guideLeftOuter   = (float)(t.LeftOuter   * w);
            _guideLeftInner   = (float)(t.LeftInner   * w);
            _guideRightInner  = (float)(t.RightInner  * w);
            _guideRightOuter  = (float)(t.RightOuter  * w);
            _guideTopOuter    = (float)(t.TopOuter    * h);
            _guideTopInner    = (float)(t.TopInner    * h);
            _guideBottomInner = (float)(t.BottomInner * h);
            _guideBottomOuter = (float)(t.BottomOuter * h);
            _guidesVisible = true;
            GuidesChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        // Guides are stored in image-pixel coordinates so they stay attached to the image
        // regardless of zoom or pan.
        private void InitGuides()
        {
            if (_image is null) return;
            float w = _image.Width;
            float h = _image.Height;
            _guideLeftOuter   = w * 0.05f;
            _guideLeftInner   = w * 0.12f;
            _guideRightInner  = w * 0.88f;
            _guideRightOuter  = w * 0.95f;
            _guideTopOuter    = h * 0.05f;
            _guideTopInner    = h * 0.12f;
            _guideBottomInner = h * 0.88f;
            _guideBottomOuter = h * 0.95f;
        }

        private GuideId HitTestGuide(Point pt)
        {
            if (!_guidesVisible || _image is null || _isCropMode) return GuideId.None;

            float midX = ClientSize.Width  / 2f;
            float midY = ClientSize.Height / 2f;
            int   hw   = HandleWidth  / 2;
            int   hl   = HandleLength / 2;

            // Handle zones — wider perpendicular hit area at the centre of the viewport
            if (InHandle(pt, ImgToCanX(_guideLeftOuter),  midY, true,  hw, hl)) return GuideId.LeftOuter;
            if (InHandle(pt, ImgToCanX(_guideLeftInner),  midY, true,  hw, hl)) return GuideId.LeftInner;
            if (InHandle(pt, ImgToCanX(_guideRightInner), midY, true,  hw, hl)) return GuideId.RightInner;
            if (InHandle(pt, ImgToCanX(_guideRightOuter), midY, true,  hw, hl)) return GuideId.RightOuter;
            if (InHandle(pt, midX, ImgToCanY(_guideTopOuter),    false, hw, hl)) return GuideId.TopOuter;
            if (InHandle(pt, midX, ImgToCanY(_guideTopInner),    false, hw, hl)) return GuideId.TopInner;
            if (InHandle(pt, midX, ImgToCanY(_guideBottomInner), false, hw, hl)) return GuideId.BottomInner;
            if (InHandle(pt, midX, ImgToCanY(_guideBottomOuter), false, hw, hl)) return GuideId.BottomOuter;

            // Fallback: line hit anywhere along the guide (original behaviour)
            if (Math.Abs(pt.X - ImgToCanX(_guideLeftOuter))   <= GuideHitSlop) return GuideId.LeftOuter;
            if (Math.Abs(pt.X - ImgToCanX(_guideLeftInner))   <= GuideHitSlop) return GuideId.LeftInner;
            if (Math.Abs(pt.X - ImgToCanX(_guideRightInner))  <= GuideHitSlop) return GuideId.RightInner;
            if (Math.Abs(pt.X - ImgToCanX(_guideRightOuter))  <= GuideHitSlop) return GuideId.RightOuter;
            if (Math.Abs(pt.Y - ImgToCanY(_guideTopOuter))    <= GuideHitSlop) return GuideId.TopOuter;
            if (Math.Abs(pt.Y - ImgToCanY(_guideTopInner))    <= GuideHitSlop) return GuideId.TopInner;
            if (Math.Abs(pt.Y - ImgToCanY(_guideBottomInner)) <= GuideHitSlop) return GuideId.BottomInner;
            if (Math.Abs(pt.Y - ImgToCanY(_guideBottomOuter)) <= GuideHitSlop) return GuideId.BottomOuter;

            return GuideId.None;
        }

        private static bool InHandle(Point pt, float cx, float cy, bool vertical, int halfW, int halfL)
        {
            float dx = Math.Abs(pt.X - cx);
            float dy = Math.Abs(pt.Y - cy);
            return vertical ? (dx <= halfW && dy <= halfL) : (dx <= halfL && dy <= halfW);
        }

        private void MoveGuide(GuideId guide, Point mousePos)
        {
            if (_image is null) return;
            float imgX    = CanToImgX(mousePos.X);
            float imgY    = CanToImgY(mousePos.Y);
            const float minGap = 1f; // image pixels

            switch (guide)
            {
                case GuideId.LeftOuter:
                    _guideLeftOuter   = Math.Clamp(imgX, 0,                     _guideLeftInner   - minGap); break;
                case GuideId.LeftInner:
                    _guideLeftInner   = Math.Clamp(imgX, _guideLeftOuter  + minGap, _guideRightInner - minGap); break;
                case GuideId.RightInner:
                    _guideRightInner  = Math.Clamp(imgX, _guideLeftInner  + minGap, _guideRightOuter - minGap); break;
                case GuideId.RightOuter:
                    _guideRightOuter  = Math.Clamp(imgX, _guideRightInner + minGap, _image.Width);   break;
                case GuideId.TopOuter:
                    _guideTopOuter    = Math.Clamp(imgY, 0,                     _guideTopInner    - minGap); break;
                case GuideId.TopInner:
                    _guideTopInner    = Math.Clamp(imgY, _guideTopOuter   + minGap, _guideBottomInner - minGap); break;
                case GuideId.BottomInner:
                    _guideBottomInner = Math.Clamp(imgY, _guideTopInner   + minGap, _guideBottomOuter - minGap); break;
                case GuideId.BottomOuter:
                    _guideBottomOuter = Math.Clamp(imgY, _guideBottomInner + minGap, _image.Height); break;
            }
        }

        private static Cursor CursorForGuide(GuideId guide) => guide switch
        {
            GuideId.LeftOuter or GuideId.LeftInner or
            GuideId.RightInner or GuideId.RightOuter => Cursors.SizeWE,
            GuideId.TopOuter or GuideId.TopInner or
            GuideId.BottomInner or GuideId.BottomOuter => Cursors.SizeNS,
            _ => Cursors.Hand
        };

        // ── Coordinate helpers ───────────────────────────────────────────

        private float ImgToCanX(float ix) => ix * (float)_zoomFactor + _panOffset.X;
        private float ImgToCanY(float iy) => iy * (float)_zoomFactor + _panOffset.Y;
        private float CanToImgX(float cx) => (cx - _panOffset.X) / (float)_zoomFactor;
        private float CanToImgY(float cy) => (cy - _panOffset.Y) / (float)_zoomFactor;

        // ── Painting ─────────────────────────────────────────────────────

        protected override void OnPaint(PaintEventArgs e)
        {
            if (IsDisposed) return;
            base.OnPaint(e);

            if (_image is null)
            {
                DrawPlaceholder(e.Graphics);
                return;
            }

            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            e.Graphics.DrawImage(_image, new RectangleF(
                _panOffset.X, _panOffset.Y,
                (float)(_image.Width  * _zoomFactor),
                (float)(_image.Height * _zoomFactor)));

            if (_guidesVisible && !_isCropMode)
            {
                DrawGuides(e.Graphics);
                DrawInfoPanel(e.Graphics);
            }

            if (_isCropMode)
                DrawCropOverlay(e.Graphics);
        }

        private void DrawPlaceholder(Graphics g)
        {
            string text = "Open an image to begin  (Ctrl+O)";
            using var font = new Font("Segoe UI", 14f, FontStyle.Regular);
            using var brush = new SolidBrush(Color.FromArgb(90, 90, 90));
            var size = g.MeasureString(text, font);
            g.DrawString(text, font, brush,
                (ClientSize.Width  - size.Width)  / 2f,
                (ClientSize.Height - size.Height) / 2f);
        }

        private void DrawGuides(Graphics g)
        {
            // Convert image-pixel guide positions to canvas coordinates for drawing
            float lox = ImgToCanX(_guideLeftOuter);
            float lix = ImgToCanX(_guideLeftInner);
            float rix = ImgToCanX(_guideRightInner);
            float rox = ImgToCanX(_guideRightOuter);
            float toy = ImgToCanY(_guideTopOuter);
            float tiy = ImgToCanY(_guideTopInner);
            float biy = ImgToCanY(_guideBottomInner);
            float boy = ImgToCanY(_guideBottomOuter);

            float canW = ClientSize.Width;
            float canH = ClientSize.Height;

            using var fillBrush = new SolidBrush(Color.FromArgb(45, 0, 200, 255));
            using var guidePen  = new Pen(Color.FromArgb(210, 0, 200, 255), 1f);

            // ── Filled border regions ──────────────────────────────────
            g.FillRectangle(fillBrush, lox, 0, lix - lox, canH);   // left
            g.FillRectangle(fillBrush, rix, 0, rox - rix, canH);   // right
            g.FillRectangle(fillBrush, 0, toy, canW, tiy - toy);   // top
            g.FillRectangle(fillBrush, 0, biy, canW, boy - biy);   // bottom

            // ── Guide lines ────────────────────────────────────────────
            g.DrawLine(guidePen, lox, 0, lox, canH);
            g.DrawLine(guidePen, lix, 0, lix, canH);
            g.DrawLine(guidePen, rix, 0, rix, canH);
            g.DrawLine(guidePen, rox, 0, rox, canH);
            g.DrawLine(guidePen, 0, toy, canW, toy);
            g.DrawLine(guidePen, 0, tiy, canW, tiy);
            g.DrawLine(guidePen, 0, biy, canW, biy);
            g.DrawLine(guidePen, 0, boy, canW, boy);

            // ── Measurement labels ─────────────────────────────────────
            // When two paired guides are so close that their handles would cover the label,
            // shift the label away from the handle centre (midY for vertical pairs,
            // midX for horizontal pairs). L/R shift in opposite directions, as do T/B,
            // so the labels never land on top of each other when multiple pairs are narrow.
            var m = GetGuideMeasurements();
            const float safeZone = HandleLength + 12f;       // canvas-pixel gap threshold
            const float shiftV   = HandleLength / 2f + 24f;  // Y-shift for L/R labels (~label height/2 is small)
            const float shiftH   = HandleLength / 2f + 54f;  // X-shift for T/B labels (labels are wider, need more room)
            float midX = canW / 2f;
            float midY = canH / 2f;

            DrawMeasurementLabel(g, $"L: {(int)Math.Round(m.Left)}px",
                (lox + lix) / 2f,
                lix - lox < safeZone ? midY - shiftV : midY,
                vertical: true);
            DrawMeasurementLabel(g, $"R: {(int)Math.Round(m.Right)}px",
                (rix + rox) / 2f,
                rox - rix < safeZone ? midY + shiftV : midY,
                vertical: true);
            DrawMeasurementLabel(g, $"T: {(int)Math.Round(m.Top)}px",
                tiy - toy < safeZone ? midX + shiftH : midX,
                (toy + tiy) / 2f,
                vertical: false);
            DrawMeasurementLabel(g, $"B: {(int)Math.Round(m.Bottom)}px",
                boy - biy < safeZone ? midX - shiftH : midX,
                (biy + boy) / 2f,
                vertical: false);

            // ── Drag handles ───────────────────────────────────────────
            DrawGuideHandles(g, lox, lix, rix, rox, toy, tiy, biy, boy);
        }

        private void DrawInfoPanel(Graphics g)
        {
            if (_image is null) return;

            var m   = GetGuideMeasurements();
            float dpi = _dpiX;

            float leftIn   = m.Left   / dpi;
            float rightIn  = m.Right  / dpi;
            float topIn    = m.Top    / dpi;
            float bottomIn = m.Bottom / dpi;

            float lr = m.Left + m.Right;
            int lPct = lr > 0 ? (int)Math.Round(m.Left / lr * 100) : 0;
            int rPct = 100 - lPct;

            float tb = m.Top + m.Bottom;
            int tPct = tb > 0 ? (int)Math.Round(m.Top / tb * 100) : 0;
            int bPct = 100 - tPct;

            using var headerFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var dataFont   = new Font("Segoe UI", 8.5f);
            using var smallFont  = new Font("Segoe UI", 7.5f);

            // Build rows: (text, font, isSectionHeader)
            var rows = new (string Text, Font Font, bool IsHeader)[]
            {
                ("Border Analysis",          headerFont, true),
                ($"@ {dpi:F0} dpi",          smallFont,  false),
                ("",                         dataFont,   false),
                ("Left:",                    headerFont, false),
                ($"  {leftIn:F3}\"",         dataFont,   false),
                ("Right:",                   headerFont, false),
                ($"  {rightIn:F3}\"",        dataFont,   false),
                ("Top:",                     headerFont, false),
                ($"  {topIn:F3}\"",          dataFont,   false),
                ("Bottom:",                  headerFont, false),
                ($"  {bottomIn:F3}\"",       dataFont,   false),
                ("",                         dataFont,   false),
                ("L / R Ratio:",             headerFont, false),
                ($"  {lPct}  /  {rPct}",     dataFont,   false),
                ("",                         dataFont,   false),
                ("T / B Ratio:",             headerFont, false),
                ($"  {tPct}  /  {bPct}",     dataFont,   false),
            };

            // Measure panel size
            float lineH    = headerFont.GetHeight(g) + 3f;
            float panelH   = rows.Length * lineH + 14f;
            float panelW   = 0f;
            foreach (var (text, font, _) in rows)
                panelW = Math.Max(panelW, g.MeasureString(text.Length > 0 ? text : " ", font).Width);
            panelW += 20f;

            float px = ClientSize.Width - panelW - 12f;
            float py = 12f;

            // Background + border
            using var bgBrush     = new SolidBrush(Color.FromArgb(210, 12, 12, 12));
            using var borderPen   = new Pen(Color.FromArgb(100, 0, 200, 255), 1f);
            using var headerBrush = new SolidBrush(Color.FromArgb(0, 220, 255));
            using var textBrush   = new SolidBrush(Color.FromArgb(210, 210, 210));
            using var dimBrush    = new SolidBrush(Color.FromArgb(130, 130, 130));

            var panelRect = new RectangleF(px - 8, py - 4, panelW + 16, panelH);
            g.FillRectangle(bgBrush, panelRect);
            g.DrawRectangle(borderPen, panelRect.X, panelRect.Y, panelRect.Width, panelRect.Height);

            // Draw rows right-aligned within the panel
            float ty = py + 2f;
            for (int i = 0; i < rows.Length; i++)
            {
                var (text, font, isHeader) = rows[i];
                if (text.Length == 0) { ty += lineH; continue; }

                SizeF sz = g.MeasureString(text, font);
                float tx = px + panelW - sz.Width; // right-align

                var brush = i == 0 ? headerBrush       // title
                          : i == 1 ? dimBrush           // dpi sub-line
                          : isHeader ? headerBrush      // section labels
                          : textBrush;                  // values

                g.DrawString(text, font, brush, tx, ty);
                ty += lineH;
            }
        }

        private static void DrawMeasurementLabel(Graphics g, string text, float cx, float cy, bool vertical)
        {
            using var font    = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var bgBrush = new SolidBrush(Color.FromArgb(190, 10, 10, 10));
            using var fgBrush = new SolidBrush(Color.FromArgb(255, 0, 220, 255));

            SizeF sz = g.MeasureString(text, font);
            float x = cx - sz.Width  / 2f;
            float y = cy - sz.Height / 2f;

            // If the gap between guides is too narrow for the label, shift it outside
            // (caller passes the centre of the gap; if label doesn't fit, nudge it away)
            g.FillRectangle(bgBrush, x - 3, y - 2, sz.Width + 6, sz.Height + 4);
            g.DrawString(text, font, fgBrush, x, y);
        }

        private void DrawGuideHandles(Graphics g,
            float lox, float lix, float rix, float rox,
            float toy, float tiy, float biy, float boy)
        {
            float midX = ClientSize.Width  / 2f;
            float midY = ClientSize.Height / 2f;

            var prev = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // (canvas-x, canvas-y, isVertical, guideId)
            var handles = new (float cx, float cy, bool vert, GuideId id)[]
            {
                (lox,  midY, true,  GuideId.LeftOuter),
                (lix,  midY, true,  GuideId.LeftInner),
                (rix,  midY, true,  GuideId.RightInner),
                (rox,  midY, true,  GuideId.RightOuter),
                (midX, toy,  false, GuideId.TopOuter),
                (midX, tiy,  false, GuideId.TopInner),
                (midX, biy,  false, GuideId.BottomInner),
                (midX, boy,  false, GuideId.BottomOuter),
            };

            using var normalBg = new SolidBrush(Color.FromArgb(215, 10, 28, 40));
            using var activeBg = new SolidBrush(Color.FromArgb(230, 0,  80, 120));
            using var rimPen   = new Pen(Color.FromArgb(255, 0, 200, 255), 1.5f);
            using var dotBrush = new SolidBrush(Color.FromArgb(200, 0, 200, 255));

            foreach (var (cx, cy, vert, id) in handles)
                DrawHandle(g, cx, cy, vert, id == _draggingGuide ? activeBg : normalBg, rimPen, dotBrush);

            g.SmoothingMode = prev;
        }

        private static void DrawHandle(Graphics g, float cx, float cy, bool vertical,
            Brush bgBrush, Pen rimPen, Brush dotBrush)
        {
            float rx = vertical ? cx - HandleWidth  / 2f : cx - HandleLength / 2f;
            float ry = vertical ? cy - HandleLength / 2f : cy - HandleWidth  / 2f;
            float rw = vertical ? HandleWidth  : HandleLength;
            float rh = vertical ? HandleLength : HandleWidth;

            using var path = MakeRoundedRectPath(rx, ry, rw, rh, HandleRadius);
            g.FillPath(bgBrush, path);
            g.DrawPath(rimPen, path);

            // Three grip dots centred in the handle
            const float dotR = 1.8f;
            const float gap  = 5f;
            for (int i = -1; i <= 1; i++)
            {
                float dx = vertical ? 0f : i * gap;
                float dy = vertical ? i * gap : 0f;
                g.FillEllipse(dotBrush, cx + dx - dotR, cy + dy - dotR, dotR * 2, dotR * 2);
            }
        }

        private static GraphicsPath MakeRoundedRectPath(float x, float y, float w, float h, int r)
        {
            int d = r * 2;
            var path = new GraphicsPath();
            path.AddArc(x,         y,         d, d, 180, 90);
            path.AddArc(x + w - d, y,         d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d,   0, 90);
            path.AddArc(x,         y + h - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }

        private void DrawCropOverlay(Graphics g)
        {
            var sel = GetNormalizedCropRect();
            bool hasSelection = sel.Width > 2 && sel.Height > 2;

            using var overlayBrush = new SolidBrush(Color.FromArgb(140, 0, 0, 0));

            if (!hasSelection)
            {
                g.FillRectangle(overlayBrush, ClientRectangle);
                return;
            }

            g.FillRectangle(overlayBrush, 0, 0, ClientSize.Width, sel.Top);
            g.FillRectangle(overlayBrush, 0, sel.Bottom, ClientSize.Width, ClientSize.Height - sel.Bottom);
            g.FillRectangle(overlayBrush, 0, sel.Top, sel.Left, sel.Height);
            g.FillRectangle(overlayBrush, sel.Right, sel.Top, ClientSize.Width - sel.Right, sel.Height);

            using var shadowPen = new Pen(Color.FromArgb(180, 0, 0, 0), 2f);
            g.DrawRectangle(shadowPen, sel);

            using var whitePen = new Pen(Color.White, 1f) { DashStyle = DashStyle.Dash };
            g.DrawRectangle(whitePen, sel);

            const int handleSize = 7;
            using var handleBrush = new SolidBrush(Color.White);
            using var handlePen   = new Pen(Color.FromArgb(180, 0, 0, 0), 1f);
            foreach (var handle in GetHandleRects(sel, handleSize))
            {
                g.FillRectangle(handleBrush, handle);
                g.DrawRectangle(handlePen, handle);
            }
        }

        private static IEnumerable<Rectangle> GetHandleRects(Rectangle sel, int size)
        {
            int h = size / 2;
            int mx = sel.Left + sel.Width  / 2;
            int my = sel.Top  + sel.Height / 2;
            yield return new Rectangle(sel.Left  - h, sel.Top    - h, size, size);
            yield return new Rectangle(sel.Right - h, sel.Top    - h, size, size);
            yield return new Rectangle(sel.Left  - h, sel.Bottom - h, size, size);
            yield return new Rectangle(sel.Right - h, sel.Bottom - h, size, size);
            yield return new Rectangle(mx - h, sel.Top    - h, size, size);
            yield return new Rectangle(mx - h, sel.Bottom - h, size, size);
            yield return new Rectangle(sel.Left  - h, my - h, size, size);
            yield return new Rectangle(sel.Right - h, my - h, size, size);
        }

        // ── Crop handle helpers ──────────────────────────────────────────

        private CropHandle HitTestCropHandle(Point pt)
        {
            if (!_hasCropSelection) return CropHandle.None;
            const int slop = 8;
            int mx = (_cropLeft + _cropRight)  / 2;
            int my = (_cropTop  + _cropBottom) / 2;

            // Corners take priority over edges
            if (Near(pt, _cropLeft,  _cropTop,    slop)) return CropHandle.TopLeft;
            if (Near(pt, _cropRight, _cropTop,    slop)) return CropHandle.TopRight;
            if (Near(pt, _cropLeft,  _cropBottom, slop)) return CropHandle.BottomLeft;
            if (Near(pt, _cropRight, _cropBottom, slop)) return CropHandle.BottomRight;
            if (Near(pt, mx,         _cropTop,    slop)) return CropHandle.TopCenter;
            if (Near(pt, mx,         _cropBottom, slop)) return CropHandle.BottomCenter;
            if (Near(pt, _cropLeft,  my,          slop)) return CropHandle.MiddleLeft;
            if (Near(pt, _cropRight, my,          slop)) return CropHandle.MiddleRight;
            return CropHandle.None;
        }

        private static bool Near(Point pt, int x, int y, int slop)
            => Math.Abs(pt.X - x) <= slop && Math.Abs(pt.Y - y) <= slop;

        private void MoveCropHandle(CropHandle handle, Point pt)
        {
            const int min = 4;
            switch (handle)
            {
                case CropHandle.TopLeft:
                    _cropLeft = Math.Min(pt.X, _cropRight  - min);
                    _cropTop  = Math.Min(pt.Y, _cropBottom - min); break;
                case CropHandle.TopCenter:
                    _cropTop  = Math.Min(pt.Y, _cropBottom - min); break;
                case CropHandle.TopRight:
                    _cropRight = Math.Max(pt.X, _cropLeft + min);
                    _cropTop   = Math.Min(pt.Y, _cropBottom - min); break;
                case CropHandle.MiddleLeft:
                    _cropLeft  = Math.Min(pt.X, _cropRight  - min); break;
                case CropHandle.MiddleRight:
                    _cropRight = Math.Max(pt.X, _cropLeft   + min); break;
                case CropHandle.BottomLeft:
                    _cropLeft   = Math.Min(pt.X, _cropRight  - min);
                    _cropBottom = Math.Max(pt.Y, _cropTop    + min); break;
                case CropHandle.BottomCenter:
                    _cropBottom = Math.Max(pt.Y, _cropTop    + min); break;
                case CropHandle.BottomRight:
                    _cropRight  = Math.Max(pt.X, _cropLeft   + min);
                    _cropBottom = Math.Max(pt.Y, _cropTop    + min); break;
            }
        }

        private static Cursor CursorForCropHandle(CropHandle handle) => handle switch
        {
            CropHandle.TopLeft     or CropHandle.BottomRight  => Cursors.SizeNWSE,
            CropHandle.TopRight    or CropHandle.BottomLeft   => Cursors.SizeNESW,
            CropHandle.TopCenter   or CropHandle.BottomCenter => Cursors.SizeNS,
            CropHandle.MiddleLeft  or CropHandle.MiddleRight  => Cursors.SizeWE,
            _ => Cursors.Cross
        };

        // ── Mouse ────────────────────────────────────────────────────────

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (_isCropMode) return;
            double factor = e.Delta > 0 ? 1.15 : 1.0 / 1.15;
            ZoomAroundPoint(e.Location, _zoomFactor * factor);
            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) { base.OnMouseDown(e); return; }

            if (_isCropMode)
            {
                // Try to grab a handle on an existing selection first
                var handle = HitTestCropHandle(e.Location);
                if (handle != CropHandle.None)
                {
                    _draggingCropHandle = handle;
                }
                else
                {
                    // Start a fresh selection, discarding any previous one
                    _hasCropSelection = false;
                    _draggingCropHandle = CropHandle.None;
                    _isCropSelecting = true;
                    _cropStart = _cropCurrent = e.Location;
                    Cursor = Cursors.Cross;
                }
            }
            else
            {
                var hit = HitTestGuide(e.Location);
                if (hit != GuideId.None)
                {
                    _draggingGuide = hit;
                }
                else
                {
                    _isDragging = true;
                    _dragStart = e.Location;
                    _panAtDragStart = _panOffset;
                    Cursor = Cursors.SizeAll;
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isCropMode)
            {
                if (_draggingCropHandle != CropHandle.None)
                {
                    MoveCropHandle(_draggingCropHandle, e.Location);
                    Invalidate();
                }
                else if (_isCropSelecting)
                {
                    _cropCurrent = e.Location;
                    Invalidate();
                }
                else
                {
                    // Hover: update cursor based on which handle (if any) is under the mouse
                    var handle = HitTestCropHandle(e.Location);
                    Cursor = CursorForCropHandle(handle);
                }
                base.OnMouseMove(e);
                return;
            }

            if (_draggingGuide != GuideId.None)
            {
                MoveGuide(_draggingGuide, e.Location);
                GuidesChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
            else if (_isDragging)
            {
                _panOffset = new PointF(
                    _panAtDragStart.X + (e.X - _dragStart.X),
                    _panAtDragStart.Y + (e.Y - _dragStart.Y));
                Invalidate();
            }
            else
            {
                var hit = HitTestGuide(e.Location);
                Cursor = CursorForGuide(hit);
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_draggingCropHandle != CropHandle.None)
                {
                    _draggingCropHandle = CropHandle.None;
                    Cursor = CursorForCropHandle(HitTestCropHandle(e.Location));
                }
                else if (_isCropSelecting)
                {
                    _cropCurrent = e.Location;
                    _isCropSelecting = false;

                    // Normalize and promote to four-edge representation
                    var r = new Rectangle(
                        Math.Min(_cropStart.X, _cropCurrent.X),
                        Math.Min(_cropStart.Y, _cropCurrent.Y),
                        Math.Abs(_cropCurrent.X - _cropStart.X),
                        Math.Abs(_cropCurrent.Y - _cropStart.Y));

                    if (r.Width >= 4 && r.Height >= 4)
                    {
                        _cropLeft = r.Left; _cropRight  = r.Right;
                        _cropTop  = r.Top;  _cropBottom = r.Bottom;
                        _hasCropSelection = true;
                    }
                    Invalidate();
                }
                else if (_draggingGuide != GuideId.None)
                {
                    _draggingGuide = GuideId.None;
                    Cursor = CursorForGuide(HitTestGuide(e.Location));
                }
                else if (_isDragging)
                {
                    _isDragging = false;
                    Cursor = Cursors.Hand;
                }
            }

            base.OnMouseUp(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_image is null) return;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Null out _image before base.Dispose so any WM_PAINT triggered
                // by handle destruction finds nothing to draw and exits cleanly.
                var img = _image;
                _image = null;
                img?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
