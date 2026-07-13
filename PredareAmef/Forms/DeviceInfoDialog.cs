using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PredareAmef.Services;

namespace PredareAmef.Forms
{
    /// <summary>
    /// Dialog cu info complet aparat fiscal — format Datecs Manager, monospace.
    /// </summary>
    public sealed class DeviceInfoDialog : Form
    {
        private readonly DeviceFullInfo _info;
        private TextBox txtInfo;
        private Button btnCopy;
        private Button btnSave;
        private Button btnClose;

        public DeviceInfoDialog(DeviceFullInfo info)
        {
            _info = info ?? new DeviceFullInfo();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Info aparat fiscal";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(700, 620);
            this.BackColor = Color.White;

            txtInfo = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9.5F),
                Location = new Point(12, 12),
                Size = new Size(676, 560),
                BackColor = Color.FromArgb(250, 250, 250),
                Text = _info.ToPrettyText()
            };

            btnCopy = new Button
            {
                Text = "📋 Copiaza",
                Location = new Point(12, 582),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat
            };
            btnCopy.FlatAppearance.BorderSize = 0;
            btnCopy.Click += (s, e) => { try { Clipboard.SetText(txtInfo.Text); } catch { } };

            btnSave = new Button
            {
                Text = "💾 Salveaza .txt",
                Location = new Point(140, 582),
                Size = new Size(140, 30),
                BackColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                using (var sfd = new SaveFileDialog
                {
                    Filter = "Text file (*.txt)|*.txt",
                    FileName = "info_aparat_" + (_info.SerialNumber ?? "unknown") + ".txt"
                })
                {
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        try { File.WriteAllText(sfd.FileName, txtInfo.Text, System.Text.Encoding.UTF8); }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, "Eroare salvare: " + ex.Message, "Save",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };

            btnClose = new Button
            {
                Text = "Inchide",
                Location = new Point(588, 582),
                Size = new Size(100, 30),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.FlatAppearance.BorderSize = 0;

            this.Controls.Add(txtInfo);
            this.Controls.Add(btnCopy);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnClose);
            this.AcceptButton = btnClose;
        }
    }
}
