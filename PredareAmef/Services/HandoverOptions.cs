using System;

namespace PredareAmef.Services
{
    public enum TransportKind { Serial, Lan }

    public sealed class HandoverOptions
    {
        // Connection
        public TransportKind Transport { get; set; } = TransportKind.Serial;
        public string ComPort { get; set; } = "COM1";
        public int BaudRate { get; set; } = 115200;
        public string LanIp { get; set; } = "192.168.1.100";
        public int LanPort { get; set; } = 3999;

        // Step toggles
        public bool Step1_DumpFm { get; set; } = true;          // CMD 116 raw dump → .bin
        public bool Step3_GenerateTxt { get; set; } = true;     // parse .bin → .txt
        public bool Step5_PrintSummary { get; set; } = true;
        public bool Step6_ExportXml { get; set; } = true;
        public bool Step7_ExportHeader { get; set; } = true;

        // Export ANAF .p7b — perioada custom (overrides default "3 luni" cu pivot).
        // Daca AnafCustomDateRange = true, foloseste AnafDateFrom..AnafDateTo (luna cu luna).
        // Daca false, foloseste comportamentul vechi (pivot + 2 luni anterioare).
        public bool AnafCustomDateRange { get; set; } = false;
        public DateTime AnafDateFrom { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-2);
        public DateTime AnafDateTo { get; set; } = DateTime.Now;

        // Output
        public string OutputRoot { get; set; }
            = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public string ClientName { get; set; } = "Client";

        // Multi-sesiune: daca OutputDirOverride e setat, orchestratorul foloseste exact
        // acest folder (creat de runner-ul multi-sesiune cu format <Firma>_<Serie>).
        // In caz contrar, isi creeaza singur folderul vechi: Predare_<Client>_<timestamp>.
        public string OutputDirOverride { get; set; }

        // Pre-completate de scanner (cand vin din multi-sesiune). Daca sunt setate,
        // orchestratorul nu mai re-citeste CMD 123 pentru identitate.
        public string PrescannedSerial { get; set; }
        public string PrescannedFirma  { get; set; }
        public string PrescannedCif    { get; set; }
        public string PrescannedFmNum  { get; set; }

        // ── Proces verbal de predare (generat de CRM la finalul predarii) ──

        /// <summary>Daca la final cerem CRM-ului procesul verbal de predare a memoriei.</summary>
        public bool GenereazaPv { get; set; } = false;

        /// <summary>Motivul predarii, scris in document. Fara el nu are rost sa cerem documentul.</summary>
        public string PvMotiv { get; set; } = "";

        /// <summary>Cine preda — apare in document si in istoricul din CRM.</summary>
        public string PvPredatDe { get; set; } = "";

        /// <summary>"DA" daca s-a montat memorie noua.</summary>
        public string PvMemorieNoua { get; set; } = "DA";

        /// <summary>Unitatea de service, care alege sablonul: Retail / Suport / A.M.E.F.</summary>
        public string PvUnitateService { get; set; } = "IT SMART RETAIL";

        public string PvObservatii { get; set; } = "";

        /// <summary>Daca tiparim etichetele (memorie + plic) dupa procesul verbal.</summary>
        public bool TiparesteEtichete { get; set; } = false;

        /// <summary>Imprimanta de etichete; gol = cea implicita a calculatorului.</summary>
        public string ImprimantaEtichete { get; set; } = "";

        /// <summary>
        /// O copie a tuturor optiunilor. Se face camp cu camp de catre runtime, nu de mana:
        /// o copie scrisa de mana uita campurile adaugate mai tarziu, si asa s-au pierdut
        /// odata procesul verbal si etichetele.
        /// </summary>
        public HandoverOptions Copie()
        {
            return (HandoverOptions)MemberwiseClone();
        }
    }

    public sealed class DeviceInfo
    {
        public string SerialNumber { get; set; } = "-";
        public string FmNumber { get; set; } = "-";
        public string TaxNumber { get; set; } = "-";
        public string ClientNameHeader { get; set; } = "-";
        // CMD 255 nZreport intoarce numarul ULTIMULUI Z emis (NU al urmatorului — confirmat pe FP-700/800)
        public int NextZ { get; set; }
        public int LastEmittedZ => NextZ;
    }
}
