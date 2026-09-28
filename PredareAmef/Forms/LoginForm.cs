using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using PredareAmef.Ui;

namespace PredareAmef.Forms
{
    /// <summary>
    /// Parola de la pornirea aplicatiei, in aceeasi haina ca restul interfetei:
    /// fereastra fara chenarul Windows, un card alb si un singur camp.
    /// </summary>
    public sealed class LoginForm : Form
    {
        private const string PAROLA_ASTEPTATA = "0841";
        private const int INCERCARI_MAXIME = 3;

        private readonly TextBox _parola = new TextBox();
        private readonly Label _mesaj = new Label();
        private int _incercari;

        public LoginForm()
        {
            Text = "Predare AMEF";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(420, 330);
            BackColor = Paleta.Fundal;
            Font = Paleta.Corp;
            Paleta.FaraPalpaire(this);

            var card = new Card
            {
                Bounds = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height),
                Raza = 18,
                Dock = DockStyle.Fill,
            };
            Controls.Add(card);

            // ── sigla ──
            var sigla = new Card
            {
                Bounds = new Rectangle(40, 38, 44, 44),
                Raza = 12,
                Fundal = Paleta.Accent,
                Contur = Color.Transparent,
            };
            sigla.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = new Pen(Color.White, 2f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    e.Graphics.DrawRectangle(p, 11, 12, 22, 15);
                    e.Graphics.DrawLine(p, 16, 32, 28, 32);
                }
            };
            card.Controls.Add(sigla);

            card.Controls.Add(new Label
            {
                Text = "Predare AMEF",
                Font = new Font("Segoe UI Semibold", 16F),
                ForeColor = Paleta.Text,
                AutoSize = true,
                Location = new Point(96, 42),
                BackColor = Color.Transparent,
            });
            card.Controls.Add(new Label
            {
                Text = "Utilitar predare memorie fiscală",
                Font = Paleta.Mic,
                ForeColor = Paleta.TextSecundar,
                AutoSize = true,
                Location = new Point(98, 70),
                BackColor = Color.Transparent,
            });

            // ── campul de parola ──
            card.Controls.Add(new Label
            {
                Text = "Parola",
                Font = Paleta.CorpTare,
                ForeColor = Paleta.TextSecundar,
                AutoSize = true,
                Location = new Point(40, 126),
                BackColor = Color.Transparent,
            });

            var ramaCamp = new Card
            {
                Bounds = new Rectangle(40, 150, 340, 46),
                Raza = 11,
                Fundal = Color.White,
                Contur = Paleta.Margine,
            };
            _parola.BorderStyle = BorderStyle.None;
            _parola.UseSystemPasswordChar = true;
            _parola.Font = new Font("Segoe UI", 13F);
            _parola.BackColor = Color.White;
            _parola.ForeColor = Paleta.Text;
            _parola.Bounds = new Rectangle(14, 13, 312, 24);
            _parola.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Verifica(); }
                if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; Renunta(); }
            };
            ramaCamp.Controls.Add(_parola);
            card.Controls.Add(ramaCamp);

            _mesaj.Font = Paleta.Mic;
            _mesaj.ForeColor = Paleta.Eroare;
            _mesaj.AutoSize = true;
            _mesaj.Location = new Point(42, 204);
            _mesaj.BackColor = Color.Transparent;
            _mesaj.Text = "";
            card.Controls.Add(_mesaj);

            // ── butoane ──
            var anuleaza = new Buton
            {
                Text = "Anulează",
                Fel = FelButon.Secundar,
                Bounds = new Rectangle(40, 250, 130, 44),
            };
            anuleaza.Click += (s, e) => Renunta();

            var conecteaza = new Buton
            {
                Text = "Conectează",
                Fel = FelButon.Principal,
                Bounds = new Rectangle(250, 250, 130, 44),
            };
            conecteaza.Click += (s, e) => Verifica();

            card.Controls.Add(anuleaza);
            card.Controls.Add(conecteaza);

            Shown += (s, e) => _parola.Focus();

            // Fereastra n-are chenar, deci ii dam noi colturile rotunjite ale cardului;
            // altfel se vad patru colturi drepte in jurul lui.
            Resize += (s, e) => RotunjesteFereastra();
            RotunjesteFereastra();
            Paleta.PotrivesteFundalurile(this);
        }

        private void RotunjesteFereastra()
        {
            using (var cale = Paleta.Rotunjit(new Rectangle(0, 0, Width, Height), 18))
                Region = new Region(cale);
        }

        /// <summary>Fereastra n-are bara de titlu, dar se poate muta tragand de ea.</summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) MutaFereastra();
        }

        private void MutaFereastra()
        {
            const int WM_NCLBUTTONDOWN = 0xA1;
            const int HTCAPTION = 0x2;
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        private void Verifica()
        {
            if (_parola.Text == PAROLA_ASTEPTATA)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            _incercari++;
            _parola.Clear();
            _parola.Focus();

            if (_incercari >= INCERCARI_MAXIME)
            {
                MessageBox.Show(this, "Număr maxim de încercări atins. Aplicația se închide.",
                    "Predare AMEF", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            _mesaj.Text = "Parolă greșită (" + _incercari + " din " + INCERCARI_MAXIME + ")";
        }

        private void Renunta()
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    }
}
