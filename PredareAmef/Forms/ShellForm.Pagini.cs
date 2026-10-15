using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using PredareAmef.Services;
using PredareAmef.Ui;

namespace PredareAmef.Forms
{
    public sealed partial class ShellForm
    {
        // ══════════════════════════════════════════════════════════════
        //  2. Predare — ce nu se poate citi din aparat
        // ══════════════════════════════════════════════════════════════

        private Panel ConstruiestePaginaPredare()
        {
            var pag = PaginaGoala();

            var antet = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Color.Transparent };
            var titlu = Titlu("Datele predării");
            titlu.Location = new Point(0, 0);
            var sub = Subtitlu("Se completează o dată și se folosesc pentru toate aparatele selectate");
            sub.Location = new Point(2, 32);
            antet.Controls.Add(titlu);
            antet.Controls.Add(sub);

            var zona = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            // ── cardul din stanga ──
            var stanga = new Card { Raza = 16 };
            int y = 22;

            var lblMotiv = Eticheta("Motivul predării");
            lblMotiv.Location = new Point(24, y); y += 22;
            _cbMotiv = new ComboBox
            {
                Location = new Point(24, y),
                Font = Paleta.Corp,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Color.White,
                ForeColor = Paleta.Text,
            };
            _cbMotiv.Items.AddRange(new object[]
            {
                "Memorie fiscală plină",
                "Înlocuire memorie fiscală defectă",
                "Casare aparat",
                "Schimbare proprietar",
                "Înstrăinare AMEF",
                "La cererea organului fiscal",
            });
            _cbMotiv.SelectedIndex = 0;
            y += 32;
            var explicatieMotiv = new Label
            {
                Text = "Se tipărește în procesul verbal, la „Motivul preluării memoriei fiscale”.",
                Font = Paleta.Mic,
                ForeColor = Paleta.TextSecundar,
                AutoSize = true,
                Location = new Point(24, y),
                BackColor = Color.Transparent
            };
            y += 30;

            var lblUnitate = Eticheta("Unitatea de service");
            lblUnitate.Location = new Point(24, y);
            var lblPredat = Eticheta("Predat de");
            y += 22;
            _cbUnitate = Lista(230, "IT SMART RETAIL", "IT SMART SUPORT", "IT SMART A.M.E.F");
            _cbUnitate.Location = new Point(24, y);
            _txtPredatDe = Camp(PvDialog.ReprezentantPentru("IT SMART RETAIL"), 230);
            _txtPredatDe.Location = new Point(280, y);
            _cbUnitate.SelectedIndexChanged += (s, e) => _txtPredatDe.Text = PvDialog.ReprezentantPentru(_cbUnitate.Text);
            y += 32;
            var explicatiePredat = new Label
            {
                Text = "Se completează singur după firma aleasă.",
                Font = Paleta.Mic,
                ForeColor = Paleta.TextSecundar,
                AutoSize = true,
                Location = new Point(280, y),
                BackColor = Color.Transparent
            };
            y += 30;

            var lblMemorie = Eticheta("S-a montat memorie nouă");
            lblMemorie.Location = new Point(24, y); y += 24;
            _rbMemorieDa = new RadioButton { Text = "DA", Checked = true, Location = new Point(26, y), AutoSize = true, Font = Paleta.Corp, BackColor = Color.Transparent };
            _rbMemorieNu = new RadioButton { Text = "NU", Location = new Point(96, y), AutoSize = true, Font = Paleta.Corp, BackColor = Color.Transparent };
            y += 34;

            var lblObs = Eticheta("Observații");
            lblObs.Location = new Point(24, y); y += 22;
            _txtObservatii = new TextBox
            {
                Location = new Point(24, y),
                Multiline = true,
                Height = 96,
                Font = Paleta.Corp,
                BorderStyle = BorderStyle.FixedSingle,
            };
            y += 108;

            var lblFolder = Eticheta("Folderul în care se salvează");
            lblFolder.Location = new Point(24, y); y += 22;
            _txtFolder = Camp(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), 300);
            _txtFolder.Location = new Point(24, y);
            var btnFolder = new Buton { Text = "Alege...", Fel = FelButon.Secundar, Size = new Size(88, 30), Location = new Point(332, y - 3) };
            btnFolder.Click += (s, e) =>
            {
                using (var d = new FolderBrowserDialog { SelectedPath = _txtFolder.Text })
                    if (d.ShowDialog(this) == DialogResult.OK) _txtFolder.Text = d.SelectedPath;
            };

            stanga.Controls.AddRange(new Control[]
            {
                lblMotiv, _cbMotiv, explicatieMotiv, lblUnitate, lblPredat, _cbUnitate, _txtPredatDe, explicatiePredat,
                lblMemorie, _rbMemorieDa, _rbMemorieNu, lblObs, _txtObservatii, lblFolder, _txtFolder, btnFolder
            });
            stanga.Resize += (s, e) =>
            {
                int latime = stanga.Width - 48;
                _cbMotiv.Width = latime;
                _txtObservatii.Width = latime;
                int jumatate = (latime - 26) / 2;
                _cbUnitate.Width = jumatate;
                _txtPredatDe.Width = jumatate;
                _txtPredatDe.Location = new Point(24 + jumatate + 26, _txtPredatDe.Top);
                explicatiePredat.Location = new Point(24 + jumatate + 26, explicatiePredat.Top);
                lblPredat.Location = new Point(24 + jumatate + 26, lblUnitate.Top);
                _txtFolder.Width = Math.Max(160, latime - 100);
                btnFolder.Location = new Point(24 + _txtFolder.Width + 12, btnFolder.Top);
            };

            // ── coloana din dreapta ──
            var cardPv = new Card { Raza = 16, Height = 96 };
            _chkPv = new CheckBox { Checked = true, Location = new Point(0, 0), Size = new Size(18, 18), BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            cardPv.Controls.Add(new Label { Text = "Proces verbal din CRM", Font = Paleta.TitluCard, ForeColor = Paleta.Text, AutoSize = true, Location = new Point(22, 18), BackColor = Color.Transparent });
            cardPv.Controls.Add(new Label
            {
                Text = "Rapoartele Z, totalul vânzărilor și perioada\nse iau din memoria aparatului.",
                Font = Paleta.Mic,
                ForeColor = Paleta.TextSecundar,
                AutoSize = true,
                Location = new Point(22, 42),
                BackColor = Color.Transparent
            });
            cardPv.Controls.Add(_chkPv);
            cardPv.Resize += (s, e) => _chkPv.Location = new Point(cardPv.Width - 42, 20);

            var cardEtichete = new Card { Raza = 16, Height = 150 };
            _chkEtichete = new CheckBox { Checked = true, Size = new Size(18, 18), BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            cardEtichete.Controls.Add(new Label { Text = "Tipărire etichete", Font = Paleta.TitluCard, ForeColor = Paleta.Text, AutoSize = true, Location = new Point(22, 18), BackColor = Color.Transparent });
            cardEtichete.Controls.Add(new Label
            {
                Text = "Memorie 62×28 mm și plic 80×35 mm,\npentru fiecare aparat.",
                Font = Paleta.Mic,
                ForeColor = Paleta.TextSecundar,
                AutoSize = true,
                Location = new Point(22, 42),
                BackColor = Color.Transparent
            });
            var lblImprimanta = Eticheta("Imprimanta");
            lblImprimanta.Location = new Point(22, 86);
            _cbImprimanta = Lista(240, EtichetaPrinter.Imprimante().ToArray());
            _cbImprimanta.Location = new Point(22, 108);
            string implicita = EtichetaPrinter.ImprimantaImplicita();
            if (implicita != null && _cbImprimanta.Items.Contains(implicita)) _cbImprimanta.SelectedItem = implicita;
            cardEtichete.Controls.AddRange(new Control[] { _chkEtichete, lblImprimanta, _cbImprimanta });
            cardEtichete.Resize += (s, e) =>
            {
                _chkEtichete.Location = new Point(cardEtichete.Width - 42, 20);
                _cbImprimanta.Width = cardEtichete.Width - 44;
            };

            var cardStart = new Card { Raza = 16, Height = 200, Fundal = Paleta.CardInchis, Contur = Color.Transparent };
            cardStart.Controls.Add(new Label { Text = "La pornire se execută", Font = Paleta.Mic, ForeColor = Paleta.TextPeInchis, AutoSize = true, Location = new Point(22, 20), BackColor = Color.Transparent });
            cardStart.Controls.Add(new Label
            {
                Text = "Citire memorie · raport sumar · export ANAF · antet,\napoi proces verbal și etichete",
                Font = Paleta.Corp,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(22, 44),
                BackColor = Color.Transparent
            });
            _lblSelectate = new Label { Text = "0 aparate", Font = new Font("Segoe UI Semibold", 12F), ForeColor = Color.White, AutoSize = true, Location = new Point(22, 96), BackColor = Color.Transparent };
            cardStart.Controls.Add(_lblSelectate);
            var btnStart = new Buton { Text = "Pornește predarea", Fel = FelButon.PeInchis, Height = 46, Raza = 11 };
            btnStart.Click += (s, e) => PornestePredarea();
            cardStart.Controls.Add(btnStart);
            cardStart.Resize += (s, e) =>
            {
                btnStart.Bounds = new Rectangle(22, cardStart.Height - 68, cardStart.Width - 44, 46);
            };

            zona.Controls.AddRange(new Control[] { stanga, cardPv, cardEtichete, cardStart });
            zona.Resize += (s, e) =>
            {
                int latimeDreapta = Math.Max(300, (int)(zona.Width * 0.36));
                int latimeStanga = zona.Width - latimeDreapta - 18;
                stanga.Bounds = new Rectangle(0, 0, latimeStanga, Math.Min(zona.Height - 4, 466));
                int x = latimeStanga + 18;
                cardPv.Bounds = new Rectangle(x, 0, latimeDreapta, 96);
                cardEtichete.Bounds = new Rectangle(x, 112, latimeDreapta, 150);
                cardStart.Bounds = new Rectangle(x, 278, latimeDreapta, 200);
            };

            pag.Controls.Add(zona);
            pag.Controls.Add(antet);
            return pag;
        }

        // ══════════════════════════════════════════════════════════════
        //  3. In lucru
        // ══════════════════════════════════════════════════════════════

        private Panel ConstruiestePaginaProgres()
        {
            var pag = PaginaGoala();

            var antet = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Color.Transparent };
            var titlu = Titlu("Predare în lucru");
            titlu.Location = new Point(0, 0);
            _lblStareRulare = Subtitlu("Nicio predare pornită");
            _lblStareRulare.Location = new Point(2, 32);
            _lblProcent = new Label { Text = "0%", Font = new Font("Segoe UI Semibold", 22F), ForeColor = Paleta.Text, AutoSize = true, BackColor = Color.Transparent };
            _btnOpreste = new Buton { Text = "Oprește", Fel = FelButon.Periculos, Size = new Size(110, 40), Enabled = false };
            _btnOpreste.Click += (s, e) => { if (_ctsRulare != null) _ctsRulare.Cancel(); };
            antet.Controls.AddRange(new Control[] { titlu, _lblStareRulare, _lblProcent, _btnOpreste });
            antet.Resize += (s, e) =>
            {
                _lblProcent.Location = new Point(antet.Width - _lblProcent.Width - 130, 6);
                _btnOpreste.Location = new Point(antet.Width - _btnOpreste.Width, 8);
            };

            _progresGeneral = new BaraProgres { Dock = DockStyle.Top, Height = 8 };

            _listaProgres = new Panel { Dock = DockStyle.Top, Height = 250, AutoScroll = true, BackColor = Color.Transparent };
            Paleta.FaraPalpaire(_listaProgres);

            var cardJurnal = new Card { Dock = DockStyle.Fill, Raza = 16, Fundal = Paleta.CardInchis, Contur = Color.Transparent };
            var etJurnal = new Label
            {
                Text = "JURNAL",
                Font = Paleta.MicTare,
                ForeColor = Color.FromArgb(0x7F, 0x93, 0xA6),
                BackColor = Paleta.CardInchis,
                AutoSize = true,
                Location = new Point(20, 15),
            };
            _jurnal = new RichTextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Paleta.CardInchis,
                ForeColor = Paleta.TextPeInchis,
                Font = Paleta.Monospatiat,
                ReadOnly = true,
            };
            cardJurnal.Controls.Add(_jurnal);
            cardJurnal.Controls.Add(etJurnal);
            // lasam o rama in jurul casetei, altfel textul ar acoperi coltul rotunjit al cardului
            cardJurnal.Resize += (s, e) =>
                _jurnal.Bounds = new Rectangle(18, 40, Math.Max(40, cardJurnal.Width - 36), Math.Max(30, cardJurnal.Height - 58));

            pag.Controls.Add(cardJurnal);
            pag.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.Transparent });
            pag.Controls.Add(_listaProgres);
            pag.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.Transparent });
            pag.Controls.Add(_progresGeneral);
            pag.Controls.Add(antet);
            return pag;
        }

        // ══════════════════════════════════════════════════════════════
        //  4. Rezultate
        // ══════════════════════════════════════════════════════════════

        private Panel ConstruiestePaginaRezultate()
        {
            var pag = PaginaGoala();

            var antet = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Color.Transparent };
            var titlu = Titlu("Rezultate");
            titlu.Location = new Point(0, 0);
            _lblSubtitluRezultate = Subtitlu("Nicio predare terminată în această sesiune");
            _lblSubtitluRezultate.Location = new Point(2, 32);
            var btnFolder = new Buton { Text = "Deschide folderul", Fel = FelButon.Secundar, Size = new Size(170, 40) };
            btnFolder.Click += (s, e) =>
            {
                string cale = _runner != null ? _runner.SessionRootDir : _txtFolder.Text;
                if (!string.IsNullOrWhiteSpace(cale) && Directory.Exists(cale))
                    System.Diagnostics.Process.Start("explorer.exe", "\"" + cale + "\"");
            };
            antet.Controls.AddRange(new Control[] { titlu, _lblSubtitluRezultate, btnFolder });
            antet.Resize += (s, e) => btnFolder.Location = new Point(antet.Width - btnFolder.Width, 8);

            _listaRezultate = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };
            Paleta.FaraPalpaire(_listaRezultate);

            pag.Controls.Add(_listaRezultate);
            pag.Controls.Add(antet);
            return pag;
        }

        // ══════════════════════════════════════════════════════════════
        //  Rularea predarii
        // ══════════════════════════════════════════════════════════════

        private void PornestePredarea()
        {
            ActualizeazaSelectia();
            var alese = _aparate.Where(a => a.Selected).ToList();
            if (alese.Count == 0)
            {
                MessageBox.Show(this, "Selectați cel puțin un aparat.", "Predare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Arata("aparate");
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtFolder.Text) || !Directory.Exists(_txtFolder.Text))
            {
                MessageBox.Show(this, "Folderul în care se salvează nu există.", "Predare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_chkPv.Checked && string.IsNullOrWhiteSpace(_cbMotiv.Text))
            {
                MessageBox.Show(this, "Completați motivul predării — el se tipărește în document.", "Predare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var optiuni = new HandoverOptions
            {
                OutputRoot = _txtFolder.Text.Trim(),
                Step1_DumpFm = true,
                Step3_GenerateTxt = true,
                Step5_PrintSummary = true,
                Step6_ExportXml = true,
                Step7_ExportHeader = true,
                GenereazaPv = _chkPv.Checked,
                PvMotiv = _cbMotiv.Text.Trim(),
                PvPredatDe = _txtPredatDe.Text.Trim(),
                PvMemorieNoua = _rbMemorieDa.Checked ? "DA" : "NU",
                PvUnitateService = _cbUnitate.Text.Trim(),
                PvObservatii = _txtObservatii.Text.Trim(),
                TiparesteEtichete = _chkEtichete.Checked,
                ImprimantaEtichete = _cbImprimanta.Text.Trim(),
            };

            _runner = new MultiSessionRunner();
            var sesiuni = _runner.InitSessions(alese, optiuni);

            PregatesteProgresul(sesiuni);
            Arata("progres");
            _btnOpreste.Enabled = true;
            _ctsRulare = new CancellationTokenSource();
            var ct = _ctsRulare.Token;

            new Thread(() =>
            {
                try
                {
                    _runner.RunSessions(optiuni, 2, "", s =>
                    {
                        try { BeginInvoke((MethodInvoker)delegate { ActualizeazaProgresul(s); }); }
                        catch { }
                    }, ct, (s, mesaj, nivel) =>
                    {
                        try { BeginInvoke((MethodInvoker)delegate { ScrieInJurnal(s, mesaj, nivel); }); }
                        catch { }
                    });
                }
                catch (Exception ex)
                {
                    try { BeginInvoke((MethodInvoker)delegate { ScrieInJurnal(null, "Eroare: " + ex.Message, LogLevel.Error); }); }
                    catch { }
                }
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _btnOpreste.Enabled = false;
                        _ctsRulare = null;
                        AratraRezultatele();
                        Arata("rezultate");
                    });
                }
                catch { }
            })
            { IsBackground = true }.Start();
        }

        private void PregatesteProgresul(List<SessionStatus> sesiuni)
        {
            _listaProgres.Controls.Clear();
            _stariAparate.Clear();
            _bareAparate.Clear();
            _jurnal.Clear();
            _progresGeneral.Valoare = 0;
            _lblProcent.Text = "0%";
            _lblStareRulare.Text = sesiuni.Count + " aparate · pornit la " + DateTime.Now.ToString("HH:mm");

            int y = 0;
            foreach (var s in sesiuni)
            {
                var card = new Card { Bounds = new Rectangle(0, y, _listaProgres.Width - 24, 104), Raza = 16, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

                card.Controls.Add(new Label
                {
                    Text = s.Device.Firma + " · " + s.Device.Serie,
                    Font = Paleta.CorpTare,
                    ForeColor = Paleta.Text,
                    AutoSize = true,
                    Location = new Point(22, 18),
                    BackColor = Color.Transparent
                });
                card.Controls.Add(new Label
                {
                    Text = s.Device.ComPort + " · " + s.Device.Model,
                    Font = Paleta.Mic,
                    ForeColor = Paleta.TextSecundar,
                    AutoSize = true,
                    Location = new Point(24, 40),
                    BackColor = Color.Transparent
                });

                var stare = new Label
                {
                    Text = "așteaptă",
                    Font = Paleta.Mic,
                    ForeColor = Paleta.TextSecundar,
                    AutoSize = true,
                    Location = new Point(24, 76),
                    BackColor = Color.Transparent
                };
                var bara = new BaraProgres { Bounds = new Rectangle(22, 62, card.Width - 44, 6), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

                card.Controls.Add(bara);
                card.Controls.Add(stare);
                _listaProgres.Controls.Add(card);

                _stariAparate[s.Device.Serie] = stare;
                _bareAparate[s.Device.Serie] = bara;
                y += 114;
            }
        }

        private void ActualizeazaProgresul(SessionStatus s)
        {
            Label stare;
            BaraProgres bara;
            if (_stariAparate.TryGetValue(s.Device.Serie, out stare))
            {
                stare.Text = s.Done
                    ? (s.Success ? "gata" : "eșuat: " + (s.Error ?? ""))
                    : (s.SubLabel ?? s.Status);
                stare.ForeColor = s.Done ? (s.Success ? Paleta.Reusit : Paleta.Eroare) : Paleta.TextSecundar;
            }
            if (_bareAparate.TryGetValue(s.Device.Serie, out bara))
            {
                bara.Valoare = s.Progress / 100.0;
                bara.Umplere = s.Done && !s.Success ? Paleta.Eroare : (s.Done ? Paleta.Reusit : Paleta.Accent);
            }

            double total = 0;
            int cate = 0;
            foreach (var st in _runner.Statuses) { total += st.Progress; cate++; }
            double medie = cate > 0 ? total / cate : 0;
            _progresGeneral.Valoare = medie / 100.0;
            _lblProcent.Text = ((int)medie) + "%";

            int gata = _runner.Statuses.Count(x => x.Done);
            _lblStareRulare.Text = _runner.Statuses.Count + " aparate · " + gata + " gata";

        }

        /// <summary>
        /// O linie de jurnal, ca in aplicatia veche: ora, aparatul (cand sunt mai multe)
        /// si mesajul, colorat dupa nivel. Sub-progresul nu ajunge aici, ca sa nu se umple
        /// jurnalul cu sute de linii de KB pe secunda.
        /// </summary>
        private void ScrieInJurnal(SessionStatus s, string mesaj, LogLevel nivel)
        {
            Color culoare;
            switch (nivel)
            {
                case LogLevel.Success: culoare = Color.FromArgb(0x6F, 0xD8, 0xA8); break;
                case LogLevel.Warning: culoare = Color.FromArgb(0xF0, 0xB4, 0x5E); break;
                case LogLevel.Error: culoare = Color.FromArgb(0xF2, 0x8B, 0x82); break;
                case LogLevel.Debug: culoare = Color.FromArgb(0x7F, 0x93, 0xA6); break;
                default: culoare = Paleta.TextPeInchis; break;
            }

            string cap = "[" + DateTime.Now.ToString("HH:mm:ss") + "] ";
            if (s != null && _runner != null && _runner.Statuses.Count > 1)
                cap += s.Device.Serie + "  ";

            _jurnal.SelectionStart = _jurnal.TextLength;
            _jurnal.SelectionColor = culoare;
            _jurnal.AppendText(cap + mesaj + Environment.NewLine);
            _jurnal.SelectionStart = _jurnal.TextLength;
            _jurnal.ScrollToCaret();
        }

        private void AratraRezultatele()
        {
            _listaRezultate.Controls.Clear();
            if (_runner == null) return;

            int reusite = _runner.Statuses.Count(x => x.Success);
            _lblSubtitluRezultate.Text = _runner.Statuses.Count + " aparate · " + reusite + " reușite · " +
                                         DateTime.Now.ToString("dd.MM.yyyy, HH:mm");

            int y = 0;
            foreach (var s in _runner.Statuses)
            {
                var card = new Card { Bounds = new Rectangle(0, y, _listaRezultate.Width - 24, 150), Raza = 16, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

                card.Controls.Add(new Label
                {
                    Text = s.Device.Firma,
                    Font = Paleta.TitluCard,
                    ForeColor = Paleta.Text,
                    AutoSize = true,
                    Location = new Point(24, 18),
                    BackColor = Color.Transparent
                });
                card.Controls.Add(new Label
                {
                    Text = s.Device.Serie + " · NUI " + s.Device.FmNum + " · " + s.Device.Model,
                    Font = Paleta.Mic,
                    ForeColor = Paleta.TextSecundar,
                    AutoSize = true,
                    Location = new Point(26, 42),
                    BackColor = Color.Transparent
                });

                var pastila = new Pastila
                {
                    Text = s.Success ? "terminat" : "eșuat",
                    Fundal = s.Success ? Paleta.ReusitFundal : Paleta.EroareFundal,
                    Culoare = s.Success ? Paleta.Reusit : Paleta.Eroare,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                };
                pastila.PotrivesteLatimea();
                pastila.Location = new Point(card.Width - pastila.Width - 24, 20);
                card.Controls.Add(pastila);

                var detalii = new Label
                {
                    Text = s.Success
                        ? "Fișierele predării, procesul verbal și etichetele sunt în folderul sesiunii."
                        : ("Nu s-a terminat: " + (s.Error ?? "motiv necunoscut")),
                    Font = Paleta.Corp,
                    ForeColor = s.Success ? Paleta.TextSecundar : Paleta.Eroare,
                    AutoSize = true,
                    Location = new Point(24, 72),
                    BackColor = Color.Transparent
                };
                card.Controls.Add(detalii);

                var btnDeschide = new Buton { Text = "Deschide folderul", Fel = FelButon.Secundar, Size = new Size(160, 34), Location = new Point(24, 100) };
                string dir = s.OutputDir;
                btnDeschide.Click += (snd, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                        System.Diagnostics.Process.Start("explorer.exe", "\"" + dir + "\"");
                };
                card.Controls.Add(btnDeschide);

                var btnPv = new Buton { Text = "Proces verbal", Fel = FelButon.Secundar, Size = new Size(140, 34), Location = new Point(194, 100) };
                btnPv.Click += (snd, e) =>
                {
                    try
                    {
                        var pv = Directory.GetFiles(dir ?? "", "Proces_verbal_*.docx").FirstOrDefault();
                        if (pv != null) System.Diagnostics.Process.Start(pv);
                        else MessageBox.Show(this, "Nu există proces verbal pentru acest aparat.", "Rezultate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show(this, ex.Message, "Rezultate", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                };
                card.Controls.Add(btnPv);

                _listaRezultate.Controls.Add(card);
                y += 162;
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  Export XML la cerere (operatiune separata de predare)
        // ══════════════════════════════════════════════════════════════

        private Panel ConstruiestePaginaExportXml()
        {
            var pag = PaginaGoala();

            var antet = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Color.Transparent };
            var titlu = Titlu("Export XML la cerere"); titlu.Location = new Point(0, 0);
            var sub = Subtitlu("Export ANAF .p7b pentru un aparat, pe perioada aleasa — fara predare"); sub.Location = new Point(2, 32);
            antet.Controls.Add(titlu); antet.Controls.Add(sub);

            var zona = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            var card = new Card { Raza = 16 };
            int y = 22;

            var lblAp = Eticheta("Aparat"); lblAp.Location = new Point(24, y); y += 22;
            _cbExpAparat = new ComboBox { Location = new Point(24, y), Font = Paleta.Corp, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.White, ForeColor = Paleta.Text, Width = 360 };
            _btnExpScan = new Buton { Text = "Scanează", Fel = FelButon.Secundar, Size = new Size(110, 30), Location = new Point(392, y - 2) };
            _btnExpScan.Click += (s, e) => ScaneazaPentruExport();
            y += 42;

            var lblMod = Eticheta("Perioada"); lblMod.Location = new Point(24, y); y += 22;
            _rbExpData = new RadioButton { Text = "După dată", Checked = true, Location = new Point(26, y), AutoSize = true, Font = Paleta.Corp, BackColor = Color.Transparent };
            _rbExpZ = new RadioButton { Text = "După nr. Z", Location = new Point(150, y), AutoSize = true, Font = Paleta.Corp, BackColor = Color.Transparent };
            y += 30;

            var lblDe = Eticheta("De la"); lblDe.Location = new Point(26, y);
            var lblPana = Eticheta("Până la"); lblPana.Location = new Point(186, y);
            y += 20;
            _dtpExpDe = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(26, y), Width = 140, Font = Paleta.Corp, Value = new DateTime(DateTime.Now.Year, 1, 1) };
            _dtpExpPana = new DateTimePicker { Format = DateTimePickerFormat.Short, Location = new Point(186, y), Width = 140, Font = Paleta.Corp, Value = DateTime.Now };
            _numExpZde = new NumericUpDown { Location = new Point(26, y), Width = 140, Font = Paleta.Corp, Minimum = 1, Maximum = 99999, Value = 1, Visible = false };
            _numExpZpana = new NumericUpDown { Location = new Point(186, y), Width = 140, Font = Paleta.Corp, Minimum = 1, Maximum = 99999, Value = 1, Visible = false };
            y += 40;

            MethodInvoker aplicaMod = () =>
            {
                bool dupaData = _rbExpData.Checked;
                _dtpExpDe.Visible = dupaData; _dtpExpPana.Visible = dupaData;
                _numExpZde.Visible = !dupaData; _numExpZpana.Visible = !dupaData;
            };
            _rbExpData.CheckedChanged += (s, e) => aplicaMod();
            _rbExpZ.CheckedChanged += (s, e) => aplicaMod();

            var lblFolder = Eticheta("Folderul în care se salvează"); lblFolder.Location = new Point(24, y); y += 22;
            _txtExpFolder = Camp(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), 300);
            _txtExpFolder.Location = new Point(24, y);
            var btnFolder = new Buton { Text = "Alege...", Fel = FelButon.Secundar, Size = new Size(88, 30), Location = new Point(332, y - 3) };
            btnFolder.Click += (s, e) =>
            {
                using (var d = new FolderBrowserDialog { SelectedPath = _txtExpFolder.Text })
                    if (d.ShowDialog(this) == DialogResult.OK) _txtExpFolder.Text = d.SelectedPath;
            };
            y += 44;

            // Unitate service + notificare SPV (intra in PV-ul de interventie)
            var lblUnit = Eticheta("Unitate service"); lblUnit.Location = new Point(24, y);
            var lblNotif = Eticheta("Notificare SPV (nr./dată) — opțional"); lblNotif.Location = new Point(236, y);
            y += 20;
            _cbExpUnitate = Lista(200, "IT SMART RETAIL", "IT SMART SUPORT", "IT SMART A.M.E.F");
            _cbExpUnitate.Location = new Point(24, y);
            _cbExpUnitate.SelectedIndex = 0;
            _txtExpNotificare = Camp("", 280); _txtExpNotificare.Location = new Point(236, y);
            y += 38;

            _chkExpPv = new CheckBox { Text = "Generează PV de intervenție", Checked = true, Location = new Point(26, y), AutoSize = true, Font = Paleta.Corp, BackColor = Color.Transparent };
            _chkExpCrm = new CheckBox { Text = "Înregistrează în CRM (dosar + RMA)", Checked = true, Location = new Point(260, y), AutoSize = true, Font = Paleta.Corp, BackColor = Color.Transparent };
            y += 26;
            _chkExpEmail = new CheckBox { Text = "Trimite arhiva pe email la:", Location = new Point(26, y), AutoSize = true, Font = Paleta.Corp, BackColor = Color.Transparent };
            _txtExpEmail = Camp("", 230); _txtExpEmail.Location = new Point(236, y - 2); _txtExpEmail.Enabled = false;
            _chkExpEmail.CheckedChanged += (s, e) => _txtExpEmail.Enabled = _chkExpEmail.Checked;
            y += 40;

            _btnExpExporta = new Buton { Text = "Exportă XML", Fel = FelButon.Principal, Size = new Size(160, 42), Location = new Point(24, y) };
            _btnExpExporta.Click += (s, e) => RuleazaExportXml();
            _lblExpStare = new Label { Text = "", Font = Paleta.Corp, ForeColor = Paleta.TextSecundar, AutoSize = true, Location = new Point(200, y + 12), BackColor = Color.Transparent };

            card.Controls.AddRange(new Control[]
            {
                lblAp, _cbExpAparat, _btnExpScan, lblMod, _rbExpData, _rbExpZ,
                lblDe, lblPana, _dtpExpDe, _dtpExpPana, _numExpZde, _numExpZpana,
                lblFolder, _txtExpFolder, btnFolder,
                lblUnit, lblNotif, _cbExpUnitate, _txtExpNotificare,
                _chkExpPv, _chkExpCrm, _chkExpEmail, _txtExpEmail,
                _btnExpExporta, _lblExpStare
            });

            var cardLog = new Card { Raza = 16, Fundal = Paleta.CardInchis, Contur = Color.Transparent, Padding = new Padding(16) };
            _txtExpLog = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None, BackColor = Paleta.CardInchis, ForeColor = Color.White, Font = new Font("Consolas", 8.5f), Dock = DockStyle.Fill };
            cardLog.Controls.Add(_txtExpLog);

            zona.Controls.AddRange(new Control[] { card, cardLog });
            zona.Resize += (s, e) =>
            {
                const int latimeCard = 540;  // cardul de formular, latime fixa in stanga
                int h = Math.Max(120, zona.Height - 4);
                card.Bounds = new Rectangle(0, 0, latimeCard, Math.Min(h, 470));
                // Fereastra neagra (log) in dreapta, cat mai lata — ca sa se vada mult info.
                int xLog = latimeCard + 18;
                cardLog.Bounds = new Rectangle(xLog, 0, Math.Max(260, zona.Width - xLog), h);
            };

            pag.Controls.Add(zona);
            pag.Controls.Add(antet);
            return pag;
        }

        private void ReincarcaAparateExport()
        {
            if (_cbExpAparat == null) return;
            int sel = _cbExpAparat.SelectedIndex;
            _cbExpAparat.Items.Clear();
            foreach (var a in _aparate)
                _cbExpAparat.Items.Add(Branding.CurataNume(a.Firma) + " · " + a.Serie + " · " + Branding.CurataNume(a.Model) + "  (" + a.ComPort + ")");
            if (_cbExpAparat.Items.Count > 0)
                _cbExpAparat.SelectedIndex = (sel >= 0 && sel < _cbExpAparat.Items.Count) ? sel : 0;
            if (_lblExpStare != null && _aparate.Count == 0)
                _lblExpStare.Text = "Niciun aparat — apasă Scanează.";
        }

        private void ScaneazaPentruExport()
        {
            _btnExpScan.Enabled = false;
            _lblExpStare.Text = "Caut aparate pe porturile seriale...";
            var log = new ActionLogger((m, l) => { });
            new Thread(() =>
            {
                List<ScannedDevice> gasite = new List<ScannedDevice>();
                try { gasite = _scanner.ScanAllPorts(log, CancellationToken.None); }
                catch { }
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _aparate = gasite ?? new List<ScannedDevice>();
                        ReincarcaAparateExport();
                        _btnExpScan.Enabled = true;
                        _lblExpStare.Text = _aparate.Count + " aparate găsite";
                    });
                }
                catch { }
            })
            { IsBackground = true }.Start();
        }

        private void RuleazaExportXml()
        {
            int idx = _cbExpAparat.SelectedIndex;
            if (idx < 0 || idx >= _aparate.Count)
            {
                MessageBox.Show(this, "Selectați un aparat (scanați întâi).", "Export XML", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var dev = _aparate[idx];
            string root = (_txtExpFolder.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                MessageBox.Show(this, "Folderul în care se salvează nu există.", "Export XML", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool dupaData = _rbExpData.Checked;
            DateTime dFrom = _dtpExpDe.Value.Date, dTo = _dtpExpPana.Value.Date;
            int zFrom = (int)_numExpZde.Value, zTo = (int)_numExpZpana.Value;

            // Valori din UI, citite acum (thread-ul de fundal nu atinge controalele).
            string unitate = _cbExpUnitate.Text;
            string reprezentant = PvDialog.ReprezentantPentru(unitate);
            string notificare = (_txtExpNotificare.Text ?? "").Trim();
            bool faPv = _chkExpPv.Checked;
            bool faCrm = _chkExpCrm.Checked;
            bool faEmail = _chkExpEmail.Checked;
            string emailTo = (_txtExpEmail.Text ?? "").Trim();

            string firmaCurat = Branding.CurataNume(dev.Firma);
            string modelCurat = Branding.CurataNume(dev.Model);
            string nui = (dev.FmNum ?? "").Trim();
            if (nui == "" || nui == "-") nui = "NUI";
            string folder = Path.Combine(root, SafeNameExp("Export XML_" + firmaCurat + "_" + dev.Serie + "_" + nui));

            string perioadaTxt = dupaData
                ? dFrom.ToString("dd.MM.yyyy") + " - " + dTo.ToString("dd.MM.yyyy")
                : "Z" + zFrom + " - Z" + zTo;

            _btnExpExporta.Enabled = false;
            _txtExpLog.Clear();
            _lblExpStare.Text = "Export în curs...";

            new Thread(() =>
            {
                int files = 0; string err = null;
                var log = new ActionLogger((m, l) => { try { BeginInvoke((MethodInvoker)delegate { _txtExpLog.AppendText(m + "\r\n"); }); } catch { } });
                DudeClient dude = null;
                try
                {
                    Directory.CreateDirectory(folder);
                    string eOpen;
                    dude = DudeClient.OpenSerial(dev.ComPort, dev.Baud, out eOpen, log);
                    if (dude == null) err = "Conexiune eșuată: " + (eOpen ?? "necunoscut");
                    else
                    {
                        var xml = new XmlExporter();
                        if (dupaData) files = xml.ExportDateRange(dude, folder, dFrom, dTo, cif: SafeNameExp(dev.CIF), log: log);
                        else files = xml.ExportZRange(dude, folder, zFrom, zTo, cif: SafeNameExp(dev.CIF), log: log);
                    }
                }
                catch (Exception ex) { err = ex.Message; }
                finally { try { if (dude != null) dude.Dispose(); } catch { } }

                // PV-ul de interventie se genereaza pe fundal (scriere fisier, rapid).
                string pvPath = null;
                string nrRma = null;
                var zips = new List<string>();
                if (err == null && files > 0)
                {
                    var linii = new List<PvLinieExport>();
                    int total = ColecteazaExport(folder, dupaData, zFrom, zTo, linii, zips);
                    if (faPv)
                    {
                        try
                        {
                            var d = new PvInterventieData
                            {
                                Firma = firmaCurat, Cif = dev.CIF, Model = modelCurat, Serie = dev.Serie, Nui = nui,
                                UnitateService = unitate, Reprezentant = reprezentant,
                                DupaData = dupaData, DataDe = dFrom, DataPana = dTo, ZDe = zFrom, ZPana = zTo,
                                NotificareSpv = notificare, Linii = linii, TotalFisiere = total
                            };
                            try { pvPath = PvInterventie.GenereazaPdf(d, folder); }
                            catch (Exception exPdf)
                            {
                                log.Log("PV PDF a esuat (" + exPdf.Message + "), generez RTF.", LogLevel.Warning);
                                pvPath = PvInterventie.GenereazaRtf(d, folder);
                            }
                            log.Log("PV de interventie generat: " + Path.GetFileName(pvPath), LogLevel.Success);
                        }
                        catch (Exception ex) { log.Log("PV de interventie: " + ex.Message, LogLevel.Warning); }
                    }

                    // Inregistrare in CRM: creeaza RMA + pune PV-ul si arhiva in dosarul clientului.
                    if (faCrm)
                    {
                        try
                        {
                            var atas = new List<string>(zips);
                            if (pvPath != null) atas.Add(pvPath);
                            var sbRez = new StringBuilder();
                            foreach (var l in linii) sbRez.Append(l.Eticheta).Append(" (").Append(l.Fisiere).Append(" Z); ");
                            var crm = new CrmPvService();
                            nrRma = crm.InregistreazaInterventie(dev.Serie, unitate, perioadaTxt, notificare, total, sbRez.ToString(), atas);
                            if (nrRma != null) log.Log("CRM: RMA " + nrRma + " creat; PV + arhiva in dosarul clientului.", LogLevel.Success);
                            else log.Log("CRM: nu s-a inregistrat (" + (crm.UltimaEroare ?? "necunoscut") + ")", LogLevel.Warning);
                        }
                        catch (Exception ex) { log.Log("CRM: " + ex.Message, LogLevel.Warning); }
                    }
                }

                int filesC = files; string errC = err; string pvC = pvPath; string rmaC = nrRma;
                var zipsC = zips;
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _btnExpExporta.Enabled = true;
                        if (errC != null) { _lblExpStare.Text = "Eroare: " + errC; return; }

                        _lblExpStare.Text = filesC + " fișiere exportate";
                        if (filesC <= 0)
                        {
                            MessageBox.Show(this, "Nu s-au găsit rapoarte Z în perioada aleasă.", "Export XML", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        // Email (actiune catre exterior) — doar cu confirmarea utilizatorului.
                        if (faEmail && !string.IsNullOrWhiteSpace(emailTo))
                        {
                            var atasamente = new List<string>(zipsC);
                            if (pvC != null) atasamente.Add(pvC);
                            if (MessageBox.Show(this,
                                    "Trimit pe email către:\n" + emailTo + "\n\n" +
                                    atasamente.Count + " atașamente (arhive" + (pvC != null ? " + PV" : "") + ").\n\nTrimit acum?",
                                    "Trimite pe email", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                _lblExpStare.Text = "Trimit email...";
                                var logUi = new ActionLogger((m, l) => { try { _txtExpLog.AppendText(m + "\r\n"); } catch { } });
                                new Thread(() =>
                                {
                                    bool ok = new EmailService().TrimiteArhiva(emailTo, firmaCurat, dev.Serie, perioadaTxt, atasamente, logUi);
                                    try { BeginInvoke((MethodInvoker)delegate { _lblExpStare.Text = ok ? "Email trimis." : "Email: eșuat (vezi log)."; }); } catch { }
                                })
                                { IsBackground = true }.Start();
                            }
                        }

                        string mesaj = filesC + " fișiere exportate în:\n" + folder;
                        if (pvC != null) mesaj += "\n\nPV: " + Path.GetFileName(pvC);
                        if (rmaC != null) mesaj += "\nCRM: " + rmaC + " (dosarul clientului)";
                        if (MessageBox.Show(this, mesaj + "\n\nDeschizi folderul?", "Export XML", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                            try { System.Diagnostics.Process.Start("explorer.exe", "\"" + folder + "\""); } catch { }
                    });
                }
                catch { }
            })
            { IsBackground = true }.Start();
        }

        /// <summary>
        /// Dupa export, aduna lunile (sau folderul plat la export pe Z) cu rapoarte Z si
        /// arhivele ZIP produse. Returneaza numarul total de rapoarte Z.
        /// </summary>
        private static int ColecteazaExport(string folder, bool dupaData, int zFrom, int zTo,
            List<PvLinieExport> linii, List<string> zips)
        {
            int total = 0;
            var subdirs = Directory.Exists(folder) ? Directory.GetDirectories(folder) : new string[0];
            if (subdirs.Length > 0)
            {
                Array.Sort(subdirs);
                foreach (var sub in subdirs)
                {
                    int z = NumaraZ(sub);
                    if (z <= 0) continue;
                    total += z;
                    string zip = PrimulZip(sub);
                    if (zip != null) zips.Add(zip);
                    linii.Add(new PvLinieExport { Eticheta = Path.GetFileName(sub), Fisiere = z, Arhiva = zip != null ? Path.GetFileName(zip) : null });
                }
            }
            else
            {
                int z = NumaraZ(folder);
                total = z;
                string zip = PrimulZip(folder);
                if (zip != null) zips.Add(zip);
                if (z > 0) linii.Add(new PvLinieExport { Eticheta = "Rapoarte Z " + zFrom + " - " + zTo, Fisiere = z, Arhiva = zip != null ? Path.GetFileName(zip) : null });
            }
            return total;
        }

        private static int NumaraZ(string dir)
        {
            if (!Directory.Exists(dir)) return 0;
            int n = 0;
            foreach (var f in Directory.GetFiles(dir, "*.p7b"))
                if (!Path.GetFileName(f).StartsWith("Perioada_raportare", StringComparison.OrdinalIgnoreCase)) n++;
            return n;
        }

        private static string PrimulZip(string dir)
        {
            if (!Directory.Exists(dir)) return null;
            var z = Directory.GetFiles(dir, "*.zip");
            return z.Length > 0 ? z[0] : null;
        }

        private static string SafeNameExp(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "necunoscut";
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Trim();
        }
    }
}
