using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace PredareAmef.Services
{
    /// <summary>
    /// Pas 6 — Export ANAF .p7b oficial pentru luna curenta + N luni anterioare.
    ///
    /// Foloseste DUDE COM <c>download_ANAF_DTRange()</c> care produce fisiere
    /// <c>{CUI}_Z{NNNN}.p7b</c> semnate digital — METODA OFICIALA acceptata de ANAF.
    /// (Nu CMD 128 raw XML, care produce XML nesemnat, nevalid pentru raportare.)
    ///
    /// Fluxul per luna:
    ///   1. set_Download_Path(<dir/luna>)
    ///   2. DateRange_StartValue = "DD-MM-YY HH:MM:SS" (start luna)
    ///   3. DateRange_EndValue   = "DD-MM-YY HH:MM:SS" (sfarsit luna)
    ///   4. download_ANAF_DTRange()
    ///   5. Numar fisierele .p7b create
    /// </summary>
    public sealed class XmlExporter
    {
        /// <summary>
        /// Maxim cate luni inapoi cautam o luna cu date (pentru aparatele nefolosite recent).
        /// </summary>
        private const int MAX_LOOKBACK_MONTHS = 24;

        public int ExportLastNMonths(DudeClient dude, string outputDir, int monthsBack, string cif, ILogger log, CancellationToken ct = default(CancellationToken))
        {
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            int totalFiles = 0;
            var now = DateTime.Now;

            // ── Pasul 1: scan invers până găsim prima lună cu fișiere ANAF (pivot).
            // Pentru aparatele nefolosite recent, ultima activitate poate fi cu luni
            // în urmă. Începem de la luna curentă și mergem înapoi max MAX_LOOKBACK_MONTHS.
            DateTime pivotMonth = default(DateTime);
            bool foundPivot = false;

            for (int i = 0; i < MAX_LOOKBACK_MONTHS && !ct.IsCancellationRequested; i++)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd   = monthStart.AddMonths(1).AddSeconds(-1);
                string monthLabel = monthStart.ToString("yyyy-MM");
                string subDir = Path.Combine(outputDir, monthLabel);
                if (!Directory.Exists(subDir)) Directory.CreateDirectory(subDir);

                log.Log("Cautare pivot ANAF .p7b — luna " + monthLabel +
                        " (" + monthStart.ToString("dd-MM-yy") + " → " + monthEnd.ToString("dd-MM-yy") + ")",
                        LogLevel.Info);

                int saved = DownloadMonth(dude, monthStart, monthEnd, subDir, log);
                if (saved > 0)
                {
                    log.Log("  → " + saved + " fisiere .p7b pentru " + monthLabel + " (PIVOT)", LogLevel.Success);
                    pivotMonth = monthStart;
                    foundPivot = true;
                    totalFiles += saved;
                    break;
                }
                log.Log("  → 0 fisiere pentru " + monthLabel + ", caut mai inapoi...", LogLevel.Debug);
                // Curat folder gol
                try { Directory.Delete(subDir, false); } catch { }
            }

            if (!foundPivot)
            {
                log.Log("Niciun .p7b in ultimii " + MAX_LOOKBACK_MONTHS + " luni — aparat nefolosit sau memorie fiscala goala.", LogLevel.Warning);
                return 0;
            }

            // ── Pasul 2: exportă încă `monthsBack` luni anterioare pivotului.
            for (int m = 1; m <= monthsBack && !ct.IsCancellationRequested; m++)
            {
                var monthStart = pivotMonth.AddMonths(-m);
                var monthEnd   = monthStart.AddMonths(1).AddSeconds(-1);
                string monthLabel = monthStart.ToString("yyyy-MM");
                string subDir = Path.Combine(outputDir, monthLabel);
                if (!Directory.Exists(subDir)) Directory.CreateDirectory(subDir);

                log.Log("Export ANAF .p7b — luna " + monthLabel +
                        " (" + monthStart.ToString("dd-MM-yy") + " → " + monthEnd.ToString("dd-MM-yy") + ")",
                        LogLevel.Info);

                int saved = DownloadMonth(dude, monthStart, monthEnd, subDir, log);
                totalFiles += saved;

                log.Log("  → " + saved + " fisiere .p7b pentru " + monthLabel,
                        saved > 0 ? LogLevel.Success : LogLevel.Warning);

                if (saved == 0)
                {
                    try { Directory.Delete(subDir, false); } catch { }
                }
            }

            log.Log("Export ANAF total: " + totalFiles + " fisiere .p7b (pivot=" + pivotMonth.ToString("yyyy-MM") +
                    ") in " + outputDir,
                    totalFiles > 0 ? LogLevel.Success : LogLevel.Warning);
            return totalFiles;
        }

        /// <summary>
        /// Download .p7b pentru o luna intreaga via DUDE COM.
        /// DUDE auto-numeste fisierele {CUI}_Z{NNNN}.p7b in download_Path.
        /// Retry de 3 ori la eroare -100001 (I/O error) cu pauza intre tentative.
        /// </summary>
        private int DownloadMonth(DudeClient dude, DateTime start, DateTime end, string saveDir, ILogger log)
        {
            try
            {
                int filesBefore = Directory.GetFiles(saveDir, "*.p7b").Length;

                int spr = dude.SetDownloadPath(saveDir);
                if (spr != 0)
                {
                    log.Log("  set_Download_Path esuat (rc=" + spr + "): " + (dude.LastError ?? ""), LogLevel.Warning);
                    return 0;
                }

                dude.DateRangeStart = start.ToString("dd-MM-yy HH:mm:ss");
                dude.DateRangeEnd   = end.ToString("dd-MM-yy HH:mm:ss");

                int rc = 0;
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    rc = dude.DownloadAnafByDateRange();
                    if (rc == 0) break;

                    string err = dude.LastError ?? "";
                    log.Log("  download_ANAF_DTRange (attempt " + attempt + "/3) rc=" + rc + " msg='" + err + "'",
                            LogLevel.Debug);

                    // -100001 = I/O error — aparatul e blocat. Pauza si retry.
                    // Alte coduri (lipsa Z in interval, etc.) — abandonam imediat.
                    if (rc != -100001) break;
                    if (attempt < 3)
                    {
                        log.Log("  Pauza 5s inainte de retry (aparat blocat dupa multe operatii)...", LogLevel.Warning);
                        System.Threading.Thread.Sleep(5000);
                    }
                }

                int filesAfter = Directory.GetFiles(saveDir, "*.p7b").Length;
                int newFiles = filesAfter - filesBefore;

                if (newFiles > 0)
                {
                    var files = Directory.GetFiles(saveDir, "*.p7b").OrderBy(f => f).Take(3);
                    foreach (var f in files)
                        log.Log("    " + Path.GetFileName(f) + " (" + new FileInfo(f).Length + " bytes)", LogLevel.Debug);
                    if (filesAfter > 3) log.Log("    ... +" + (filesAfter - 3) + " alte fisiere", LogLevel.Debug);
                }

                return newFiles;
            }
            catch (Exception ex)
            {
                log.Log("  Exceptie download ANAF: " + ex.Message, LogLevel.Warning);
                return 0;
            }
        }
    }
}
