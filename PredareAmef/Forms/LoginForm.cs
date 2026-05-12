using System;
using System.Drawing;
using System.Windows.Forms;

namespace PredareAmef.Forms
{
    /// <summary>
    /// Dialog de parola la pornirea aplicatiei. Parola hardcodata. UI light flat.
    /// </summary>
    public sealed class LoginForm : Form
    {
        private const string EXPECTED_PASSWORD = "0841";
        private const int MAX_ATTEMPTS = 3;

        private TextBox txtPassword;
        private Button btnOk, btnCancel;
        private Label lblTitle, lblSubtitle, lblMsg;
        private Panel headerPanel;
        private int _attempts = 0;

        public LoginForm()
        {
            Text = "Predare AMEF";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(420, 260);
            ShowInTaskbar = false;
            KeyPreview = true;
            Theme.ApplyFormStyle(this);

            // ── Header band (accent albastru sus) ─────────────────────────────
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Theme.Primary
            };

            var lblBrand = new Label
            {
                Text = "Predare AMEF",
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                Left = 24,
                Top = 18,
                BackColor = Color.Transparent
            };
            var lblBrandSub = new Label
            {
                Text = "Utilitar predare memorie fiscala",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(220, 230, 250),
                AutoSize = true,
                Left = 25,
                Top = 48,
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(lblBrand);
            headerPanel.Controls.Add(lblBrandSub);
            Controls.Add(headerPanel);

            // ── Continut ──────────────────────────────────────────────────────
            lblTitle = new Label
            {
                Left = 24, Top = 96, AutoSize = true,
                Text = "Autentificare",
                Font = Theme.FontTitle,
                ForeColor = Theme.Text
            };

            lblSubtitle = new Label
            {
                Left = 24, Top = 126, Width = 380, Height = 18,
                Text = "Introdu parola pentru a accesa aplicatia.",
                Font = Theme.FontBase,
                ForeColor = Theme.TextDim
            };

            txtPassword = new TextBox
            {
                Left = 24, Top = 154, Width = 372, Height = 32,
                Font = new Font("Segoe UI", 12F),
                PasswordChar = '•',
                MaxLength = 32
            };
            Theme.ApplyTextBox(txtPassword);

            lblMsg = new Label
            {
                Left = 24, Top = 190, Width = 372, Height = 18,
                ForeColor = Theme.Danger,
                Font = Theme.FontSmall,
                Text = ""
            };

            btnCancel = new Button
            {
                Left = 215, Top = 216, Width = 88, Height = 36,
                Text = "Anulare",
                DialogResult = DialogResult.Cancel
            };
            Theme.ApplyButtonSecondary(btnCancel);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            btnOk = new Button
            {
                Left = 308, Top = 216, Width = 88, Height = 36,
                Text = "Conectare",
                DialogResult = DialogResult.None
            };
            Theme.ApplyButtonPrimary(btnOk);
            btnOk.Click += (s, e) => TryLogin();

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[] { lblTitle, lblSubtitle, txtPassword, lblMsg, btnOk, btnCancel });

            Shown += (s, e) => txtPassword.Focus();
        }

        private void TryLogin()
        {
            _attempts++;
            if (txtPassword.Text == EXPECTED_PASSWORD)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            if (_attempts >= MAX_ATTEMPTS)
            {
                MessageBox.Show(this,
                    "Numar maxim de tentative atins. Aplicatia se inchide.",
                    "Acces refuzat", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            lblMsg.Text = "Parola incorecta (" + _attempts + "/" + MAX_ATTEMPTS + ")";
            txtPassword.SelectAll();
            txtPassword.Focus();
        }
    }
}
