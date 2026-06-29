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
