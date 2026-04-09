namespace CardAnalysis
{
    /// <summary>
    /// A flat, dark ToolStrip renderer with a subtle accent highlight on hover/press.
    /// </summary>
    internal class ModernRenderer : ToolStripRenderer
    {
        // Palette
        private static readonly Color BgColor       = Color.FromArgb(30,  30,  30);
        private static readonly Color HoverColor     = Color.FromArgb(55,  55,  55);
        private static readonly Color PressedColor   = Color.FromArgb(0,  120, 215);  // Windows accent blue
        private static readonly Color CheckedColor   = Color.FromArgb(0,   90, 160);
        private static readonly Color TextColor      = Color.FromArgb(230, 230, 230);
        private static readonly Color TextDisabled   = Color.FromArgb(100, 100, 100);
        private static readonly Color SeparatorColor = Color.FromArgb(70,  70,  70);

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(BgColor);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var item = (ToolStripButton)e.Item;
            var g = e.Graphics;
            var r = new Rectangle(2, 2, item.Width - 4, item.Height - 4);

            if (!item.Enabled)
            {
                // nothing — transparent
            }
            else if (item.Pressed || item.Checked)
            {
                using var b = new SolidBrush(item.Pressed ? PressedColor : CheckedColor);
                g.FillRoundedRect(b, r, 4);
            }
            else if (item.Selected)
            {
                using var b = new SolidBrush(HoverColor);
                g.FillRoundedRect(b, r, 4);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? TextColor : TextDisabled;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var g = e.Graphics;
            int x = e.Item.Width / 2;
            int margin = 5;
            using var pen = new Pen(SeparatorColor);
            g.DrawLine(pen, x, margin, x, e.Item.Height - margin);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // No border under the toolbar — clean edge
        }

        protected override void OnRenderLabelBackground(ToolStripItemRenderEventArgs e)
        {
            // Transparent labels (zoom %, etc.)
        }
    }

    /// <summary>
    /// Color table that paints the MenuStrip and its dropdowns in the same dark palette.
    /// </summary>
    internal class DarkMenuColorTable : ProfessionalColorTable
    {
        private static readonly Color BgDark     = Color.FromArgb(30,  30,  30);
        private static readonly Color BgDrop     = Color.FromArgb(40,  40,  40);
        private static readonly Color Hover      = Color.FromArgb(55,  55,  55);
        private static readonly Color Border     = Color.FromArgb(65,  65,  65);
        private static readonly Color Accent     = Color.FromArgb(0,  120, 215);

        // Menu bar background
        public override Color MenuStripGradientBegin => BgDark;
        public override Color MenuStripGradientEnd   => BgDark;

        // Top-level item hover
        public override Color MenuItemSelectedGradientBegin => Hover;
        public override Color MenuItemSelectedGradientEnd   => Hover;
        public override Color MenuItemSelected              => Hover;
        public override Color MenuItemBorder                => Border;

        // Dropdown panel
        public override Color ToolStripDropDownBackground  => BgDrop;
        public override Color ImageMarginGradientBegin     => BgDrop;
        public override Color ImageMarginGradientMiddle    => BgDrop;
        public override Color ImageMarginGradientEnd       => BgDrop;
        public override Color MenuBorder                   => Border;

        // Dropdown item hover
        public override Color MenuItemPressedGradientBegin => Accent;
        public override Color MenuItemPressedGradientEnd   => Accent;

        public override Color ToolStripBorder => Border;
        public override Color SeparatorDark   => Border;
        public override Color SeparatorLight  => Border;
    }

    /// <summary>
    /// Color table that paints the StatusStrip in the same dark tone as the toolbar.
    /// </summary>
    internal class DarkStatusColorTable : ProfessionalColorTable
    {
        private static readonly Color Bg = Color.FromArgb(22, 22, 22);
        public override Color StatusStripGradientBegin => Bg;
        public override Color StatusStripGradientEnd   => Bg;
        public override Color ToolStripBorder          => Color.FromArgb(50, 50, 50);
    }

    internal static class GraphicsExtensions
    {
        public static void FillRoundedRect(this Graphics g, Brush brush, Rectangle r, int radius)
        {
            int d = radius * 2;
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }
}
