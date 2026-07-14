using System;
using System.Text;

namespace PredareAmef.Services
{
    /// <summary>
    /// DTO cu informatii complete despre aparatul fiscal (echivalent Datecs Manager info).
    /// Fiecare camp poate fi null daca CMD-ul nu raspunde — se afiseaza "—".
    /// </summary>
    public sealed class DeviceFullInfo
    {
        // === Identificare ===
        public string ModelType;         // Tip model (ex: FP-700)
        public string Name;              // Denumire (ex: FP-700)
        public string FirmwareVersion;   // 418516
        public string FirmwareDate;      // 31Jul20 1000
        public string ServiceKey;        // S.Key
        public string SerialNumber;      // DB4700019588
        public string PrintColumns;      // 48
        public string ZLeft;             // 358
        public string ZTotal;            // 2500
        public string ExpiryDate;        // Revizie expira

        // === Retea & SIM ===
        public string LocalIp;
        public string MacLan;
        public string AnafInterface;     // 1 = GPRS
        public string ModemModel;        // 2 = Modem DP 25
        public string IccidSim;
        public string ImsiSim;
        public string Apn;

        // === Fiscalizare ===
        public string VatPayer;          // DA/NU
        public string SerialFabricatie;
        public string SeriaFiscalaNUI;
        public string HeaderFirma;
        public string HeaderAdresa;
        public string TaxNumberCIF;

        // === Ultima conexiune ANAF ===
        public string AnafLastTx;
        public string AnafLastZ;
        public string AnafStatus;
        public string AnafErrorCode;

        // === Ultimul bon / Z ===
        public string LastBonNr;
        public string LastBonDate;
        public string LastZNr;
        public string LastZDate;

        /// <summary>Formatare tip Datecs Manager, monospace.</summary>
        public string ToPrettyText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("══════ IDENTIFICARE ECHIPAMENT ══════");
            sb.AppendLine("Tip model          : " + N(ModelType));
            sb.AppendLine("Denumire (Name)    : " + N(Name));
            sb.AppendLine("Versiune firmware  : " + N(FirmwareVersion) + (FirmwareDate != null ? "  (" + FirmwareDate + ")" : "")
                                                 + (ServiceKey != null ? "  │  S.Key: " + ServiceKey : ""));
            sb.AppendLine("Serie fabricatie   : " + N(SerialNumber));
            sb.AppendLine("Coloane imprimare  : " + N(PrintColumns));
            if (!string.IsNullOrEmpty(ZLeft) && !string.IsNullOrEmpty(ZTotal))
            {
                double pctUsed = 0;
                if (int.TryParse(ZLeft, out int zl) && int.TryParse(ZTotal, out int zt) && zt > 0)
                    pctUsed = (1.0 - (double)zl / zt) * 100.0;
                sb.AppendLine("Z ramase in FM     : " + ZLeft + " / " + ZTotal + "  (" + pctUsed.ToString("0.0") + "% utilizat)");
            }
            else
            {
                sb.AppendLine("Z ramase in FM     : " + N(ZLeft));
            }
            sb.AppendLine("Revizia expira la  : " + N(ExpiryDate));
            sb.AppendLine();

            sb.AppendLine("══════ RETEA & SIM ══════");
            sb.AppendLine("Adresa IP activa   : " + N(LocalIp));
            sb.AppendLine("MAC LAN            : " + N(MacLan));
            sb.AppendLine("Interfata ANAF     : " + N(AnafInterface));
            sb.AppendLine("Model Modem        : " + N(ModemModel));
            sb.AppendLine("ICCID SIM          : " + N(IccidSim));
            sb.AppendLine("IMSI SIM           : " + N(ImsiSim));
            sb.AppendLine("APN                : " + N(Apn));
            sb.AppendLine();

            sb.AppendLine("══════ INFO FISCALIZARE ══════");
            sb.AppendLine("Platitor TVA       : " + N(VatPayer));
            sb.AppendLine("Serie fabricatie   : " + N(SerialFabricatie ?? SerialNumber));
            sb.AppendLine("Seria fiscala (NUI): " + N(SeriaFiscalaNUI));
            sb.AppendLine("Firma (Header 1)   : " + N(HeaderFirma));
            sb.AppendLine("Adresa (Header 2)  : " + N(HeaderAdresa));
            sb.AppendLine("CIF (TAXnumber)    : " + N(TaxNumberCIF));
            sb.AppendLine();

            sb.AppendLine("══════ ULTIMA CONEXIUNE ANAF ══════");
            sb.AppendLine("Data transmitere   : " + N(AnafLastTx));
            sb.AppendLine("Nr. Z transmis     : " + N(AnafLastZ));
            sb.AppendLine("Raport statut      : " + N(AnafStatus));
            sb.AppendLine("Cod eroare         : " + N(AnafErrorCode));
            sb.AppendLine();

            sb.AppendLine("══════ ULTIMUL BON / RAPORT Z ══════");
            sb.AppendLine("Nr. bon fiscal     : " + N(LastBonNr));
            sb.AppendLine("Data/ora bon fisc. : " + N(LastBonDate));
            sb.AppendLine("Nr. raport Z       : " + N(LastZNr));
            sb.AppendLine("Data/ora raport Z  : " + N(LastZDate));
            return sb.ToString();
        }

        private static string N(string s) => string.IsNullOrWhiteSpace(s) ? "—" : s.Trim();
    }

    /// <summary>
    /// Interogheaza aparatul via DUDE cu CMD-uri variate si populeaza DeviceFullInfo.
    /// Best-effort — campurile care nu raspund raman null.
    /// </summary>
    public static class DeviceInfoReader
    {
        public static DeviceFullInfo Read(DudeClient dude, ILogger log)
        {
            var info = new DeviceFullInfo();
            if (dude == null) return info;

            // Identificare de baza — din proprietatile DudeClient
            info.Name = dude.ModelName;
            info.ModelType = dude.ModelName;
            info.SerialNumber = dude.SerialNumber;
            info.SerialFabricatie = dude.SerialNumber;

            // CMD 90 — Firmware version + date
            try
            {
                string o = "";
                if (dude.ExecuteCommand(90, "", ref o) == 0)
                {
                    var parts = (o ?? "").Split('\t', ',');
                    if (parts.Length >= 1) info.FirmwareVersion = SafeTrim(parts[0]);
                    if (parts.Length >= 2) info.FirmwareDate = SafeTrim(parts[1]);
                    if (parts.Length >= 3) info.ServiceKey = SafeTrim(parts[2]);
                }
            }
            catch { }

            // ReadVar255 — coloane, service expiry (nume corecte din protocol Datecs v2.10)
            info.PrintColumns = TryReadVar(dude, "PrintColumns");
            info.ExpiryDate = TryReadVar(dude, "ServiceDate");

            // CMD 68 — Number of remaining entries for Z-reports in FM
            try
            {
                string o = "";
                if (dude.ExecuteCommand(68, "", ref o) == 0)
                {
                    // Response: ErrorCode\tReportsLeft\t (returnat prin LastAnswer sau prin output)
                    var parts = (o ?? "").Split('\t', ',');
                    // Prima valoare non-zero, non-empty = ReportsLeft
                    foreach (var p in parts)
                    {
                        var v = p?.Trim();
                        if (!string.IsNullOrEmpty(v) && int.TryParse(v, out int n) && n > 0)
                        {
                            info.ZLeft = v;
                            break;
                        }
                    }
                }
            }
            catch { }
            info.ZTotal = "3650";   // constant pt majoritatea modelelor Datecs

            // Retea (nume corecte din tabelul de parametri Datecs v2.10)
            info.LocalIp = TryReadVar(dude, "LAN_IP");
            info.MacLan = TryReadVar(dude, "LanMAC");
            info.ModemModel = TryReadVar(dude, "ModemModel");
            info.IccidSim = TryReadVar(dude, "SimICCID");
            info.ImsiSim = TryReadVar(dude, "SimIMSI");
            info.Apn = TryReadVar(dude, "APN");
            info.AnafInterface = string.IsNullOrEmpty(info.ModemModel) ? null : "GPRS (modem)";

            // Fiscalizare
            info.SeriaFiscalaNUI = dude.FmNumber;
            info.HeaderFirma = dude.ReadVar255("Header", 0);
            info.HeaderAdresa = dude.ReadVar255("Header", 1);
            info.TaxNumberCIF = TryReadVar(dude, "TAXnumber");

            // Fallback CIF via CMD 123 param "1"
            if (string.IsNullOrWhiteSpace(info.TaxNumberCIF))
            {
                try
                {
                    string o = "";
                    if (dude.ExecuteCommand(123, "1\t", ref o) == 0)
                    {
                        var parts = (o ?? "").Split('\t');
                        if (parts.Length >= 6) info.TaxNumberCIF = parts[5]?.Trim();
                        if (parts.Length >= 4 && string.IsNullOrEmpty(info.HeaderFirma))
                            info.HeaderFirma = SafeTrim(parts[3]);
                    }
                }
                catch { }
            }
            // Curata prefixul "CIF:" daca exista
            if (!string.IsNullOrEmpty(info.TaxNumberCIF) && info.TaxNumberCIF.StartsWith("CIF:", StringComparison.OrdinalIgnoreCase))
                info.TaxNumberCIF = info.TaxNumberCIF.Substring(4).Trim();

            // Platitor TVA — inferat: daca TaxNumber incepe cu "RO" e cu TVA
            if (!string.IsNullOrEmpty(info.TaxNumberCIF))
                info.VatPayer = info.TaxNumberCIF.StartsWith("RO", StringComparison.OrdinalIgnoreCase) ? "DA" : "NU";

            // ANAF status via CMD 71 param "2" (Information about connection with NRA server)
            try
            {
                string o = "";
                if (dude.ExecuteCommand(71, "2\t", ref o) == 0)
                {
                    // Response: LastDate, NextDate, Zrep, ZErrnReport, ZErrCnt, ZErrStatus,
                    //           SellErrnDoc, SellErrCnt, SellErrStatus, SellNumber, SellDate,
                    //           LastErr, RemMinutes
                    var parts = (o ?? "").Split('\t');
                    if (parts.Length >= 1) info.AnafLastTx = SafeTrim(parts[0]);
                    if (parts.Length >= 3) info.AnafLastZ = SafeTrim(parts[2]);
                    if (parts.Length >= 12)
                    {
                        string err = SafeTrim(parts[11]);
                        info.AnafErrorCode = err ?? "0";
                        info.AnafStatus = (err == null || err == "0") ? "✔ TRIMIS OK" : "✗ EROARE";
                    }
                    else
                    {
                        info.AnafStatus = "✔ TRIMIS";
                        info.AnafErrorCode = "0";
                    }
                }
            }
            catch { }

            // Ultimul bon / Raport Z via CMD 123 param "3" (Last fiscal receipt)
            try
            {
                string o = "";
                if (dude.ExecuteCommand(123, "3\t", ref o) == 0)
                {
                    // Response: BonFiscal, DateBonFiscal, Znumber, Zdate
                    var parts = (o ?? "").Split('\t');
                    if (parts.Length >= 1) info.LastBonNr = SafeTrim(parts[0]);
                    if (parts.Length >= 2) info.LastBonDate = SafeTrim(parts[1]);
                    if (parts.Length >= 3) info.LastZNr = SafeTrim(parts[2]);
                    if (parts.Length >= 4) info.LastZDate = SafeTrim(parts[3]);
                }
            }
            catch { }

            log?.Log("Info aparat citit (best-effort).", LogLevel.Info);
            return info;
        }

        private static string TryReadVar(DudeClient dude, string varName)
        {
            try
            {
                string v = dude.ReadVar255(varName);
                return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
            }
            catch { return null; }
        }

        private static string SafeTrim(string s)
        {
            if (s == null) return null;
            s = s.Trim();
            return (s == "" || s == "-" || s == "?") ? null : s;
        }
    }
}
