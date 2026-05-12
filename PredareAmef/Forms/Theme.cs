using System.Drawing;
using System.Windows.Forms;

namespace PredareAmef.Forms
{
    /// <summary>
    /// Paleta de culori + helpers pentru stilizare consistenta (Light Flat).
    /// Inspirat din Fluent Design + Material You. Apelat din toate form-urile.
    /// </summary>
    internal static class Theme
    {
        // ── Paleta light flat ──────────────────────────────────────────────────
        public static readonly Color Background    = Color.FromArgb(0xF8, 0xF9, 0xFB); // body
        public static readonly Color Surface       = Color.White;                       // carduri
        public static readonly Color Surface2      = Color.FromArgb(0xF1, 0xF3, 0xF5); // input bg / row alt
        public static readonly Color Border        = Color.FromArgb(0xE0, 0xE4, 0xE9);
        public static readonly Color BorderFocus   = Color.FromArgb(0x93, 0xC5, 0xFD);

        public static readonly Color Primary       = Color.FromArgb(0x25, 0x63, 0xEB); // blue-600
        public static readonly Color PrimaryHover  = Color.FromArgb(0x1D, 0x4E, 0xD8); // blue-700
        public static readonly Color PrimaryActive = Color.FromArgb(0x1E, 0x40, 0xAF); // blue-800

        public static readonly Color Success       = Color.FromArgb(0x16, 0xA3, 0x4A); // green-600
        public static readonly Color SuccessHover  = Color.FromArgb(0x15, 0x80, 0x3D);
        public static readonly Color Danger        = Color.FromArgb(0xDC, 0x26, 0x26); // red-600
        public static readonly Color DangerHover   = Color.FromArgb(0xB9, 0x1C, 0x1C);
        public static readonly Color Warning       = Color.FromArgb(0xEA, 0xB3, 0x08); // yellow-500
        public static readonly Color WarningHover  = Color.FromArgb(0xCA, 0x8A, 0x04);

        public static readonly Color Text          = Color.FromArgb(0x1F, 0x29, 0x37); // gray-800
        public static readonly Color TextDim       = Color.FromArgb(0x6B, 0x72, 0x80); // gray-500
        public static readonly Color TextMuted     = Color.FromArgb(0x9C, 0xA3, 0xAF); // gray-400

        // ── Fonts ──────────────────────────────────────────────────────────────
        public static readonly Font FontBase       = new Font("Segoe UI", 9F);
        public static readonly Font FontBaseBold   = new Font("Segoe UI", 9F, FontStyle.Bold);
        public static readonly Font FontTitle      = new Font("Segoe UI Semibold", 14F, FontStyle.Regular);
        public static readonly Font FontSubtitle   = new Font("Segoe UI", 11F, FontStyle.Regular);
        public static readonly Font FontSmall      = new Font("Segoe UI", 8F);
        public static readonly Font FontButton     = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        public static readonly Font FontMono       = new Font("Consolas", 9F);

        // ── Helpers de stilizare ───────────────────────────────────────────────
        public static void ApplyFormStyle(Form f)
        {
            f.BackColor = Background;
            f.ForeColor = Text;
            f.Font = FontBase;
        }

        public static void ApplyButtonPrimary(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Primary;
            b.ForeColor = Color.White;
            b.Font = FontButton;
            b.Cursor = Cursors.Hand;
            b.FlatAppearance.MouseOverBackColor = PrimaryHover;
            b.FlatAppearance.MouseDownBackColor = PrimaryActive;
            b.Height = System.Math.Max(b.Height, 34);
        }

        public static void ApplyButtonSecondary(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Border;
            b.BackColor = Surface;
            b.ForeColor = Text;
            b.Font = FontButton;
            b.Cursor = Cursors.Hand;
            b.FlatAppearance.MouseOverBackColor = Surface2;
            b.FlatAppearance.MouseDownBackColor = Border;
            b.Height = System.Math.Max(b.Height, 34);
        }

        public static void ApplyButtonSuccess(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Success;
            b.ForeColor = Color.White;
            b.Font = FontButton;
            b.Cursor = Cursors.Hand;
            b.FlatAppearance.MouseOverBackColor = SuccessHover;
            b.Height = System.Math.Max(b.Height, 34);
        }

        public static void ApplyButtonDanger(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Danger;
            b.ForeColor = Color.White;
            b.Font = FontButton;
            b.Cursor = Cursors.Hand;
            b.FlatAppearance.MouseOverBackColor = DangerHover;
            b.Height = System.Math.Max(b.Height, 34);
        }

        public static void ApplyButtonWarning(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Warning;
            b.ForeColor = Color.White;
            b.Font = FontButton;
            b.Cursor = Cursors.Hand;
            b.FlatAppearance.MouseOverBackColor = WarningHover;
            b.Height = System.Math.Max(b.Height, 34);
        }

        public static void ApplyTextBox(TextBox t)
        {
            t.BorderStyle = BorderStyle.FixedSingle;
            t.BackColor = Surface;
            t.ForeColor = Text;
            t.Font = FontBase;
        }

        public static void ApplyComboBox(ComboBox c)
        {
            c.FlatStyle = FlatStyle.Flat;
            c.BackColor = Surface;
            c.ForeColor = Text;
            c.Font = FontBase;
        }

        public static void ApplyDataGridView(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.BackgroundColor = Surface;
            dgv.BorderStyle = BorderStyle.None;
            dgv.GridColor = Border;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.RowTemplate.Height = 30;
            dgv.Font = FontBase;
            dgv.DefaultCellStyle.BackColor = Surface;
            dgv.DefaultCellStyle.ForeColor = Text;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0xDB, 0xEA, 0xFE); // blue-100
            dgv.DefaultCellStyle.SelectionForeColor = Text;
            dgv.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Surface2;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Surface2;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextDim;
            dgv.ColumnHeadersDefaultCellStyle.Font = FontBaseBold;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Surface2;
            dgv.ColumnHeadersHeight = 36;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        }

        /// <summary>Card cu fundal alb si border subtire (pentru sectiuni vizuale).</summary>
        public static Panel MakeCard()
        {
            var p = new Panel
            {
                BackColor = Surface,
                Padding = new Padding(14)
            };
            p.Paint += (s, e) =>
            {
                using (var pen = new Pen(Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            return p;
        }

        /// <summary>Linie orizontala separator subtire.</summary>
        public static Panel MakeSeparator()
        {
            return new Panel { Height = 1, BackColor = Border, Dock = DockStyle.Top };
        }

        /// <summary>Label pentru titlu sectiune.</summary>
        public static Label MakeSectionLabel(string text)
        {
            return new Label
            {
                Text = text.ToUpper(),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = TextDim,
                AutoSize = true
            };
        }
    }
}
