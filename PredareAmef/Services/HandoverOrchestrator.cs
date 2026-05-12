using System;
using System.IO;
using System.Threading;

namespace PredareAmef.Services
{
    /// <summary>
    /// Orchestreaza fluxul complet de predare memorie fiscala — 100% nativ, fara FMI.exe.
    ///
    /// Pasi:
    ///   0. Conectare aparat + citire identitate (serial, CIF, Z curent)
    ///   1+2. Citire memorie fiscala raw (CMD 116) → salvare .bin
    ///   3. Generare .txt (interpretare .bin sau fallback pe CMD 95 sumar pe tot intervalul)
    ///   4. Citire FM text Z1 -> ultim Z (CMD 95 tip 10, salvare .txt)
    ///   5. Printare sumar pe aparat Z1 -> ultim Z (CMD 95 tip 0)
    ///   6. Export XML luna curenta + 2 anterioare (CMD 128)
    ///   7. Citire antet + salvare .txt (CMD 255 Header)
    /// </summary>
    public sealed class HandoverOrchestrator
    {
        private const int TOTAL_STEPS = 6;

        public string OutputDir { get; private set; }

        /// <summary>Numar fisiere .p7b descarcate la Pas 5 (ANAF). 0 = ANAF esuat.</summary>
        public int LastAnafFileCount { get; private set; } = -1;

        public bool Run(HandoverOptions opt, ILogger log) => Run(opt, log, CancellationToken.None);

        public bool Run(HandoverOptions opt, ILogger log, CancellationToken ct)
        {
            if (opt == null) throw new ArgumentNullException(nameof(opt));

            if (!string.IsNullOrWhiteSpace(opt.OutputDirOverride))
            {
                OutputDir = opt.OutputDirOverride;
            }
            else
            {
                string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string clientTag = SafeName(opt.ClientName);
                OutputDir = Path.Combine(opt.OutputRoot, "Predare_" + clientTag + "_" + ts);
            }
            Directory.CreateDirectory(OutputDir);

            // Atasez file logger persistent (daca logger-ul suporta)
            if (log is ActionLogger al)
            {
                string logPath = Path.Combine(OutputDir, "predare.log");
                al.AttachFile(logPath);
                log.Log("Log file: " + logPath, LogLevel.Debug);
            }

            log.Log("Output: " + OutputDir, LogLevel.Info);

            // ── Pas 0: Conectare + identitate ──
            log.Progress(0, TOTAL_STEPS, "Conectare aparat");
            DudeClient dude = OpenConnection(opt, log);
            try
            {
                if (dude == null) return false;

                var info = ReadDeviceInfo(dude, log);
                if (info == null) return false;

                if (info.LastEmittedZ <= 0)
                {
                    log.Log("Aparatul nu are Z-uri emise (nZreport=" + info.NextZ + ").", LogLevel.Error);
                    return false;
                }

                log.Log("Aparat: " + info.SerialNumber + " | CIF: " + info.TaxNumber + " | Z 1 → " + info.LastEmittedZ, LogLevel.Success);

                string serialTag = SafeName(info.SerialNumber);

                // ── Pas 1+2: Citire raw FM via CMD 116 → .bin ──
                string binPath = Path.Combine(OutputDir, serialTag + ".bin");
                if (opt.Step1_DumpFm && !ct.IsCancellationRequested)
                {
                    log.Progress(1, TOTAL_STEPS, "Citire memorie fiscala (.bin)");
                    var reader = new FiscalMemoryReader();
                    int bytes = reader.ReadFullFiscalMemory(dude, binPath, info.NextZ, log, ct);
                    if (bytes <= 0)
                        log.Log("Dump FM nereusit — continui cu ceilalti pasi.", LogLevel.Warning);

                    // Pe unele firmware-uri (FP-800) nZreport raporteaza last-1. Numaram in .bin
                    // sa fim siguri ca prindem ultimul Z real. NU folosim probare device-side
                    // (CMD 95 cu range invalid corupe starea pentru ANAF).
                    // ATENTIE: pe FP-700 structura .bin e diferita — counter-ul poate raporta
                    // valori complet false (ex: 3064 cand real e 2497). Acceptam doar +1
                    // (bug-ul confirmat FP-800 off-by-one); diferente mai mari = counter broken.
                    int realLast = CountZRecordsInBin(binPath);
                    int delta = realLast - info.NextZ;
                    if (delta == 1)
                    {
                        log.Log(".bin contine " + realLast + " Z reports; nZreport raporta " + info.NextZ +
                                " (off-by-one FP-800). Folosesc " + realLast + ".", LogLevel.Warning);
                        info.NextZ = realLast;
                    }
                    else if (delta > 1)
                    {
                        log.Log(".bin counter raporteaza " + realLast + " dar nZreport=" + info.NextZ +
                                " (delta=" + delta + " — counter broken pe acest model). " +
                                "Folosesc nZreport=" + info.NextZ + ".", LogLevel.Warning);
                    }
                }

                // ── Pas 3: Generare .txt SUMAR (CMD 95 tip 10) — Z1 → ultim Z ──
                if (opt.Step3_GenerateTxt && !ct.IsCancellationRequested)
                {
                    log.Progress(3, TOTAL_STEPS, "Citire FM text sumar Z1 → Z" + info.LastEmittedZ);
                    string txtPath = Path.Combine(OutputDir, serialTag + "_FM_Z1-Z" + info.LastEmittedZ + ".txt");
                    var reader = new MfReader();
                    reader.ReadFmByZRange(dude, 1, info.LastEmittedZ, txtPath, detailed: false, log: log, ct: ct);
                }

                // ── Pas 4: Printare sumar pe aparat ──
                if (opt.Step5_PrintSummary && !ct.IsCancellationRequested)
                {
                    log.Progress(4, TOTAL_STEPS, "Printare sumar pe aparat Z1 → Z" + info.LastEmittedZ);
                    var printer = new MfPrinter();
                    try
                    {
                        printer.PrintFmSummaryByZRange(dude, 1, info.LastEmittedZ, log);
                    }
                    catch (Exception ex)
                    {
                        // RPC_E_DISCONNECTED (0x800706BE) la print lung pe aparate cu multe Z-uri,
                        // hartie aproape de capat, sau timeout COM. Continuam cu pasii urmatori.
                        log.Log("Printare ESUATA: " + ex.Message + " — continui cu export ANAF.", LogLevel.Warning);
                        // Daca DUDE a deconectat, reconectam pentru pasii urmatori (export ANAF + antet)
                        try { dude.Dispose(); } catch { }
                        dude = OpenConnection(opt, log);
                        if (dude == null)
                        {
                            log.Log("Reconectare esuata — opresc fluxul.", LogLevel.Error);
                            return false;
                        }
                        log.Log("Reconectat dupa eroare printare. Continui.", LogLevel.Success);
                    }
                }

                // ── Pas 5: Export ANAF .p7b oficial pentru luna curenta + 2 anterioare ──
                if (opt.Step6_ExportXml && !ct.IsCancellationRequested)
                {
                    log.Progress(5, TOTAL_STEPS, "Export ANAF .p7b luna curenta + 2 anterioare");
                    string anafDir = Path.Combine(OutputDir, "ANAF_p7b");
                    var xml = new XmlExporter();
                    LastAnafFileCount = xml.ExportLastNMonths(dude, anafDir, monthsBack: 2, cif: SafeName(info.TaxNumber), log: log, ct: ct);
                }

                // ── Pas 6: Antet → .txt ──
                if (opt.Step7_ExportHeader && !ct.IsCancellationRequested)
                {
                    log.Progress(6, TOTAL_STEPS, "Citire antet → .txt");
                    string hdrPath = Path.Combine(OutputDir, serialTag + "_antet.txt");
                    var hdr = new HeaderReader();
                    hdr.ReadAndSaveHeader(dude, hdrPath, log);
                }

                if (ct.IsCancellationRequested)
                {
                    log.Progress(TOTAL_STEPS, TOTAL_STEPS, "Anulat de utilizator");
                    log.Log("=== PREDARE ANULATA ===", LogLevel.Warning);
                    log.Log("Output partial: " + OutputDir, LogLevel.Warning);
                    return false;
                }

                log.Progress(TOTAL_STEPS, TOTAL_STEPS, "Predare finalizata");
                log.Log("=== PREDARE FINALIZATA ===", LogLevel.Success);
                log.Log("Output: " + OutputDir, LogLevel.Success);
                return true;
            }
            finally
            {
                try { dude?.Dispose(); } catch { }
            }
        }

        /// <summary>
        /// Reia DOAR Pas 5 (ANAF) si Pas 6 (Antet) pe un aparat.
        /// Folosit dupa reboot fizic al aparatului care a esuat anterior la ANAF.
        /// Foloseste OutputDirOverride pentru a scrie in folderul existent al sesiunii.
        /// </summary>
        public bool RunAnafOnly(HandoverOptions opt, ILogger log, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(opt.OutputDirOverride))
                throw new InvalidOperationException("RunAnafOnly necesita OutputDirOverride setat.");
            OutputDir = opt.OutputDirOverride;
            Directory.CreateDirectory(OutputDir);

            if (log is ActionLogger al)
                al.AttachFile(Path.Combine(OutputDir, "predare.log"));

            log.Log("=== RELUARE ANAF (dupa reboot aparat) ===", LogLevel.Info);
            log.Progress(0, 2, "Reconectare aparat");

            using (var dude = OpenConnection(opt, log))
            {
                if (dude == null) return false;

                var info = ReadDeviceInfo(dude, log);
                if (info == null) return false;

                string serialTag = SafeName(info.SerialNumber);

                // Pas 5 — ANAF
                if (!ct.IsCancellationRequested)
                {
                    log.Progress(1, 2, "Export ANAF .p7b luna curenta + 2 anterioare");
                    string anafDir = Path.Combine(OutputDir, "ANAF_p7b");
                    var xml = new XmlExporter();
                    LastAnafFileCount = xml.ExportLastNMonths(dude, anafDir, monthsBack: 2, cif: SafeName(info.TaxNumber), log: log, ct: ct);
                }

                // Pas 6 — antet (re-scriem si fisierul de antet ca sa fie consistenta)
                if (!ct.IsCancellationRequested)
                {
                    log.Progress(2, 2, "Citire antet → .txt");
                    string hdrPath = Path.Combine(OutputDir, serialTag + "_antet.txt");
                    var hdr = new HeaderReader();
                    hdr.ReadAndSaveHeader(dude, hdrPath, log);
                }

                bool ok = LastAnafFileCount > 0;
                log.Log(ok
                    ? "=== RELUARE ANAF FINALIZATA (" + LastAnafFileCount + " fisiere) ==="
                    : "=== RELUARE ANAF ESUATA (0 fisiere) — verifica reboot aparat ===",
                    ok ? LogLevel.Success : LogLevel.Warning);
                return ok;
            }
        }

        private DudeClient OpenConnection(HandoverOptions opt, ILogger log)
        {
            string err;
            DudeClient dude = opt.Transport == TransportKind.Serial
                ? DudeClient.OpenSerial(opt.ComPort, opt.BaudRate, out err, log)
                : DudeClient.OpenLan(opt.LanIp, opt.LanPort, out err);

            if (dude == null)
            {
                log.Log("Conectare esuata: " + (err ?? "necunoscut"), LogLevel.Error);
                return null;
            }

            string tag = opt.Transport == TransportKind.Serial
                ? opt.ComPort + " @ " + opt.BaudRate
                : opt.LanIp + ":" + opt.LanPort;
            log.Log("Conectat (" + tag + ") | Model: " + (dude.ModelName ?? "-"), LogLevel.Success);
            return dude;
        }

        private DeviceInfo ReadDeviceInfo(DudeClient dude, ILogger log)
        {
            var info = new DeviceInfo();
            string o = "";

            int r = dude.ExecuteCommand(123, "1\t", ref o);
            if (r == 0)
            {
                var f = (o ?? "").Split('\t');
                if (f.Length >= 6)
                {
                    info.SerialNumber = NullIfDash(f[1]) ?? dude.SerialNumber ?? "-";
                    info.ClientNameHeader = NullIfDash(f[3]) ?? "-";
                    info.FmNumber = NullIfDash(f[4]) ?? dude.FmNumber ?? "-";
                    info.TaxNumber = NullIfDash(f[5]) ?? "-";
                }
            }
            else
            {
                log.Log("CMD 123 esuat err=" + r + " — fallback", LogLevel.Warning);
                info.SerialNumber = dude.SerialNumber ?? "-";
                info.FmNumber = dude.FmNumber ?? "-";
            }

            string next = dude.ReadVar255("nZreport");
            if (!int.TryParse(next, out int nz))
            {
                log.Log("Nu s-a putut citi nZreport.", LogLevel.Error);
                return null;
            }

            info.NextZ = nz;
            return info;
        }

        /// <summary>
        /// Numara record-urile Z dintr-un dump .bin. Z records incep la offset 0x3D44,
        /// 176 bytes fiecare, dens impachetate. Slot gol = primii 8 bytes 0xFF.
        /// Stop dupa 50 sloturi goale consecutive (safety pentru padding final).
        /// </summary>
        private static int CountZRecordsInBin(string binPath)
        {
            if (!File.Exists(binPath)) return 0;
            const int Z_START = 0x3D44;
            const int Z_SIZE  = 176;

            byte[] data;
            try { data = File.ReadAllBytes(binPath); }
            catch { return 0; }

            if (data.Length < Z_START + Z_SIZE) return 0;

            int count = 0;
            int emptyStreak = 0;
            for (int off = Z_START; off + Z_SIZE <= data.Length; off += Z_SIZE)
            {
                bool isEmpty = true;
                for (int i = 0; i < 8; i++)
                {
                    if (data[off + i] != 0xFF) { isEmpty = false; break; }
                }
                if (isEmpty)
                {
                    emptyStreak++;
                    if (emptyStreak >= 50) break;
                }
                else
                {
                    count++;
                    emptyStreak = 0;
                }
            }
            return count;
        }

        private static string NullIfDash(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            return (s == "-" || s == "?") ? null : s;
        }

        private static string SafeName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "UNKNOWN";
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Trim();
        }
    }
}
