using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using PredareAmef.Services;
using PredareAmef.Ui;

namespace PredareAmef.Forms
{
    /// <summary>
    /// Fereastra aplicatiei: meniu in stanga, patru ecrane in dreapta — Aparate, Predare,
    /// In lucru, Rezultate. Logica ramane in serviciile de pana acum; aici e doar interfata.
    /// </summary>
    public sealed partial class ShellForm : Form
    {
        private const int LatimeMeniu = 248;

        private readonly Panel _continut = new Panel { Dock = DockStyle.Fill, BackColor = Paleta.Fundal };
        private readonly Dictionary<string, RandMeniu> _meniu = new Dictionary<string, RandMeniu>();

        // ── ecrane ──
        private Panel _pagAparate, _pagPredare, _pagProgres, _pagRezultate;

        // ── aparate ──
        private readonly DeviceScannerService _scanner = new DeviceScannerService();
        private List<ScannedDevice> _aparate = new List<ScannedDevice>();
        private readonly Dictionary<string, CheckBox> _bifeAparate = new Dictionary<string, CheckBox>();
        private Panel _listaAparate, _zonaCautare;
        private Label _lblPortCautat;
        private Label _lblSubtitluAparate, _lblNrPorturi, _lblNrAparate, _lblNrZ;
        private Buton _btnScan, _btnContinua;
        private CancellationTokenSource _ctsScan;

        // ── predare ──
        private ComboBox _cbMotiv, _cbUnitate, _cbImprimanta;
        private TextBox _txtPredatDe, _txtObservatii, _txtFolder;
        private RadioButton _rbMemorieDa, _rbMemorieNu;
        private CheckBox _chkPv, _chkEtichete;
        private Label _lblSelectate;

        // ── in lucru ──
        private BaraProgres _progresGeneral;
        private Label _lblProcent, _lblStareRulare;
        private Panel _listaProgres;
        private RichTextBox _jurnal;
        private readonly Dictionary<string, Label> _stariAparate = new Dictionary<string, Label>();
        private readonly Dictionary<string, BaraProgres> _bareAparate = new Dictionary<string, BaraProgres>();
        private MultiSessionRunner _runner;
        private CancellationTokenSource _ctsRulare;
        private Buton _btnOpreste;

        // ── rezultate ──
        private Panel _listaRezultate;
        private Label _lblSubtitluRezultate;

        public ShellForm()
        {
            Text = "Predare AMEF";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1280, 820);
            MinimumSize = new Size(1120, 720);
            BackColor = Paleta.Fundal;
            Font = Paleta.Corp;
            Paleta.FaraPalpaire(this);

            Controls.Add(_continut);
            Controls.Add(ConstruiesteMeniu());

            _pagAparate = ConstruiestePaginaAparate();
            _pagPredare = ConstruiestePaginaPredare();
            _pagProgres = ConstruiestePaginaProgres();
            _pagRezultate = ConstruiestePaginaRezultate();

            foreach (var p in new[] { _pagAparate, _pagPredare, _pagProgres, _pagRezultate })
            {
                p.Visible = false;
                _continut.Controls.Add(p);
            }

            Arata("aparate");
            Paleta.PotrivesteFundalurile(this);
            Shown += (s, e) => PornesteScanare();
        }

        // ══════════════════════════════════════════════════════════════
        //  Meniul lateral
        // ══════════════════════════════════════════════════════════════

        private Control ConstruiesteMeniu()
        {
            var bara = new Panel { Dock = DockStyle.Left, Width = LatimeMeniu, BackColor = Paleta.Card };
            Paleta.FaraPalpaire(bara);
            bara.Paint += (s, e) =>
            {
                using (var p = new Pen(Paleta.Margine))
                    e.Graphics.DrawLine(p, bara.Width - 1, 0, bara.Width - 1, bara.Height);
            };

            var sigla = new Card
            {
                Bounds = new Rectangle(16, 20, 34, 34),
                Raza = 10,
                Fundal = Paleta.Accent,
                Contur = Color.Transparent
            };
            sigla.Paint += (s, e) =>
            {
                using (var p = new Pen(Color.White, 1.8f))
                {
                    p.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    p.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    e.Graphics.DrawRectangle(p, 8, 9, 18, 12);
                    e.Graphics.DrawLine(p, 12, 25, 22, 25);
                }
            };
            bara.Controls.Add(sigla);

            bara.Controls.Add(new Label
            {
                Text = "Predare AMEF",
                Font = new Font("Segoe UI Semibold", 11.5F),
                ForeColor = Paleta.Text,
                AutoSize = true,
                Location = new Point(58, 22),
                BackColor = Color.Transparent
            });
            bara.Controls.Add(new Label
            {
                Text = "versiunea " + VersiuneaAplicatiei(),
                Font = Paleta.Mic,
                ForeColor = Paleta.TextSters,
                AutoSize = true,
                Location = new Point(60, 41),
                BackColor = Color.Transparent
            });

            int y = 76;
            bara.Controls.Add(Sectiune("LUCRU", ref y));
            y += 4;
            bara.Controls.Add(FaRandMeniu("aparate", "Aparate", "aparate", ref y));
            bara.Controls.Add(FaRandMeniu("predare", "Predare", "predare", ref y));
            bara.Controls.Add(FaRandMeniu("progres", "În lucru", "lucru", ref y));
            bara.Controls.Add(FaRandMeniu("rezultate", "Rezultate", "rezultate", ref y));

            var stare = new Card
            {
                Bounds = new Rectangle(16, 0, LatimeMeniu - 32, 66),
                Raza = 12,
            };
            bara.Resize += (s, e) => stare.Location = new Point(16, Math.Max(220, bara.Height - 86));
            var punct = new Card
            {
                Bounds = new Rectangle(14, 16, 8, 8),
                Raza = 4,
                Fundal = Paleta.Reusit,
                Contur = Color.Transparent
            };
            stare.Controls.Add(punct);
            stare.Controls.Add(new Label
            {
                Text = "Conectat la CRM",
                Font = Paleta.CorpTare,
                ForeColor = Paleta.Text,
                AutoSize = true,
                Location = new Point(28, 11),
                BackColor = Color.Transparent
            });
            stare.Controls.Add(new Label
            {
                Text = "crm.itsmartretail.ro",
                Font = Paleta.Mic,
                ForeColor = Paleta.TextSecundar,
                AutoSize = true,
                Location = new Point(14, 34),
                BackColor = Color.Transparent
            });
            bara.Controls.Add(stare);

            return bara;
        }

        /// <summary>
        /// Versiunea aplicatiei. Application.ProductVersion da versiunea gazdei cand
        /// fereastra e pornita din alta parte (un script, un test).
        /// </summary>
        private static string VersiuneaAplicatiei()
        {
            try
            {
                var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return v.Major + "." + v.Minor + "." + v.Build;
            }
            catch { return "-"; }
        }

        private Label Sectiune(string text, ref int y)
        {
            var l = new Label
            {
                Text = text,
                Font = Paleta.MicTare,
                ForeColor = Paleta.TextSters,
                AutoSize = true,
                Location = new Point(24, y),
                BackColor = Color.Transparent
            };
            y += 22;
            return l;
        }

        private RandMeniu FaRandMeniu(string cheie, string text, string pictograma, ref int y)
        {
            var r = new RandMeniu
            {
                Text = text,
                Pictograma = pictograma,
                Bounds = new Rectangle(16, y, LatimeMeniu - 32, 40),
            };
            r.Click += (s, e) => Arata(cheie);
            _meniu[cheie] = r;
            y += 44;
            return r;
        }

        private void Arata(string cheie)
        {
            foreach (var kv in _meniu) kv.Value.Activ = kv.Key == cheie;
            _pagAparate.Visible = cheie == "aparate";
            _pagPredare.Visible = cheie == "predare";
            _pagProgres.Visible = cheie == "progres";
            _pagRezultate.Visible = cheie == "rezultate";
        }

        // ══════════════════════════════════════════════════════════════
        //  Bucati de interfata folosite peste tot
        // ══════════════════════════════════════════════════════════════

        private static Panel PaginaGoala()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Paleta.Fundal, Padding = new Padding(32, 24, 32, 24) };
            Paleta.FaraPalpaire(p);
            return p;
        }

        private static Label Titlu(string text)
        {
            return new Label { Text = text, Font = Paleta.Titlu, ForeColor = Paleta.Text, AutoSize = true, BackColor = Color.Transparent };
        }

        private static Label Subtitlu(string text)
        {
            return new Label { Text = text, Font = Paleta.Corp, ForeColor = Paleta.TextSecundar, AutoSize = true, BackColor = Color.Transparent };
        }

        private static Label Eticheta(string text)
        {
            return new Label { Text = text, Font = Paleta.CorpTare, ForeColor = Paleta.TextSecundar, AutoSize = true, BackColor = Color.Transparent };
        }

        private static TextBox Camp(string valoare, int latime)
        {
            return new TextBox
            {
                Text = valoare,
                Width = latime,
                Font = Paleta.Corp,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = Paleta.Text,
            };
        }

        private static ComboBox Lista(int latime, params string[] valori)
        {
            var c = new ComboBox
            {
                Width = latime,
                Font = Paleta.Corp,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Paleta.Text,
            };
            c.Items.AddRange(valori);
            if (c.Items.Count > 0) c.SelectedIndex = 0;
            return c;
        }

        private static Card CardCifra(string eticheta, string valoare, int x, int latime, out Label valoareLbl)
        {
            var c = new Card { Bounds = new Rectangle(x, 0, latime, 76), Raza = 14 };
            c.Controls.Add(new Label
            {
                Text = eticheta,
                Font = Paleta.Mic,
                ForeColor = Paleta.TextSecundar,
                AutoSize = true,
                Location = new Point(17, 14),
                BackColor = Color.Transparent
            });
            valoareLbl = new Label
            {
                Text = valoare,
                Font = Paleta.Cifra,
                ForeColor = Paleta.Text,
                AutoSize = true,
                Location = new Point(15, 32),
                BackColor = Color.Transparent
            };
            c.Controls.Add(valoareLbl);
            return c;
        }

        // ══════════════════════════════════════════════════════════════
        //  1. Aparate
        // ══════════════════════════════════════════════════════════════

        private Panel ConstruiestePaginaAparate()
        {
            var pag = PaginaGoala();

            var titlu = Titlu("Aparate găsite");
            titlu.Location = new Point(0, 0);
            _lblSubtitluAparate = Subtitlu("Scanare pe porturile seriale...");
            _lblSubtitluAparate.Location = new Point(2, 32);

            _btnScan = new Buton { Text = "Scanează din nou", Fel = FelButon.Secundar, Size = new Size(160, 40) };
            _btnScan.Click += (s, e) => PornesteScanare();
            _btnContinua = new Buton { Text = "Continuă", Fel = FelButon.Principal, Size = new Size(130, 40) };
            _btnContinua.Click += (s, e) => { ActualizeazaSelectia(); Arata("predare"); };

            var antet = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Color.Transparent };
            antet.Controls.Add(titlu);
            antet.Controls.Add(_lblSubtitluAparate);
            antet.Controls.Add(_btnScan);
            antet.Controls.Add(_btnContinua);
            antet.Resize += (s, e) =>
            {
                _btnContinua.Location = new Point(antet.Width - _btnContinua.Width, 8);
                _btnScan.Location = new Point(_btnContinua.Left - _btnScan.Width - 10, 8);
            };

            var cifre = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Color.Transparent };
            Label l1, l2, l3;
            var c1 = CardCifra("Porturi scanate", "—", 0, 100, out l1);
            var c2 = CardCifra("Aparate găsite", "—", 0, 100, out l2);
            var c3 = CardCifra("Selectate pentru predare", "—", 0, 100, out l3);
            _lblNrPorturi = l1; _lblNrAparate = l2; _lblNrZ = l3;
            cifre.Controls.AddRange(new Control[] { c1, c2, c3 });
            cifre.Resize += (s, e) =>
            {
                int latime = (cifre.Width - 28) / 3;
                c1.Bounds = new Rectangle(0, 0, latime, 76);
                c2.Bounds = new Rectangle(latime + 14, 0, latime, 76);
                c3.Bounds = new Rectangle(2 * latime + 28, 0, cifre.Width - 2 * latime - 28, 76);
            };

            var cardLista = new Card { Dock = DockStyle.Fill, Raza = 16 };
            var capLista = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Paleta.Subtil };
            capLista.Paint += (s, e) =>
            {
                using (var p = new Pen(Paleta.MargineFina)) e.Graphics.DrawLine(p, 0, capLista.Height - 1, capLista.Width, capLista.Height - 1);
                DeseneazaColoane(e.Graphics, capLista.Width, 12,
                    new[] { "", "CLIENT", "SERIE / NUI", "MODEL · PORT", "STARE" }, Paleta.TextSters, Paleta.MicTare);
            };
            _listaAparate = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Paleta.Card, Padding = new Padding(0, 40, 0, 8) };
            Paleta.FaraPalpaire(_listaAparate);

            // Peste lista, cat tine cautarea: altfel ecranul pare inghetat un minut.
            _zonaCautare = new Panel { Dock = DockStyle.Fill, BackColor = Paleta.Card, Visible = false };
            Paleta.FaraPalpaire(_zonaCautare);
            var rotita = new Rotita();
            var lblCaut = new Label
            {
                Text = "Caut aparate pe porturile seriale...",
                Font = Paleta.CorpTare,
                ForeColor = Paleta.Text,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _lblPortCautat = new Label
            {
                Text = "",
                Font = Paleta.Mic,
                ForeColor = Paleta.TextSecundar,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _zonaCautare.Controls.AddRange(new Control[] { rotita, lblCaut, _lblPortCautat });
            _zonaCautare.Resize += (s, e) =>
            {
                int mijloc = _zonaCautare.Width / 2;
                int sus = Math.Max(40, _zonaCautare.Height / 2 - 60);
                rotita.Location = new Point(mijloc - rotita.Width / 2, sus);
                lblCaut.Location = new Point(mijloc - lblCaut.Width / 2, sus + 50);
                _lblPortCautat.Location = new Point(mijloc - _lblPortCautat.Width / 2, sus + 76);
            };
            _lblPortCautat.TextChanged += (s, e) =>
                _lblPortCautat.Location = new Point(_zonaCautare.Width / 2 - _lblPortCautat.Width / 2, _lblPortCautat.Top);

            cardLista.Controls.Add(_zonaCautare);
            cardLista.Controls.Add(_listaAparate);
            cardLista.Controls.Add(capLista);

            pag.Controls.Add(cardLista);
            pag.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.Transparent });
            pag.Controls.Add(cifre);
            pag.Controls.Add(antet);
            return pag;
        }

        /// <summary>
        /// Unde incepe fiecare coloana, pentru o latime data. Capul de tabel si randurile
        /// folosesc aceeasi socoteala, altfel nu se potrivesc intre ele.
        /// </summary>
        private static int[] Coloane(int latimeTotala)
        {
            const int marginea = 24, latimeBifa = 40, latimeStare = 100;
            int disponibil = latimeTotala - 2 * marginea - latimeBifa - latimeStare;
            if (disponibil < 320) disponibil = 320;

            int client = disponibil * 46 / 100;
            int serie = disponibil * 28 / 100;

            int x0 = marginea;                          // bifa
            int x1 = x0 + latimeBifa;                   // client
            int x2 = x1 + client;                       // serie / NUI
            int x3 = x2 + serie;                        // model · port
            int x4 = latimeTotala - marginea - latimeStare;  // stare
            return new[] { x0, x1, x2, x3, x4 };
        }

        /// <summary>Capul de tabel, desenat pe aceleasi coloane ca randurile.</summary>
        private static void DeseneazaColoane(Graphics g, int latimeTotala, int sus, string[] texte, Color culoare, Font font)
        {
            var x = Coloane(latimeTotala);
            for (int i = 1; i < texte.Length && i < x.Length; i++)
            {
                int pana = (i + 1 < x.Length ? x[i + 1] : latimeTotala - 24);
                TextRenderer.DrawText(g, texte[i], font, new Rectangle(x[i], sus, pana - x[i] - 8, 18), culoare,
                    TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            }
        }

        private void PornesteScanare()
        {
            if (_ctsScan != null) return;
            _btnScan.Enabled = false;
            _btnContinua.Enabled = false;
            _lblSubtitluAparate.Text = "Caut aparate pe porturile seriale...";
            _listaAparate.Controls.Clear();
            _bifeAparate.Clear();

            _zonaCautare.Visible = true;
            _zonaCautare.BringToFront();
            _lblPortCautat.Text = "";

            _ctsScan = new CancellationTokenSource();
            var ct = _ctsScan.Token;
            var log = new ActionLogger((m, l) =>
            {
                // portul incercat acum, ca sa se vada ca treaba merge inainte
                if (l != LogLevel.Debug || !m.StartsWith("COM")) return;
                try { BeginInvoke((MethodInvoker)delegate { _lblPortCautat.Text = m; }); }
                catch { }
            }, null, null);

            new Thread(() =>
            {
                List<ScannedDevice> gasite = new List<ScannedDevice>();
                try { gasite = _scanner.ScanAllPorts(log, ct); }
                catch { }
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _aparate = gasite ?? new List<ScannedDevice>();
                        _zonaCautare.Visible = false;
                        AratraAparate();
                        _ctsScan = null;
                        _btnScan.Enabled = true;
                        _btnContinua.Enabled = _aparate.Count > 0;
                    });
                }
                catch { }
            })
            { IsBackground = true }.Start();
        }

        private void AratraAparate()
        {
            _listaAparate.Controls.Clear();
            _bifeAparate.Clear();

            int y = 0;
            foreach (var a in _aparate)
            {
                var rand = FaRandAparat(a, y);
                _listaAparate.Controls.Add(rand);
                y += rand.Height;
            }

            _lblNrPorturi.Text = System.IO.Ports.SerialPort.GetPortNames().Length.ToString();
            _lblNrAparate.Text = _aparate.Count.ToString();
            _lblSubtitluAparate.Text = _aparate.Count == 0
                ? "Niciun aparat găsit. Verificați cablul și porniți aparatul."
                : _aparate.Count + " aparate găsite · " + _aparate.Count(x => x.Selected) + " selectate";
            ActualizeazaSelectatele();
        }

        private Control FaRandAparat(ScannedDevice a, int y)
        {
            var rand = new Panel { Bounds = new Rectangle(0, y, _listaAparate.Width - 20, 66), BackColor = Paleta.Card, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            Paleta.FaraPalpaire(rand);
            rand.Paint += (s, e) =>
            {
                using (var p = new Pen(Paleta.MargineFina)) e.Graphics.DrawLine(p, 20, rand.Height - 1, rand.Width - 20, rand.Height - 1);
            };

            var bifa = new CheckBox { Checked = a.Selected, Location = new Point(24, 24), Size = new Size(18, 18) };
            bifa.CheckedChanged += (s, e) => { a.Selected = bifa.Checked; ActualizeazaSelectatele(); };
            _bifeAparate[a.Serie] = bifa;
            rand.Controls.Add(bifa);

            var firma = new Label { Text = a.Firma, Font = Paleta.CorpTare, ForeColor = Paleta.Text, AutoSize = true };
            var cif = new Label { Text = a.CIF, Font = Paleta.Mic, ForeColor = Paleta.TextSecundar, AutoSize = true };
            var serie = new Label { Text = a.Serie, Font = Paleta.CorpTare, ForeColor = Paleta.Text, AutoSize = true };
            var nui = new Label { Text = "NUI " + a.FmNum, Font = Paleta.Mic, ForeColor = Paleta.TextSecundar, AutoSize = true };
            var model = new Label { Text = a.Model, Font = Paleta.Corp, ForeColor = Paleta.Text, AutoSize = true };
            var port = new Label { Text = a.ComPort + " · " + a.Baud, Font = Paleta.Mic, ForeColor = Paleta.TextSecundar, AutoSize = true };
            var stare = new Pastila { Text = "pregătit", Fundal = Paleta.ReusitFundal, Culoare = Paleta.Reusit, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            stare.PotrivesteLatimea();

            rand.Controls.AddRange(new Control[] { firma, cif, serie, nui, model, port, stare });
            rand.Resize += (s, e) =>
            {
                var x = Coloane(rand.Width);
                bifa.Location = new Point(x[0], 24);
                firma.Location = new Point(x[1], 14);
                cif.Location = new Point(x[1] + 2, 35);
                serie.Location = new Point(x[2], 14);
                nui.Location = new Point(x[2] + 2, 35);
                model.Location = new Point(x[3], 14);
                port.Location = new Point(x[3] + 2, 35);
                stare.Location = new Point(x[4], 22);
            };
            rand.PerformLayout();
            if (rand.Width > 0) rand.OnResizePublic();
            return rand;
        }

        private void ActualizeazaSelectatele()
        {
            int n = _aparate.Count(x => x.Selected);
            if (_lblSelectate != null) _lblSelectate.Text = n + (n == 1 ? " aparat" : " aparate");
            if (_lblNrZ != null) _lblNrZ.Text = n.ToString();
            if (_btnContinua != null) _btnContinua.Enabled = n > 0;
            if (_lblSubtitluAparate != null && _aparate.Count > 0)
                _lblSubtitluAparate.Text = _aparate.Count + " aparate găsite · " + n + " selectate";
        }

        private void ActualizeazaSelectia()
        {
            foreach (var a in _aparate)
            {
                CheckBox b;
                if (_bifeAparate.TryGetValue(a.Serie, out b)) a.Selected = b.Checked;
            }
        }

        // restul ecranelor sunt in ShellForm.Pagini.cs
    }

    internal static class ExtensiiControl
    {
        /// <summary>Forteaza asezarea dupa marimea curenta, la construirea randului.</summary>
        public static void OnResizePublic(this Control c)
        {
            var m = typeof(Control).GetMethod("OnResize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            m?.Invoke(c, new object[] { EventArgs.Empty });
        }
    }
}
