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
        private readonly CheckBox _etichete = new CheckBox();
        private readonly ComboBox _imprimanta = new ComboBox();

        public bool Genereaza { get { return _genereaza.Checked; } }
        public string Motiv { get { return _motiv.Text.Trim(); } }
        public string PredatDe { get { return _predatDe.Text.Trim(); } }
        public string MemorieNoua { get { return _memorieNoua.Text.Trim(); } }
        public string UnitateService { get { return _unitate.Text.Trim(); } }
        public string Observatii { get { return _observatii.Text.Trim(); } }
        public bool TiparesteEtichete { get { return _etichete.Checked; } }
        public string Imprimanta { get { return _imprimanta.Text.Trim(); } }

        public PvDialog(HandoverOptions valoriInitiale)
        {
            Text = "Proces verbal de predare";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 410);
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
                ? valoriInitiale.PvPredatDe
                : ReprezentantPentru(valoriInitiale != null ? valoriInitiale.PvUnitateService : null);
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
            // Fiecare firma de service are reprezentantul ei, acelasi care semneaza in sablonul
            // procesului verbal. Schimbarea firmei schimba si tehnicianul, dar campul ramane
            // liber: poate preda altcineva.
            _unitate.SelectedIndexChanged += (s, e) => _predatDe.Text = ReprezentantPentru(_unitate.Text);
            Controls.Add(_unitate);
            y += 14;

            AdaugaEticheta("Observatii:", ref y, urmeazaControl: true);
            _observatii.SetBounds(150, y - 22, 350, 50);
            _observatii.Multiline = true;
            _observatii.ScrollBars = ScrollBars.Vertical;
            _observatii.Text = valoriInitiale != null ? valoriInitiale.PvObservatii : "";
            Controls.Add(_observatii);
            y += 46;

            _etichete.Text = "Tipareste etichetele (memorie + plic) dupa predare";
            _etichete.SetBounds(16, y, 480, 22);
            _etichete.Checked = valoriInitiale != null && valoriInitiale.TiparesteEtichete;
            _etichete.CheckedChanged += (s, e) => ActualizeazaActive();
            Controls.Add(_etichete);
            y += 28;

            AdaugaEticheta("Imprimanta etichete:", ref y, urmeazaControl: true);
            _imprimanta.SetBounds(150, y - 22, 350, 24);
            _imprimanta.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (string nume in EtichetaPrinter.Imprimante()) _imprimanta.Items.Add(nume);
            string alesa = valoriInitiale != null && !string.IsNullOrWhiteSpace(valoriInitiale.ImprimantaEtichete)
                ? valoriInitiale.ImprimantaEtichete : EtichetaPrinter.ImprimantaImplicita();
            if (alesa != null && _imprimanta.Items.Contains(alesa)) _imprimanta.SelectedItem = alesa;
            else if (_imprimanta.Items.Count > 0) _imprimanta.SelectedIndex = 0;
            Controls.Add(_imprimanta);
            y += 20;

            var info = new Label
            {
                Text = "Numarul de rapoarte Z, totalul vanzarilor si perioada de utilizare se iau\n" +
                       "singure din raportul memoriei fiscale, dupa predare. Etichetele raman si\n" +
                       "pe disc, langa fisierele predarii.",
                ForeColor = SystemColors.GrayText,
                Bounds = new Rectangle(16, y, 484, 48),
            };
            Controls.Add(info);
            y += 54;

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

        /// <summary>
        /// Cine preda, pentru fiecare firma de service — aceleasi nume ca in sabloanele
        /// proceselor verbale din CRM, unde semneaza ca reprezentant al unitatii.
        /// </summary>
        public static string ReprezentantPentru(string unitateService)
        {
            switch ((unitateService ?? "").Trim().ToUpperInvariant())
            {
                case "IT SMART SUPORT": return "PANTURESCU MIHAELA";
                case "IT SMART A.M.E.F": return "AGRIGOROAI TEOFILIA";
                default: return "AGRIGOROAI ADRIAN";
            }
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
            _etichete.Enabled = activ;
            _imprimanta.Enabled = activ && _etichete.Checked;
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
            opt.TiparesteEtichete = TiparesteEtichete;
            opt.ImprimantaEtichete = Imprimanta;
        }
    }
}
