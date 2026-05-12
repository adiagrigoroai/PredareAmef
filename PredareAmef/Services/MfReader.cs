using System;
using System.IO;
using System.Text;
using System.Threading;

namespace PredareAmef.Services
{
    /// <summary>
    /// Pas 4 — Citire FM text pe interval Z [start..end] via CMD 95 (report_FM_ByZReports).
    /// Tip "10" = sumar la fisier, "11" = detaliat la fisier (confirmat din APK Datecs).
    /// NextLine: param "3\t\t\t", sfarsit semnalat prin err -100003.
    /// </summary>
    public sealed class MfReader
    {
        private const int CMD_FM_BY_Z = 95;
        private const int ERR_END_OF_DATA = -100003;

        public int ReadFmByZRange(DudeClient dude, int zStart, int zEnd, string savePath, bool detailed, ILogger log, CancellationToken ct = default(CancellationToken))
        {
            string type = detailed ? "11" : "10";
            log.Log("Citire FM text Z" + zStart + " -> Z" + zEnd + " (" + (detailed ? "detaliat" : "sumar") + ")", LogLevel.Info);

            string o = "";
            int r = dude.ExecuteCommand(CMD_FM_BY_Z, type + "\t" + zStart + "\t" + zEnd + "\t", ref o);
            if (r != 0)
            {
                log.Log("CMD 95 start esuat err=" + r + " (" + (dude.LastError ?? "") + ")", LogLevel.Error);
                return r;
            }

            var sb = new StringBuilder();
            int lineCount = 0;

            // NU adaugam dude.LastAnswer aici — dupa init contine doar codul de eroare ("0"),
            // nu o linie de text fiscal. Continutul real vine prin NextLine de mai jos.

            for (int i = 0; i < 500000; i++)
            {
                if (ct.IsCancellationRequested)
                {
                    log.Log("  Citire FM text ANULATA la linia " + lineCount, LogLevel.Warning);
                    break;
                }

                string lo = "";
                int rl = dude.ExecuteCommand(CMD_FM_BY_Z, "3\t\t\t", ref lo);
                if (rl == ERR_END_OF_DATA) break;
                if (rl != 0)
                {
                    log.Log("CMD 95 NextLine err=" + rl + " la linia " + (lineCount + 1), LogLevel.Warning);
                    break;
                }

                string text = "";
                var parts = (lo ?? "").Split('\t');
                if (parts.Length >= 2 && parts[0] == "0") text = parts[1];

                if (string.IsNullOrWhiteSpace(text))
                {
                    string ans = dude.LastAnswer ?? "";
                    if (!string.IsNullOrWhiteSpace(ans)) text = ans;
                }

                if (string.IsNullOrWhiteSpace(text)) break;
                sb.AppendLine(text);
                lineCount++;

                if (lineCount % 500 == 0)
                    log.Log("  citite " + lineCount + " linii...", LogLevel.Debug);
            }

            if (lineCount > 0)
            {
                File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
                log.Log("FM text salvat (" + lineCount + " linii): " + savePath, LogLevel.Success);
                return 0;
            }

            log.Log("FM text gol (0 linii)", LogLevel.Warning);
            return -1;
        }
    }
}
