using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using PredareAmef.Services;

namespace PredareAmef.Forms
{
    public partial class MainForm : Form
    {
        private Thread _worker;
        private CancellationTokenSource _cts;

        public MainForm()
        {
            InitializeComponent();
            ApplyTheme();
            this.Shown += MainForm_Shown_CheckUpdate;
        }

        private async void MainForm_Shown_CheckUpdate(object sender, EventArgs e)
        {
            this.Shown -= MainForm_Shown_CheckUpdate;
            string auto = System.Configuration.ConfigurationManager.AppSettings["AutoCheckUpdate"];
            if (!string.IsNullOrEmpty(auto) && auto.Equals("false", StringComparison.OrdinalIgnoreCase))
                return;
            string token = System.Configuration.ConfigurationManager.AppSettings["GitHubToken"] ?? "";
            try
            {
                var info = await System.Threading.Tasks.Task.Run(() => Services.UpdateService.CheckForUpdate(token));
                if (info != null && info.HasUpdate && !string.IsNullOrEmpty(info.AssetDownloadUrl))
                {
                    using (var dlg = new UpdateDialog(info, token))
                        dlg.ShowDialog(this);
                }
            }
            catch { /* update e best-effort, nu blocheaza app */ }
        }

        /// <summary>
        /// Aplica styling light flat consistent peste designer-ul existent.
        /// Se ruleaza imediat dupa InitializeComponent.
        /// </summary>
        private void ApplyTheme()
        {
            Theme.ApplyFormStyle(this);

            // Butoane principale colorate
            Theme.ApplyButtonPrimary(btnStart);
            Theme.ApplyButtonSecondary(btnCancel);
            Theme.ApplyButtonSecondary(btnBrowseOutput);
            Theme.ApplyButtonSecondary(btnOpenOutput);
            Theme.ApplyButtonSuccess(btnMulti);

            // ── ALINIERE FORTATA: cele 4 butoane principale pe aceeasi linie (Y=310)
            // Designer-ul avea coordonate care la DPI > 100% se mutau inconsistent.
            // Aici forteaza pozitia identica pentru toate.
            const int BTN_Y = 310;
            const int BTN_H = 40;
            const int GAP   = 8;
            int x = 12;
            btnStart.Location      = new System.Drawing.Point(x, BTN_Y); btnStart.Size      = new System.Drawing.Size(320, BTN_H); x += 320 + GAP;
            btnCancel.Location     = new System.Drawing.Point(x, BTN_Y); btnCancel.Size     = new System.Drawing.Size(110, BTN_H); x += 110 + GAP;
            btnOpenOutput.Location = new System.Drawing.Point(x, BTN_Y); btnOpenOutput.Size = new System.Drawing.Size(175, BTN_H); x += 175 + GAP;
            btnMulti.Location      = new System.Drawing.Point(x, BTN_Y); btnMulti.Size      = new System.Drawing.Size(165, BTN_H);

            // Inputuri
            Theme.ApplyTextBox(txtClient);
            Theme.ApplyTextBox(txtOutput);
            Theme.ApplyTextBox(txtIp);
            Theme.ApplyTextBox(txtPort);
            Theme.ApplyComboBox(cbCom);
            Theme.ApplyComboBox(cbBaud);

            // GroupBox-uri (header subtle)
            foreach (System.Windows.Forms.Control c in new System.Windows.Forms.Control[] { grpConn, grpOpt, grpSteps })
            {
                if (c is System.Windows.Forms.GroupBox gb)
                {
                    gb.ForeColor = Theme.TextDim;
                    gb.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
                }
            }

            // Log (background curat)
            rtbLog.BackColor = Theme.Surface2;
            rtbLog.ForeColor = Theme.Text;
            rtbLog.Font = Theme.FontMono;
            rtbLog.BorderStyle = System.Windows.Forms.BorderStyle.None;

            // Label-uri secundare
            lblStatus.ForeColor = Theme.Text;
            lblStatus.Font = Theme.FontBaseBold;
            lblSubStatus.ForeColor = Theme.TextDim;
            lblSubStatus.Font = Theme.FontSmall;

            // Progress bars: continuous
            prg.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            prgSub.Style = System.Windows.Forms.ProgressBarStyle.Continuous;

            // Re-titluri GroupBox cu prefix mai modern
            if (grpConn != null)  grpConn.Text  = "CONEXIUNE APARAT";
            if (grpOpt != null)   grpOpt.Text   = "IDENTIFICARE CLIENT + OUTPUT";
            if (grpSteps != null) grpSteps.Text = "PASI PREDARE";
        }

        private void OnLoad(object sender, EventArgs e)
        {
            cbCom.Items.Clear();
            foreach (var p in SerialPort.GetPortNames()) cbCom.Items.Add(p);
            if (cbCom.Items.Count > 0)
            {
                // Preselect COM6 daca exista
                int idx = cbCom.FindStringExact("COM6");
                cbCom.SelectedIndex = idx >= 0 ? idx : 0;
            }

            txtOutput.Text = ConfigurationManager.AppSettings["DefaultOutputRoot"];
            if (string.IsNullOrWhiteSpace(txtOutput.Text))
                txtOutput.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private void OnTransportChanged(object sender, EventArgs e)
        {
            bool serial = rbSerial.Checked;
            cbCom.Enabled = serial; cbBaud.Enabled = serial;
            txtIp.Enabled = !serial; txtPort.Enabled = !serial;
        }

        private void OnBrowseOutput(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Alege folderul radacina pentru output";
                if (Directory.Exists(txtOutput.Text)) dlg.SelectedPath = txtOutput.Text;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtOutput.Text = dlg.SelectedPath;
            }
        }

        private void OnOpenOutput(object sender, EventArgs e)
        {
            string path = txtOutput.Text;
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        private void OnMultiSession(object sender, EventArgs e)
        {
            using (var f = new MultiSessionForm()) { f.ShowDialog(this); }
        }

        private void OnCancel(object sender, EventArgs e)
        {
            if (_cts == null || _cts.IsCancellationRequested) return;
            var res = MessageBox.Show(this,
                "Anulezi predarea? Operatiunea curenta se va incheia la urmatorul punct de verificare (cateva secunde). Fisierele deja salvate raman in folderul output.",
                "Confirm anulare",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res != DialogResult.Yes) return;
            _cts.Cancel();
            btnCancel.Enabled = false;
            lblStatus.Text = "Anulare in curs...";
            AppendLog("Cerere de anulare trimisa — astept punct de verificare cooperativ.", LogLevel.Warning);
        }

        private void OnStart(object sender, EventArgs e)
        {
            if (_worker != null && _worker.IsAlive)
            {
                MessageBox.Show(this, "Predarea ruleaza deja.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var opt = BuildOptions();
            if (opt == null) return;

            rtbLog.Clear();
            btnStart.Enabled = false;
            btnCancel.Enabled = true;
            btnOpenOutput.Enabled = false;
            prg.Value = 0;
            prgSub.Value = 0;
            lblStatus.Text = "Pornire...";
            lblSubStatus.Text = "";

            var logger = new ActionLogger(AppendLog, UpdateProgress, UpdateSubProgress);
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            _worker = new Thread(() =>
            {
                try
                {
                    var orch = new HandoverOrchestrator();
                    bool ok = orch.Run(opt, logger, ct);
                    this.Invoke(new Action(() =>
                    {
                        if (ct.IsCancellationRequested)
                            lblStatus.Text = "Predare anulata.";
                        else
                            lblStatus.Text = ok ? "Predare finalizata cu succes." : "Predare esuata sau partiala.";
                        btnStart.Enabled = true;
                        btnCancel.Enabled = false;
                        btnOpenOutput.Enabled = true;
                        if (!string.IsNullOrWhiteSpace(orch.OutputDir))
                        {
                            txtOutput.Text = orch.OutputDir;
                            if (Directory.Exists(orch.OutputDir))
                            {
                                try { Process.Start(new ProcessStartInfo(orch.OutputDir) { UseShellExecute = true }); }
                                catch { /* nu e fatal */ }
                            }
                        }
                        logger.Dispose();
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        AppendLog("Exceptie: " + ex.Message, LogLevel.Error);
                        lblStatus.Text = "Eroare — vezi log.";
                        btnStart.Enabled = true;
                        btnCancel.Enabled = false;
                        btnOpenOutput.Enabled = true;
                        logger.Dispose();
                    }));
                }
            });
            _worker.IsBackground = true;
            _worker.Start();
        }

        private HandoverOptions BuildOptions()
        {
            var opt = new HandoverOptions
            {
                Transport = rbSerial.Checked ? TransportKind.Serial : TransportKind.Lan,
                ComPort = cbCom.SelectedItem?.ToString() ?? "",
                BaudRate = int.TryParse(cbBaud.SelectedItem?.ToString(), out int b) ? b : 115200,
                LanIp = txtIp.Text.Trim(),
                LanPort = int.TryParse(txtPort.Text.Trim(), out int p) ? p : 3999,
                OutputRoot = txtOutput.Text.Trim(),
                ClientName = string.IsNullOrWhiteSpace(txtClient.Text) ? "Client" : txtClient.Text.Trim(),
                Step1_DumpFm = chkStep1.Checked,
                Step3_GenerateTxt = chkStep3.Checked,
                Step5_PrintSummary = chkStep5.Checked,
                Step6_ExportXml = chkStep6.Checked,
                Step7_ExportHeader = chkStep7.Checked
            };

            if (opt.Transport == TransportKind.Serial && string.IsNullOrWhiteSpace(opt.ComPort))
            {
                MessageBox.Show(this, "Selecteaza un port COM.", "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            if (string.IsNullOrWhiteSpace(opt.OutputRoot) || !Directory.Exists(opt.OutputRoot))
            {
                MessageBox.Show(this, "Folderul de output nu exista.", "Validare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            return opt;
        }

        // ──── Logging thread-safe ────

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
                case LogLevel.Success: c = Color.FromArgb(0, 150, 0); break;
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

        private void UpdateProgress(int step, int total, string name)
        {
            if (prg.InvokeRequired)
            {
                prg.BeginInvoke(new Action<int, int, string>(UpdateProgress), step, total, name);
                return;
            }
            prg.Maximum = total;
            prg.Value = Math.Min(step, total);
            lblStatus.Text = "Pas " + step + "/" + total + ": " + name;

            // Reset sub-progress on step change
            prgSub.Value = 0;
            lblSubStatus.Text = "";
        }

        private void UpdateSubProgress(long current, long total, string label)
        {
            if (prgSub.InvokeRequired)
            {
                prgSub.BeginInvoke(new Action<long, long, string>(UpdateSubProgress), current, total, label);
                return;
            }
            int pct = total > 0 ? (int)Math.Min(100, (100L * current) / total) : 0;
            prgSub.Value = pct;
            lblSubStatus.Text = label + "   [" + pct + "%]";
        }
    }
}
