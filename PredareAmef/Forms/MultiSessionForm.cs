using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PredareAmef.Services;

namespace PredareAmef.Forms
{
    /// <summary>
    /// UI multi-aparat: Scan COM ports → lista aparate → pornire predari paralele.
    /// </summary>
    public sealed class MultiSessionForm : Form
    {
        private TextBox txtOutput, txtEmail;
        private Button btnBrowse, btnScan, btnStart, btnCancel, btnOpenRoot, btnRetryAnaf;
        private NumericUpDown numParallel;
        private DataGridView grid;
        private RichTextBox rtbLog;
        private Label lblStatus;
        private CancellationTokenSource _scanCts, _runCts;
        private List<ScannedDevice> _scanned = new List<ScannedDevice>();
        private List<SessionStatus> _sessions = new List<SessionStatus>();
        private MultiSessionRunner _runner;
        private System.Windows.Forms.Timer _refreshTimer;

        public MultiSessionForm()
        {
            Text = "Predare AMEF — Multi-aparat";
            Size = new Size(1100, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BuildUI();
            ApplyTheme();
            Load += OnLoad;
        }

        /// <summary>
        /// Aplica styling light flat consistent peste UI-ul construit cu coordonate fixe.
        /// </summary>
        private void ApplyTheme()
        {
            Theme.ApplyFormStyle(this);

            // Butoane
            Theme.ApplyButtonSecondary(btnBrowse);
            Theme.ApplyButtonPrimary(btnScan);
            Theme.ApplyButtonSuccess(btnStart);
            Theme.ApplyButtonSecondary(btnCancel);
            Theme.ApplyButtonSecondary(btnOpenRoot);
            Theme.ApplyButtonWarning(btnRetryAnaf);

            // Inputuri
            Theme.ApplyTextBox(txtOutput);
            Theme.ApplyTextBox(txtEmail);

            numParallel.BorderStyle = BorderStyle.FixedSingle;
            numParallel.BackColor = Theme.Surface;
            numParallel.ForeColor = Theme.Text;
            numParallel.Font = Theme.FontBase;

            // Grid
            Theme.ApplyDataGridView(grid);

            // Log
            rtbLog.BackColor = Theme.Surface2;
            rtbLog.ForeColor = Theme.Text;
            rtbLog.Font = Theme.FontMono;
            rtbLog.BorderStyle = BorderStyle.None;

            // Status bar
            lblStatus.ForeColor = Theme.TextDim;
            lblStatus.Font = Theme.FontSmall;

            // Label-uri (le iterez pe toate)
            foreach (Control c in Controls)
            {
                if (c is Label lbl && lbl != lblStatus)
                {
                    lbl.ForeColor = Theme.TextDim;
                    lbl.Font = Theme.FontBase;
                }
            }

            // ── HEADER BAND + FOOTER cu versiune (identic cu MainForm) ─────────
            string ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            this.Text = "Predare AMEF v" + ver + " — Multi-aparat";

            const int HEADER_H = 56;
            const int FOOTER_H = 26;

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = HEADER_H,
                BackColor = Theme.Primary
            };
            headerPanel.Controls.Add(new Label
            {
                Text = "Predare AMEF — Multi-aparat",
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = Color.White,
                Left = 18, Top = 8, AutoSize = true, BackColor = Color.Transparent
            });
            headerPanel.Controls.Add(new Label
            {
                Text = "Scanare COM + predare paralela pe mai multe aparate",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(220, 230, 250),
                Left = 19, Top = 34, AutoSize = true, BackColor = Color.Transparent
            });
            var lblVerBadge = new Label
            {
                Text = "v" + ver,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.FromArgb(50, 0, 0, 0),
                Padding = new Padding(8, 4, 8, 4),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblVerBadge.Location = new Point(this.ClientSize.Width - 80, 18);
            headerPanel.Controls.Add(lblVerBadge);
            this.Controls.Add(headerPanel);
            headerPanel.BringToFront();

            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom, Height = FOOTER_H,
                BackColor = Theme.Surface2,
                Padding = new Padding(12, 0, 12, 0)
            };
            footerPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Border))
                    e.Graphics.DrawLine(pen, 0, 0, footerPanel.Width, 0);
            };
            footerPanel.Controls.Add(new Label
            {
                Text = "PredareAmef v" + ver + "  •  IT Smart Retail © 2026",
                Font = Theme.FontSmall, ForeColor = Theme.TextDim,
                AutoSize = true, Left = 12, Top = 6, BackColor = Color.Transparent
            });
            this.Controls.Add(footerPanel);
            footerPanel.BringToFront();

            // Mut toate controls existente cu HEADER_H in jos (sub header)
            foreach (Control c in this.Controls)
            {
                if (c == headerPanel || c == footerPanel) continue;
                c.Top += HEADER_H;
            }
            // Extind form-ul
            this.ClientSize = new Size(this.ClientSize.Width, this.ClientSize.Height + HEADER_H + FOOTER_H);
        }

        private void BuildUI()
        {
            var lblOut = new Label { Left = 12, Top = 14, Width = 70, Text = "Output:" };
            txtOutput = new TextBox { Left = 80, Top = 12, Width = 600 };
            btnBrowse = new Button { Left = 685, Top = 11, Width = 80, Text = "Browse..." };
            btnBrowse.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    if (Directory.Exists(txtOutput.Text)) dlg.SelectedPath = txtOutput.Text;
                    if (dlg.ShowDialog(this) == DialogResult.OK) txtOutput.Text = dlg.SelectedPath;
                }
            };

            var lblEmail = new Label { Left = 12, Top = 44, Width = 70, Text = "Email:" };
            txtEmail = new TextBox { Left = 80, Top = 42, Width = 250 };
            // PlaceholderText nu e disponibil in .NET 4.7.2 — folosesc tooltip in schimb
            new ToolTip().SetToolTip(txtEmail, "Email pentru notificare in caz de esec (lasa gol pentru a dezactiva)");

            var lblPar = new Label { Left = 350, Top = 44, Width = 90, Text = "Concurenta:" };
            numParallel = new NumericUpDown { Left = 440, Top = 42, Width = 50, Minimum = 1, Maximum = 10, Value = 2 };

            btnScan = new Button { Left = 510, Top = 41, Width = 130, Text = "Scaneaza COM" };
            btnScan.Click += BtnScan_Click;

            btnStart = new Button
            {
                Left = 645, Top = 41, Width = 120, Text = "Porneste predare",
                BackColor = Color.FromArgb(0, 150, 0), ForeColor = Color.White, Enabled = false
            };
            btnStart.Click += BtnStart_Click;

            btnCancel = new Button { Left = 770, Top = 41, Width = 80, Text = "Anuleaza", Enabled = false };
            btnCancel.Click += (s, e) =>
            {
                if (_scanCts != null && !_scanCts.IsCancellationRequested) _scanCts.Cancel();
                if (_runCts != null && !_runCts.IsCancellationRequested) _runCts.Cancel();
                btnCancel.Enabled = false;
            };

            btnOpenRoot = new Button { Left = 855, Top = 41, Width = 105, Text = "Deschide folder", Enabled = false };
            btnOpenRoot.Click += (s, e) =>
            {
                if (_runner != null && Directory.Exists(_runner.SessionRootDir))
                    Process.Start(new ProcessStartInfo(_runner.SessionRootDir) { UseShellExecute = true });
            };

            btnRetryAnaf = new Button
            {
                Left = 965, Top = 41, Width = 105, Text = "Reia ANAF",
                BackColor = Color.FromArgb(220, 130, 0), ForeColor = Color.White,
                Enabled = false
            };
            new ToolTip().SetToolTip(btnRetryAnaf,
                "Repornesti fizic aparatele cu ANAF esuat, apoi apesi acest buton.\n" +
                "Aplicatia ruleaza DOAR Pas 5 (ANAF) pe acele aparate, secvential.");
            btnRetryAnaf.Click += BtnRetryAnaf_Click;

            grid = new DataGridView
            {
                Left = 12, Top = 80, Width = 1060, Height = 280,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                AutoGenerateColumns = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                MultiSelect = false
            };
            grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Sel", DataPropertyName = "Sel", Width = 40 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Port", Width = 70, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Baud", Width = 70, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Model", Width = 90, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Serie", Width = 130, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Firma", Width = 220, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CIF", Width = 120, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", Width = 150, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "%", Width = 50, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Detaliu", Width = 90, ReadOnly = true });

            rtbLog = new RichTextBox
            {
                Left = 12, Top = 370, Width = 1060, Height = 270,
                ReadOnly = true, Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(248, 248, 248)
            };

            lblStatus = new Label { Left = 12, Top = 645, Width = 1060, Height = 22, Text = "Pregatit." };

            Controls.AddRange(new Control[]
            {
                lblOut, txtOutput, btnBrowse,
                lblEmail, txtEmail, lblPar, numParallel,
                btnScan, btnStart, btnCancel, btnOpenRoot, btnRetryAnaf,
                grid, rtbLog, lblStatus
            });

            _refreshTimer = new System.Windows.Forms.Timer { Interval = 400 };
            _refreshTimer.Tick += (s, e) => RefreshGridStatuses();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            txtOutput.Text = ConfigurationManager.AppSettings["DefaultOutputRoot"];
            if (string.IsNullOrWhiteSpace(txtOutput.Text))
                txtOutput.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        // ─── SCAN ─────────────────────────────────────────────

        private void BtnScan_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
            grid.Rows.Clear();
            _scanned.Clear();
            btnScan.Enabled = false;
            btnStart.Enabled = false;
            btnCancel.Enabled = true;
            lblStatus.Text = "Scanare COM porturi...";

            _scanCts = new CancellationTokenSource();
            var ct = _scanCts.Token;

            var logger = new ActionLogger(AppendLog);

            Task.Run(() =>
            {
                try
                {
                    var scanner = new DeviceScannerService();
                    var found = scanner.ScanAllPorts(logger, ct);
                    this.Invoke((Action)(() =>
                    {
                        _scanned = found;
                        foreach (var d in _scanned)
                        {
                            int idx = grid.Rows.Add(true, d.ComPort, d.Baud, d.Model, d.Serie, d.Firma, d.CIF, "asteapta", 0, "");
                        }
                        btnScan.Enabled = true;
                        btnStart.Enabled = _scanned.Count > 0;
                        btnCancel.Enabled = false;
                        lblStatus.Text = "Scan complet: " + _scanned.Count + " aparat(e) detectat(e).";
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke((Action)(() =>
                    {
                        AppendLog("Scan exceptie: " + ex.Message, LogLevel.Error);
                        btnScan.Enabled = true;
                        btnCancel.Enabled = false;
                    }));
                }
                finally { logger.Dispose(); }
            });
        }

        // ─── START ─────────────────────────────────────────────

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (_scanned == null || _scanned.Count == 0) return;
            for (int i = 0; i < _scanned.Count; i++)
            {
                bool sel = (bool)(grid.Rows[i].Cells[0].Value ?? false);
                _scanned[i].Selected = sel;
            }
            var selected = _scanned.FindAll(d => d.Selected);
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Selecteaza cel putin un aparat.", "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtOutput.Text) || !Directory.Exists(txtOutput.Text))
            {
                MessageBox.Show(this, "Folderul de output nu exista.", "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int parallel = (int)numParallel.Value;
            string emailNotify = txtEmail.Text.Trim();

            btnScan.Enabled = false;
            btnStart.Enabled = false;
            btnCancel.Enabled = true;
            btnOpenRoot.Enabled = false;
            lblStatus.Text = "Predare in curs (" + selected.Count + " aparate, max " + parallel + " in paralel)...";

            var baseOpt = new HandoverOptions
            {
                OutputRoot = txtOutput.Text.Trim(),
                Step1_DumpFm = true,
                Step3_GenerateTxt = true,
                Step5_PrintSummary = true,
                Step6_ExportXml = true,
                Step7_ExportHeader = true
            };

            _runCts = new CancellationTokenSource();
            var ct = _runCts.Token;

            // Map device → grid row index
            var rowMap = new Dictionary<string, int>();
            for (int i = 0; i < _scanned.Count; i++) rowMap[_scanned[i].ComPort] = i;

            _runner = new MultiSessionRunner();
            var statuses = _runner.InitSessions(selected, baseOpt);
            lock (_sessions) { _sessions = statuses; }
            AppendLog("Folder sesiune: " + _runner.SessionRootDir, LogLevel.Info);
            _refreshTimer.Start();

            Task.Run(() =>
            {
                try
                {
                    _runner.RunSessions(baseOpt, parallel, emailNotify, null, ct);
                    this.Invoke((Action)(() =>
                    {
                        _refreshTimer.Stop();
                        RefreshGridStatuses();
                        btnScan.Enabled = true;
                        btnStart.Enabled = true;
                        btnCancel.Enabled = false;
                        btnOpenRoot.Enabled = true;
                        int ok = 0, fail = 0, anafFail = 0;
                        foreach (var s in statuses)
                        {
                            if (s.Success) ok++; else fail++;
                            if (s.NeedsAnafRetry) anafFail++;
                        }
                        lblStatus.Text = "Finalizat: " + ok + " OK, " + fail + " esuat" +
                                         (anafFail > 0 ? " (" + anafFail + " cu ANAF esuat)" : "") +
                                         ". Folder: " + _runner.SessionRootDir;
                        AppendLog("Multi-sesiune finalizata: " + ok + " OK / " + fail + " esuat.",
                                  fail == 0 ? LogLevel.Success : LogLevel.Warning);
                        if (anafFail > 0)
                        {
                            AppendLog("=" + anafFail + "= aparat(e) cu ANAF esuat. Reporneste-le si apasa 'Reia ANAF'.",
                                      LogLevel.Warning);
                            btnRetryAnaf.Enabled = true;
                        }
                        try { Process.Start(new ProcessStartInfo(_runner.SessionRootDir) { UseShellExecute = true }); } catch { }
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke((Action)(() =>
                    {
                        _refreshTimer.Stop();
                        AppendLog("Multi-sesiune exceptie: " + ex.Message, LogLevel.Error);
                        btnScan.Enabled = true;
                        btnStart.Enabled = true;
                        btnCancel.Enabled = false;
                    }));
                }
            });
        }

        private void BtnRetryAnaf_Click(object sender, EventArgs e)
        {
            if (_runner == null) return;
            int needRetry = 0;
            foreach (var s in _runner.Statuses) if (s.NeedsAnafRetry) needRetry++;
            if (needRetry == 0)
            {
                MessageBox.Show(this, "Niciun aparat nu necesita reluare ANAF.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var res = MessageBox.Show(this,
                "Au fost detectate " + needRetry + " aparat(e) cu ANAF esuat.\n\n" +
                "Inainte de a continua, REPORNESTE FIZIC aceste aparate (oprit ~10 sec, repornit).\n" +
                "Aplicatia va rula DOAR Pas 5 (ANAF) pe ele, secvential.\n\n" +
                "Continui?",
                "Reia ANAF dupa reboot", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res != DialogResult.Yes) return;

            btnRetryAnaf.Enabled = false;
            btnScan.Enabled = false;
            btnStart.Enabled = false;
            btnCancel.Enabled = true;
            lblStatus.Text = "Reia ANAF (secvential, " + needRetry + " aparate)...";

            _runCts = new CancellationTokenSource();
            var ct = _runCts.Token;
            _refreshTimer.Start();

            Task.Run(() =>
            {
                try { _runner.RetryAnafForFailedSessions(null, ct); }
                catch (Exception ex) { this.Invoke((Action)(() => AppendLog("Retry ANAF exceptie: " + ex.Message, LogLevel.Error))); }
                finally
                {
                    this.Invoke((Action)(() =>
                    {
                        _refreshTimer.Stop();
                        RefreshGridStatuses();
                        int stillFail = 0; int recovered = 0;
                        foreach (var s in _runner.Statuses)
                        {
                            if (s.AnafFileCount == 0) stillFail++;
                            else if (s.AnafFileCount > 0 && s.Status.Contains("finalizat")) recovered++;
                        }
                        AppendLog("Retry ANAF terminat: " + recovered + " recuperate, " + stillFail + " inca esuate.",
                                  stillFail == 0 ? LogLevel.Success : LogLevel.Warning);
                        btnRetryAnaf.Enabled = stillFail > 0;
                        btnScan.Enabled = true;
                        btnStart.Enabled = _scanned.Count > 0;
                        btnCancel.Enabled = false;
                    }));
                }
            });
        }

        private void RefreshGridStatuses()
        {
            // Map sesiuni inapoi pe grid prin ComPort
            lock (_sessions)
            {
                if (_sessions.Count == 0) return;
                foreach (var s in _sessions)
                {
                    for (int i = 0; i < grid.Rows.Count; i++)
                    {
                        var row = grid.Rows[i];
                        if (row.Cells[1].Value?.ToString() == s.Device.ComPort)
                        {
                            row.Cells[7].Value = s.Status;
                            row.Cells[8].Value = s.Progress;
                            row.Cells[9].Value = s.Done ? (s.Success ? "OK" : "ERR") : s.SubLabel;
                            row.DefaultCellStyle.BackColor = s.Done
                                ? (s.Success ? Color.FromArgb(220, 255, 220) : Color.FromArgb(255, 220, 220))
                                : Color.White;
                            break;
                        }
                    }
                }
            }
        }

        // ─── LOG ─────────────────────────────────────────────

        private void AppendLog(string msg, LogLevel level)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.BeginInvoke(new Action<string, LogLevel>(AppendLog), msg, level);
                return;
            }
            Color c;
            switch (level)
            {
                case LogLevel.Success: c = Color.FromArgb(0, 130, 0); break;
                case LogLevel.Warning: c = Color.DarkOrange; break;
                case LogLevel.Error:   c = Color.Red; break;
                case LogLevel.Debug:   c = Color.Gray; break;
                default:               c = Color.Black; break;
            }
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = c;
            rtbLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + Environment.NewLine);
            rtbLog.SelectionColor = rtbLog.ForeColor;
            rtbLog.ScrollToCaret();
        }
    }
}
