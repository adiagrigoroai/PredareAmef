using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PredareAmef.Services;

namespace PredareAmef.Forms
{
    /// <summary>
    /// Dialog scanare aparate fiscale pe COM porturi + afisare automata info aparat
    /// (Datecs Manager style) la selectarea unui aparat din lista.
    /// </summary>
    public sealed class ScanDevicesDialog : Form
    {
        private Label lblStatus;
        private ProgressBar prg;
        private ListView lvDevices;
        private Label lblInfoTitle;
        private TextBox txtInfo;
        private Button btnRescan;
        private Button btnCopyInfo;
        private Button btnSelect;
        private Button btnClose;
        private CancellationTokenSource _cts;
        private CancellationTokenSource _infoCts;

        /// <summary>Aparatul selectat de utilizator (null daca s-a inchis fara selectie).</summary>
        public ScannedDevice Selected { get; private set; }

        public ScanDevicesDialog()
        {
            InitializeComponent();
            this.Shown += async (s, e) => await ScanAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "Scan aparate fiscale (Serial COM) + Info detaliat";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(1000, 680);
            this.ClientSize = new Size(1100, 720);
            this.BackColor = Color.White;

            lblStatus = new Label
            {
                Location = new Point(12, 12),
                Size = new Size(1076, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                Text = "Scanare in progres..."
            };

            prg = new ProgressBar
            {
                Location = new Point(12, 38),
                Size = new Size(1076, 18),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            // Lista aparate (sus)
            lvDevices = new ListView
            {
                Location = new Point(12, 66),
                Size = new Size(1076, 180),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HideSelection = false,
                Font = new Font("Segoe UI", 9.5F),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            lvDevices.Columns.Add("Port", 80);
            lvDevices.Columns.Add("Baud", 70);
            lvDevices.Columns.Add("Model", 110);
            lvDevices.Columns.Add("Serie", 140);
            lvDevices.Columns.Add("Firma", 260);
            lvDevices.Columns.Add("CIF", 120);
            lvDevices.Columns.Add("FM Nr", 140);
            lvDevices.DoubleClick += (s, e) => SelectCurrent();
            lvDevices.SelectedIndexChanged += LvDevices_SelectedIndexChanged;

            // Titlu panou info
            lblInfoTitle = new Label
            {
                Location = new Point(12, 258),
                Size = new Size(1076, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                Text = "Info detaliat aparat (selecteaza un rand din tabelul de sus):",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // Panou info (jos) — multiline monospace
            txtInfo = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9.5F),
                Location = new Point(12, 284),
                Size = new Size(1076, 380),
                BackColor = Color.FromArgb(250, 250, 250),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Text = ""
            };

            btnRescan = new Button
            {
                Text = "🔄 Rescan",
                Location = new Point(12, 678),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnRescan.FlatAppearance.BorderSize = 0;
            btnRescan.Click += async (s, e) => await ScanAsync();

            btnCopyInfo = new Button
            {
                Text = "📋 Copiaza info",
                Location = new Point(142, 678),
                Size = new Size(140, 30),
                BackColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnCopyInfo.FlatAppearance.BorderSize = 0;
            btnCopyInfo.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtInfo.Text))
                    try { Clipboard.SetText(txtInfo.Text); } catch { }
            };

            btnSelect = new Button
            {
                Text = "✔ Selecteaza aparat",
                Location = new Point(830, 678),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(0, 150, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnSelect.FlatAppearance.BorderSize = 0;
            btnSelect.Click += (s, e) => SelectCurrent();

            btnClose = new Button
            {
                Text = "Anuleaza",
                Location = new Point(988, 678),
                Size = new Size(100, 30),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnClose.FlatAppearance.BorderSize = 0;

            this.Controls.Add(lblStatus);
            this.Controls.Add(prg);
            this.Controls.Add(lvDevices);
            this.Controls.Add(lblInfoTitle);
            this.Controls.Add(txtInfo);
            this.Controls.Add(btnRescan);
            this.Controls.Add(btnCopyInfo);
            this.Controls.Add(btnSelect);
            this.Controls.Add(btnClose);
            this.CancelButton = btnClose;
        }

        private async void LvDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnSelect.Enabled = lvDevices.SelectedItems.Count > 0;
            btnCopyInfo.Enabled = false;
            if (lvDevices.SelectedItems.Count == 0)
            {
                txtInfo.Text = "";
                lblInfoTitle.Text = "Info detaliat aparat (selecteaza un rand din tabelul de sus):";
                return;
            }
            var sd = lvDevices.SelectedItems[0].Tag as ScannedDevice;
            if (sd == null) return;

            _infoCts?.Cancel();
            _infoCts = new CancellationTokenSource();
            var ct = _infoCts.Token;

            lblInfoTitle.Text = "Citire info aparat " + sd.ComPort + " @ " + sd.Baud + " ... (poate dura 5-15 sec)";
            lblInfoTitle.ForeColor = Color.FromArgb(245, 158, 11);
            txtInfo.Text = "";
            Cursor = Cursors.WaitCursor;

            try
            {
                string prettyText = await Task.Run(() =>
                {
                    string err;
                    using (var dude = DudeClient.OpenSerial(sd.ComPort, sd.Baud, out err))
                    {
                        if (dude == null) return "Eroare deschidere port: " + (err ?? "");
                        if (ct.IsCancellationRequested) return "";
                        var info = DeviceInfoReader.Read(dude, null);
                        return info.ToPrettyText();
                    }
                });
                if (ct.IsCancellationRequested) return;
                txtInfo.Text = prettyText;
                lblInfoTitle.Text = "Info detaliat aparat " + sd.ComPort + " (" + sd.Model + " | " + sd.Serie + ")";
                lblInfoTitle.ForeColor = Color.FromArgb(37, 99, 235);
                btnCopyInfo.Enabled = true;
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                {
                    txtInfo.Text = "Eroare citire info: " + ex.Message;
                    lblInfoTitle.Text = "Eroare citire info aparat";
                    lblInfoTitle.ForeColor = Color.FromArgb(220, 60, 60);
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private async Task ScanAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            lblStatus.Text = "Scanare porturi COM...";
            lblStatus.ForeColor = Color.FromArgb(37, 99, 235);
            prg.Style = ProgressBarStyle.Marquee;
            lvDevices.Items.Clear();
            txtInfo.Text = "";
            lblInfoTitle.Text = "Info detaliat aparat (selecteaza un rand din tabelul de sus):";
            lblInfoTitle.ForeColor = Color.FromArgb(80, 80, 80);
            btnRescan.Enabled = false;
            btnSelect.Enabled = false;
            btnCopyInfo.Enabled = false;

            List<ScannedDevice> found;
            try
            {
                found = await Task.Run(() => new DeviceScannerService().ScanAllPorts(null, _cts.Token));
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Eroare scan: " + ex.Message;
                lblStatus.ForeColor = Color.Red;
                prg.Style = ProgressBarStyle.Blocks;
                btnRescan.Enabled = true;
                return;
            }

            prg.Style = ProgressBarStyle.Blocks;
            btnRescan.Enabled = true;

            if (found.Count == 0)
            {
                lblStatus.Text = "Niciun aparat gasit. Verifica cablul + alimentarea.";
                lblStatus.ForeColor = Color.FromArgb(180, 60, 60);
                return;
            }

            lblStatus.Text = "Gasit " + found.Count + " aparat(e). Selecteaza unul din tabel — info detaliat apare automat mai jos.";
            lblStatus.ForeColor = Color.FromArgb(16, 130, 60);
            foreach (var d in found)
            {
                var it = new ListViewItem(d.ComPort);
                it.SubItems.Add(d.Baud.ToString());
                it.SubItems.Add(d.Model);
                it.SubItems.Add(d.Serie);
                it.SubItems.Add(d.Firma);
                it.SubItems.Add(d.CIF);
                it.SubItems.Add(d.FmNum);
                it.Tag = d;
                lvDevices.Items.Add(it);
            }
            if (lvDevices.Items.Count == 1)
            {
                lvDevices.Items[0].Selected = true;
                lvDevices.Items[0].Focused = true;
                // Auto-triggers SelectedIndexChanged → citeste info automat
            }
        }

        private void SelectCurrent()
        {
            if (lvDevices.SelectedItems.Count == 0) return;
            Selected = lvDevices.SelectedItems[0].Tag as ScannedDevice;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cts?.Cancel();
            _infoCts?.Cancel();
            base.OnFormClosing(e);
        }
    }
}
