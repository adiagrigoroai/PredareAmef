using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PredareAmef.Ui
{
    internal enum FelButon { Principal, Secundar, Periculos, PeInchis }

    /// <summary>
    /// Buton desenat de noi: colturi rotunjite, fara chenarul gri al Windows-ului,
    /// cu o umbra de apasare si stare de trecere cu mausul.
    /// </summary>
    internal class Buton : Control
    {
        private bool _peste, _apasat;

        public FelButon Fel { get; set; } = FelButon.Secundar;
        public int Raza { get; set; } = 10;

        public Buton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Paleta.CorpTare;
            Height = 40;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _peste = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _peste = false; _apasat = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _apasat = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _apasat = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);

            Color fundal, text, contur = Color.Transparent;
            switch (Fel)
            {
                case FelButon.Principal:
                    fundal = _apasat ? Paleta.AccentApasat : (_peste ? Paleta.AccentApasat : Paleta.Accent);
                    text = Color.White;
                    break;
                case FelButon.Periculos:
                    fundal = _peste ? Paleta.EroareFundal : Paleta.Card;
                    text = Paleta.Eroare;
                    contur = Color.FromArgb(0xE6, 0xC8, 0xC8);
                    break;
                case FelButon.PeInchis:
                    fundal = _apasat || _peste ? Color.FromArgb(0x59, 0xB1, 0xF0) : Paleta.AccentViu;
                    text = Color.FromArgb(0x06, 0x1B, 0x2B);
                    break;
                default:
                    fundal = _peste ? Paleta.Subtil : Paleta.Card;
                    text = Paleta.Text;
                    contur = Paleta.Margine;
                    break;
            }

            if (!Enabled)
            {
                fundal = Fel == FelButon.Principal ? Color.FromArgb(0xB8, 0xC6, 0xD4) : Paleta.Subtil;
                text = Paleta.TextSters;
            }

            using (var cale = Paleta.Rotunjit(r, Raza))
            using (var b = new SolidBrush(fundal))
            {
                e.Graphics.FillPath(b, cale);
                if (contur != Color.Transparent)
                    using (var p = new Pen(contur)) e.Graphics.DrawPath(p, cale);
            }

            TextRenderer.DrawText(e.Graphics, Text, Font, r, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>Un rand din meniul lateral: pictograma desenata cu linii si eticheta.</summary>
    internal class RandMeniu : Control
    {
        private bool _peste;
        private bool _activ;

        public bool Activ
        {
            get { return _activ; }
            set { _activ = value; Invalidate(); }
        }

        /// <summary>Ce desenam in stanga: "aparate", "predare", "rezultate", "setari".</summary>
        public string Pictograma { get; set; } = "aparate";

        public RandMeniu()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Font = Paleta.Corp;
            Height = 40;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _peste = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _peste = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);

            if (_activ || _peste)
            {
                using (var cale = Paleta.Rotunjit(r, 10))
                using (var b = new SolidBrush(_activ ? Paleta.AccentFundal : Paleta.Subtil))
                    e.Graphics.FillPath(b, cale);
            }

            Color culoare = _activ ? Paleta.AccentText : Paleta.TextSecundar;
            DeseneazaPictograma(e.Graphics, new Rectangle(12, (Height - 17) / 2, 17, 17), culoare);

            var zonaText = new Rectangle(40, 0, Width - 48, Height);
            TextRenderer.DrawText(e.Graphics, Text, _activ ? Paleta.CorpTare : Font, zonaText, culoare,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void DeseneazaPictograma(Graphics g, Rectangle r, Color culoare)
        {
            using (var p = new Pen(culoare, 1.7f))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                switch (Pictograma)
                {
                    case "predare":
                        g.DrawLine(p, r.Left + 1, r.Top + r.Height / 2, r.Right - 3, r.Top + r.Height / 2);
                        g.DrawLine(p, r.Right - 7, r.Top + 3, r.Right - 3, r.Top + r.Height / 2);
                        g.DrawLine(p, r.Right - 7, r.Bottom - 3, r.Right - 3, r.Top + r.Height / 2);
                        break;
                    case "rezultate":
                        g.DrawLine(p, r.Left + 1, r.Top + 3, r.Right - 1, r.Top + 3);
                        g.DrawLine(p, r.Left + 1, r.Top + r.Height / 2, r.Right - 1, r.Top + r.Height / 2);
                        g.DrawLine(p, r.Left + 1, r.Bottom - 3, r.Left + r.Width / 2, r.Bottom - 3);
                        break;
                    case "setari":
                        g.DrawEllipse(p, r.Left + 5, r.Top + 5, 7, 7);
                        g.DrawEllipse(p, r.Left + 1, r.Top + 1, 15, 15);
                        break;
                    default: // aparate
                        g.DrawRectangle(p, r.Left + 1, r.Top + 2, r.Width - 3, r.Height - 8);
                        g.DrawLine(p, r.Left + 4, r.Bottom - 2, r.Right - 4, r.Bottom - 2);
                        break;
                }
            }
        }
    }
}
