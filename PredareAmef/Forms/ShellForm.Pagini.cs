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
                Height = 56,
                Font = Paleta.Corp,
                BorderStyle = BorderStyle.FixedSingle,
            };
            y += 68;

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

            var cardStart = new Card { Raza = 16, Height = 176, Fundal = Paleta.CardInchis, Contur = Color.Transparent };
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
            _lblSelectate = new Label { Text = "0 aparate", Font = new Font("Segoe UI Semibold", 13F), ForeColor = Color.White, AutoSize = true, Location = new Point(22, 92), BackColor = Color.Transparent };
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
                stanga.Bounds = new Rectangle(0, 0, latimeStanga, zona.Height - 4);
                int x = latimeStanga + 18;
                cardPv.Bounds = new Rectangle(x, 0, latimeDreapta, 96);
                cardEtichete.Bounds = new Rectangle(x, 112, latimeDreapta, 150);
                cardStart.Bounds = new Rectangle(x, 278, latimeDreapta, 176);
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
            _jurnal = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Paleta.CardInchis,
                ForeColor = Paleta.TextPeInchis,
                Font = Paleta.Monospatiat,
                ReadOnly = true,
                Margin = new Padding(16),
            };
            var gazdaJurnal = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 16, 18, 16), BackColor = Paleta.CardInchis };
            gazdaJurnal.Controls.Add(_jurnal);
            cardJurnal.Controls.Add(gazdaJurnal);

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
                    }, ct);
                }
                catch (Exception ex)
                {
                    try { BeginInvoke((MethodInvoker)delegate { ScrieInJurnal("Eroare: " + ex.Message, Paleta.EroareFundal); }); }
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

            ScrieInJurnal(DateTime.Now.ToString("HH:mm:ss") + "  " + s.Device.Serie + " — " + (s.SubLabel ?? s.Status),
                s.Done && s.Success ? Color.FromArgb(0x7A, 0xDF, 0xB4) : Paleta.TextPeInchis);
        }

        private void ScrieInJurnal(string text, Color culoare)
        {
            _jurnal.SelectionStart = _jurnal.TextLength;
            _jurnal.SelectionColor = culoare;
            _jurnal.AppendText(text + Environment.NewLine);
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
    }
}
