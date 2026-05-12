using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using PredareAmef.Services;

namespace PredareAmef.Forms
{
    /// <summary>
    /// Dialog de confirmare update + progress download.
    /// </summary>
    public sealed class UpdateDialog : Form
    {
        private readonly UpdateService.UpdateInfo _info;
        private readonly string _token;
        private Label lblTitle, lblVersion, lblNotes, lblStatus;
        private TextBox txtNotes;
        private Button btnUpdate, btnLater;
        private ProgressBar progress;
        private Panel headerPanel;

        public UpdateDialog(UpdateService.UpdateInfo info, string token)
        {
            _info = info;
            _token = token;

            Text = "Update disponibil — Predare AMEF";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 420);
            Theme.ApplyFormStyle(this);
            ShowInTaskbar = false;

            // Header band
            headerPanel = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Theme.Primary };
            var lblBrand = new Label
            {
                Text = "🚀  Versiune nouă disponibilă",
                Font = new Font("Segoe UI Semibold", 14F),
                ForeColor = Color.White,
                Left = 20, Top = 20, AutoSize = true, BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(lblBrand);
            Controls.Add(headerPanel);

            lblVersion = new Label
            {
                Left = 24, Top = 90, AutoSize = true,
                Text = "Versiune curentă: v" + _info.CurrentVersion + "  →  v" + (_info.LatestVersion != null ? _info.LatestVersion.ToString() : _info.TagName),
                Font = Theme.FontSubtitle, ForeColor = Theme.Text
            };

            lblNotes = new Label
            {
                Left = 24, Top = 122, AutoSize = true,
                Text = "Note de versiune:",
                Font = Theme.FontBaseBold, ForeColor = Theme.TextDim
            };

            txtNotes = new TextBox
            {
                Left = 24, Top = 144, Width = 472, Height = 170,
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                Text = string.IsNullOrWhiteSpace(_info.ReleaseNotes) ? "(fără note)" : _info.ReleaseNotes,
                Font = Theme.FontBase
            };
            Theme.ApplyTextBox(txtNotes);

            progress = new ProgressBar
            {
                Left = 24, Top = 326, Width = 472, Height = 8,
                Style = ProgressBarStyle.Continuous, Minimum = 0, Maximum = 100,
                Visible = false
            };

            lblStatus = new Label
            {
                Left = 24, Top = 338, Width = 472, Height = 18,
                Text = "", Font = Theme.FontSmall, ForeColor = Theme.TextDim
            };

            btnLater = new Button
            {
                Left = 290, Top = 368, Width = 100, Height = 36,
                Text = "Mai târziu", DialogResult = DialogResult.Cancel
            };
            Theme.ApplyButtonSecondary(btnLater);
            btnLater.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            btnUpdate = new Button
            {
                Left = 396, Top = 368, Width = 100, Height = 36,
                Text = "Actualizează"
            };
            Theme.ApplyButtonPrimary(btnUpdate);
            btnUpdate.Click += async (s, e) => await DoUpdate();

            Controls.AddRange(new Control[] { lblVersion, lblNotes, txtNotes, progress, lblStatus, btnLater, btnUpdate });
        }

        private async Task DoUpdate()
        {
            btnUpdate.Enabled = false;
            btnLater.Enabled = false;
            progress.Visible = true;
            lblStatus.Text = "Descărcare în curs...";

            // Marchez "tried" PREVENTIV — daca update-ul esueaza (copy locked etc.),
            // nu vom mai prompt-ui aceeasi versiune in urmatoarele 6h.
            UpdateService.MarkVersionTried(_info.TagName);

            string newExe = await Task.Run(() => UpdateService.DownloadUpdate(_info, _token, (rcv, total) =>
            {
                if (total > 0)
                {
                    int pct = (int)((rcv * 100) / total);
                    BeginInvoke(new Action(() =>
                    {
                        progress.Value = Math.Min(100, Math.Max(0, pct));
                        lblStatus.Text = "Descărcat " + (rcv / 1024) + " KB / " + (total / 1024) + " KB (" + pct + "%)";
                    }));
                }
            }));

            if (string.IsNullOrEmpty(newExe))
            {
                lblStatus.Text = "Eroare la descărcare. Verifică conexiunea sau token-ul.";
                lblStatus.ForeColor = Theme.Danger;
                btnUpdate.Enabled = true;
                btnLater.Enabled = true;
                progress.Visible = false;
                return;
            }

            lblStatus.Text = "Descărcare completă. Aplicația se va reporni...";
            lblStatus.ForeColor = Theme.Success;
            progress.Value = 100;
            await Task.Delay(800);
            UpdateService.ApplyUpdateAndRestart(newExe);
        }
    }
}
