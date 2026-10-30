using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DriversGet
{
    // A rounded, bordered panel used for the supplier cards, painted to match
    // TrimKit's card look (dark fill, subtle border, rounded corners).
    public class CardPanel : Panel
    {
        public int CornerRadius { get; set; }
        public Color BorderColor { get; set; }

        public CardPanel()
        {
            CornerRadius = 10;
            BorderColor = Theme.CardBorder;
            BackColor = Theme.Panel;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedRect(rect, CornerRadius))
            {
                // Parent background outside the rounded region.
                using (SolidBrush bg = new SolidBrush(Parent != null ? Parent.BackColor : Theme.Background))
                {
                    g.FillRectangle(bg, ClientRectangle);
                }
                using (SolidBrush fill = new SolidBrush(BackColor))
                {
                    g.FillPath(fill, path);
                }
                using (Pen pen = new Pen(BorderColor, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                path.CloseFigure();
                return path;
            }
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
