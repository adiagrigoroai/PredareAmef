using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PredareAmef.Ui
{
    /// <summary>Card alb cu colturi rotunjite si o margine subtire, ca in macheta.</summary>
    internal class Card : Panel
    {
        public int Raza { get; set; } = 14;
        public Color Fundal { get; set; } = Paleta.Card;
        public Color Contur { get; set; } = Paleta.Margine;

        public Card()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var cale = Paleta.Rotunjit(r, Raza))
            using (var umplere = new SolidBrush(Fundal))
            using (var creion = new Pen(Contur))
            {
                e.Graphics.FillPath(umplere, cale);
                if (Contur != Color.Transparent) e.Graphics.DrawPath(creion, cale);
            }
            base.OnPaint(e);
        }
    }

    /// <summary>Eticheta rotunda de stare: „pregatit”, „gata”, „hartie”.</summary>
    internal class Pastila : Control
    {
        public Color Fundal { get; set; } = Paleta.AccentFundal;
        public Color Culoare { get; set; } = Paleta.AccentText;

        public Pastila()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Font = Paleta.MicTare;
            Height = 22;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var cale = Paleta.Rotunjit(r, Height / 2))
            using (var umplere = new SolidBrush(Fundal))
                e.Graphics.FillPath(umplere, cale);

            TextRenderer.DrawText(e.Graphics, Text, Font, r, Culoare,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        /// <summary>Latimea potrivita textului, ca pastila sa nu fie nici stramta, nici lata degeaba.</summary>
        public void PotrivesteLatimea()
        {
            using (var g = CreateGraphics())
                Width = TextRenderer.MeasureText(g, Text, Font).Width + 22;
        }
    }

    /// <summary>Bara de progres, desenata simplu: un jgheab si o umplere.</summary>
    internal class BaraProgres : Control
    {
        private double _valoare;

        /// <summary>Intre 0 si 1.</summary>
        public double Valoare
        {
            get { return _valoare; }
            set { _valoare = value < 0 ? 0 : (value > 1 ? 1 : value); Invalidate(); }
        }

        public Color Umplere { get; set; } = Paleta.Accent;
        public Color Jgheab { get; set; } = Color.FromArgb(0xE4, 0xE9, 0xEF);

        public BaraProgres()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Height = 8;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var cale = Paleta.Rotunjit(r, Height / 2))
            using (var b = new SolidBrush(Jgheab))
                e.Graphics.FillPath(b, cale);

            int latime = (int)(r.Width * _valoare);
            if (latime < 2) return;
            using (var cale = Paleta.Rotunjit(new Rectangle(0, 0, latime, r.Height), Height / 2))
            using (var b = new SolidBrush(Umplere))
                e.Graphics.FillPath(b, cale);
        }
    }
}
