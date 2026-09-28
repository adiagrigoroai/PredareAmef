using System;
using System.Drawing;
using System.Windows.Forms;
using PredareAmef.Services;

namespace PredareAmef.Forms
{
    /// <summary>
    /// Datele care nu se pot citi din aparat pentru procesul verbal de predare: motivul,
    /// cine preda, daca s-a montat memorie noua si unitatea de service (ea alege sablonul).
    ///
    /// Restul — numarul de rapoarte Z, totalul vanzarilor si perioada de utilizare — se iau
    /// singure din raportul memoriei, la finalul predarii.
    /// </summary>
    public sealed class PvDialog : Form
    {
        private readonly ComboBox _motiv = new ComboBox();
        private readonly TextBox _predatDe = new TextBox();
        private readonly ComboBox _memorieNoua = new ComboBox();
        private readonly ComboBox _unitate = new ComboBox();
        private readonly TextBox _observatii = new TextBox();
        private readonly CheckBox _genereaza = new CheckBox();

        public bool Genereaza { get { return _genereaza.Checked; } }
        public string Motiv { get { return _motiv.Text.Trim(); } }
        public string PredatDe { get { return _predatDe.Text.Trim(); } }
        public string MemorieNoua { get { return _memorieNoua.Text.Trim(); } }
        public string UnitateService { get { return _unitate.Text.Trim(); } }
        public string Observatii { get { return _observatii.Text.Trim(); } }

        public PvDialog(HandoverOptions valoriInitiale)
        {
            Text = "Proces verbal de predare";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 330);
            Font = new Font("Segoe UI", 9f);

            int y = 14;

            _genereaza.Text = "Cere procesul verbal de la CRM la finalul predarii";
            _genereaza.SetBounds(16, y, 480, 22);
            _genereaza.Checked = valoriInitiale == null || valoriInitiale.GenereazaPv;
            _genereaza.CheckedChanged += (s, e) => ActualizeazaActive();
            Controls.Add(_genereaza);
            y += 34;

            AdaugaEticheta("Motivul predarii:", ref y, urmeazaControl: true);
            _motiv.SetBounds(150, y - 22, 350, 24);
            _motiv.DropDownStyle = ComboBoxStyle.DropDown;   // se poate si scrie altceva
            _motiv.Items.AddRange(new object[]
            {
                "Memorie fiscala plina",
                "Inlocuire memorie fiscala defecta",
                "Casare aparat",
                "Schimbare proprietar",
                "La cererea organului fiscal",
            });
            _motiv.Text = valoriInitiale != null ? valoriInitiale.PvMotiv : "";
            Controls.Add(_motiv);
            y += 14;

            AdaugaEticheta("Predat de:", ref y, urmeazaControl: true);
            _predatDe.SetBounds(150, y - 22, 350, 24);
            _predatDe.Text = valoriInitiale != null && !string.IsNullOrWhiteSpace(valoriInitiale.PvPredatDe)
                ? valoriInitiale.PvPredatDe : Environment.UserName;
            Controls.Add(_predatDe);
            y += 14;

            AdaugaEticheta("Memorie noua montata:", ref y, urmeazaControl: true);
            _memorieNoua.SetBounds(150, y - 22, 120, 24);
            _memorieNoua.DropDownStyle = ComboBoxStyle.DropDownList;
            _memorieNoua.Items.AddRange(new object[] { "DA", "NU" });
            _memorieNoua.SelectedItem = valoriInitiale != null && valoriInitiale.PvMemorieNoua == "NU" ? "NU" : "DA";
            Controls.Add(_memorieNoua);
            y += 14;

            AdaugaEticheta("Unitatea de service:", ref y, urmeazaControl: true);
            _unitate.SetBounds(150, y - 22, 350, 24);
            _unitate.DropDownStyle = ComboBoxStyle.DropDownList;
            _unitate.Items.AddRange(new object[] { "IT SMART RETAIL", "IT SMART SUPORT", "IT SMART A.M.E.F" });
            _unitate.SelectedItem = valoriInitiale != null && _unitate.Items.Contains(valoriInitiale.PvUnitateService)
                ? valoriInitiale.PvUnitateService : "IT SMART RETAIL";
            Controls.Add(_unitate);
            y += 14;

            AdaugaEticheta("Observatii:", ref y, urmeazaControl: true);
            _observatii.SetBounds(150, y - 22, 350, 50);
            _observatii.Multiline = true;
            _observatii.ScrollBars = ScrollBars.Vertical;
            _observatii.Text = valoriInitiale != null ? valoriInitiale.PvObservatii : "";
            Controls.Add(_observatii);
            y += 46;

            var info = new Label
            {
                Text = "Numarul de rapoarte Z, totalul vanzarilor si perioada de utilizare se iau\n" +
                       "singure din raportul memoriei fiscale, dupa predare.",
                ForeColor = SystemColors.GrayText,
                Bounds = new Rectangle(16, y, 484, 34),
            };
            Controls.Add(info);
            y += 40;

            var ok = new Button { Text = "Continua", DialogResult = DialogResult.OK, Bounds = new Rectangle(300, y, 96, 30) };
            var renunt = new Button { Text = "Renunta", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(404, y, 96, 30) };
            ok.Click += (s, e) =>
            {
                if (_genereaza.Checked && string.IsNullOrWhiteSpace(_motiv.Text))
                {
                    MessageBox.Show(this, "Completati motivul predarii — el se tipareste in document.",
                        "Proces verbal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };
            Controls.Add(ok);
            Controls.Add(renunt);
            AcceptButton = ok;
            CancelButton = renunt;

            ActualizeazaActive();
        }

        private void AdaugaEticheta(string text, ref int y, bool urmeazaControl)
        {
            var l = new Label { Text = text, Bounds = new Rectangle(16, y + 4, 130, 20) };
            Controls.Add(l);
            y += urmeazaControl ? 22 : 26;
        }

        private void ActualizeazaActive()
        {
            bool activ = _genereaza.Checked;
            _motiv.Enabled = activ;
            _predatDe.Enabled = activ;
            _memorieNoua.Enabled = activ;
            _unitate.Enabled = activ;
            _observatii.Enabled = activ;
        }

        /// <summary>Scrie in optiuni ce s-a completat.</summary>
        public void AplicaPeste(HandoverOptions opt)
        {
            if (opt == null) return;
            opt.GenereazaPv = Genereaza;
            opt.PvMotiv = Motiv;
            opt.PvPredatDe = PredatDe;
            opt.PvMemorieNoua = MemorieNoua;
            opt.PvUnitateService = UnitateService;
            opt.PvObservatii = Observatii;
        }
    }
}
