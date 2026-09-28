using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PredareAmef.Ui
{
    /// <summary>
    /// Culorile si literele interfetei noi: fundal deschis, carduri albe, accent albastru.
    /// Aceleasi valori ca in macheta.
    /// </summary>
    internal static class Paleta
    {
        public static readonly Color Fundal = Color.FromArgb(0xF3, 0xF5, 0xF8);
        public static readonly Color Card = Color.White;
        public static readonly Color Margine = Color.FromArgb(0xE1, 0xE6, 0xEC);
        public static readonly Color MargineFina = Color.FromArgb(0xED, 0xF1, 0xF5);
        public static readonly Color Subtil = Color.FromArgb(0xFA, 0xFB, 0xFD);

        public static readonly Color Text = Color.FromArgb(0x16, 0x20, 0x2C);
        public static readonly Color TextSecundar = Color.FromArgb(0x5B, 0x6B, 0x7C);
        public static readonly Color TextSters = Color.FromArgb(0x8A, 0x98, 0xA8);

        public static readonly Color Accent = Color.FromArgb(0x11, 0x5E, 0xA8);
        public static readonly Color AccentApasat = Color.FromArgb(0x0C, 0x49, 0x84);
        public static readonly Color AccentFundal = Color.FromArgb(0xE8, 0xF1, 0xFB);
        public static readonly Color AccentText = Color.FromArgb(0x0C, 0x4C, 0x8C);
        public static readonly Color AccentViu = Color.FromArgb(0x3D, 0x9B, 0xE9);

        public static readonly Color Reusit = Color.FromArgb(0x11, 0x7A, 0x56);
        public static readonly Color ReusitFundal = Color.FromArgb(0xE4, 0xF3, 0xEC);
        public static readonly Color Atentie = Color.FromArgb(0x9A, 0x5A, 0x11);
        public static readonly Color AtentieFundal = Color.FromArgb(0xFD, 0xF1, 0xE2);
        public static readonly Color Eroare = Color.FromArgb(0xA8, 0x2B, 0x2B);
        public static readonly Color EroareFundal = Color.FromArgb(0xFB, 0xEA, 0xEA);

        /// <summary>Cardul inchis la culoare, cel cu butonul de pornire.</summary>
        public static readonly Color CardInchis = Color.FromArgb(0x16, 0x22, 0x2E);
        public static readonly Color TextPeInchis = Color.FromArgb(0xC3, 0xD3, 0xE2);

        public static readonly Font Titlu = new Font("Segoe UI Semibold", 17F);
        public static readonly Font TitluCard = new Font("Segoe UI Semibold", 11F);
        public static readonly Font Corp = new Font("Segoe UI", 9.75F);
        public static readonly Font CorpTare = new Font("Segoe UI Semibold", 9.75F);
        public static readonly Font Mic = new Font("Segoe UI", 8.5F);
        public static readonly Font MicTare = new Font("Segoe UI Semibold", 8.5F);
        public static readonly Font Cifra = new Font("Segoe UI Semibold", 19F);
        public static readonly Font Monospatiat = new Font("Consolas", 9F);

        /// <summary>Un dreptunghi cu colturi rotunjite, pentru desenat carduri si butoane.</summary>
        public static GraphicsPath Rotunjit(Rectangle r, int raza)
        {
            int d = raza * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            var p = new GraphicsPath();
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>
        /// Da fiecarei etichete fundalul pe care sta de fapt.
        ///
        /// Windows deseneaza literele mici cu ClearType, folosind subpixelii ecranului. Pe un
        /// fundal transparent nu are peste ce sa le aseze, iar textul capata margini colorate.
        /// Dupa ce interfata e construita, luam fiecare eticheta si ii punem culoarea cardului
        /// sau a paginii pe care se afla.
        /// </summary>
        public static void PotrivesteFundalurile(Control radacina)
        {
            foreach (Control c in radacina.Controls)
            {
                if (c is Label || c is CheckBox || c is RadioButton)
                {
                    Control parinte = c.Parent;
                    while (parinte != null)
                    {
                        var card = parinte as Card;
                        if (card != null) { c.BackColor = card.Fundal; break; }
                        if (parinte.BackColor.A == 255) { c.BackColor = parinte.BackColor; break; }
                        parinte = parinte.Parent;
                    }
                }
                if (c.HasChildren) PotrivesteFundalurile(c);
            }
        }

        /// <summary>Redesenarea fara palpaire; altfel cardurile clipesc la fiecare miscare.</summary>
        public static void FaraPalpaire(Control c)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(c, true, null);
        }
    }
}
