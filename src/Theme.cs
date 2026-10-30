using System;
using System.Drawing;
using System.Windows.Forms;

namespace DriversGet
{
    // TrimKit-inspired dark palette and helpers so the WinForms app matches
    // the look of the WPF TrimKit project as closely as WinForms allows.
    public static class Theme
    {
        public static readonly Color Background = FromHex("#202124");
        public static readonly Color Sidebar = FromHex("#1C1C1F");
        public static readonly Color Panel = FromHex("#28292D");
        public static readonly Color Card = FromHex("#1C1C1F");
        public static readonly Color CardBorder = FromHex("#303336");
        public static readonly Color Input = FromHex("#292A2D");
        public static readonly Color ChromeBg = FromHex("#35363A");
        public static readonly Color ChromeHover = FromHex("#444750");
        public static readonly Color ChromePressed = FromHex("#5F6368");

        public static readonly Color Text = FromHex("#E8EAED");
        public static readonly Color TextBright = FromHex("#FFFFFF");
        public static readonly Color TextMuted = FromHex("#80868E");
        public static readonly Color TextDim = FromHex("#5F6368");

        public static readonly Color Accent = FromHex("#8AB4F8");
        public static readonly Color Green = FromHex("#81C995");
        public static readonly Color Danger = FromHex("#F28B82");

        public static readonly string FontFamily = PickFont();

        public static Font Font(float size)
        {
            return new Font(FontFamily, size, FontStyle.Regular, GraphicsUnit.Point);
        }

        public static Font Font(float size, FontStyle style)
        {
            return new Font(FontFamily, size, style, GraphicsUnit.Point);
        }

        private static string PickFont()
        {
            // Prefer Segoe UI Variable (Win11), fall back to Segoe UI.
            foreach (string candidate in new string[] { "Segoe UI Variable Display", "Segoe UI Variable", "Segoe UI" })
            {
                if (FontInstalled(candidate))
                    return candidate;
            }
            return "Segoe UI";
        }

        private static bool FontInstalled(string name)
        {
            try
            {
                using (Font f = new Font(name, 10f))
                {
                    return string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { return false; }
        }

        public static Color FromHex(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                return Color.FromArgb(r, g, b);
            }
            return Color.Gray;
        }

        // A flat, rounded-ish button styled like TrimKit's ChromeButton/AccentButton.
        public static Button MakeButton(string text, bool accent)
        {
            Button b = new Button();
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = accent ? 0 : 1;
            b.FlatAppearance.BorderColor = accent ? Accent : CardBorder;
            b.UseVisualStyleBackColor = false;
            b.Cursor = Cursors.Hand;
            b.Font = Font(9.5f, accent ? FontStyle.Bold : FontStyle.Regular);
            b.AutoSize = false;
            b.Height = 34;

            if (accent)
            {
                b.BackColor = Accent;
                b.ForeColor = Background;
                b.FlatAppearance.MouseOverBackColor = FromHex("#A6C6FA");
                b.FlatAppearance.MouseDownBackColor = FromHex("#C2D7FC");
            }
            else
            {
                b.BackColor = ChromeBg;
                b.ForeColor = TextBright;
                b.FlatAppearance.MouseOverBackColor = ChromeHover;
                b.FlatAppearance.MouseDownBackColor = ChromePressed;
            }
            return b;
        }

        public static void StyleTextBox(TextBox t)
        {
            t.BackColor = Input;
            t.ForeColor = TextBright;
            t.BorderStyle = BorderStyle.FixedSingle;
            t.Font = Font(9.5f);
        }
    }
}
