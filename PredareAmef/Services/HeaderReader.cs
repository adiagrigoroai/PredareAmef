using System;
using System.IO;
using System.Text;

namespace PredareAmef.Services
{
    /// <summary>
    /// Pas 7 — Citire antet bon (liniile 0-8, max 9 linii) via CMD 255 "Header" cu index.
    /// Salveaza un .txt cu continutul antetului.
    /// </summary>
    public sealed class HeaderReader
    {
        public int ReadAndSaveHeader(DudeClient dude, string savePath, ILogger log)
        {
            log.Log("Citire antet aparat...", LogLevel.Info);

            var sb = new StringBuilder();
            sb.AppendLine("=== ANTET AMEF ===");
            sb.AppendLine("Generat: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Serial: " + (dude.SerialNumber ?? "-"));
            sb.AppendLine("FM nr: " + (dude.FmNumber ?? "-"));
            sb.AppendLine("Model: " + (dude.ModelName ?? "-"));

            // CIF (TaxNumber) — citit via CMD 255 var "TaxNumber"
            string cif = null;
            try
            {
                cif = dude.ReadVar255("TaxNumber");
                if (string.IsNullOrWhiteSpace(cif))
                {
                    // Fallback: CMD 123 param "1" — al 6-lea tab-field contine CIF/TaxNumber
                    string o = "";
                    if (dude.ExecuteCommand(123, "1\t", ref o) == 0)
                    {
                        var parts = (o ?? "").Split('\t');
                        if (parts.Length >= 6)
                        {
                            cif = parts[5]?.Trim();
                            if (cif != null && cif.StartsWith("CIF:", StringComparison.OrdinalIgnoreCase))
                                cif = cif.Substring(4).Trim();
                        }
                    }
                }
            }
            catch { }
            sb.AppendLine("CIF: " + (string.IsNullOrWhiteSpace(cif) ? "-" : cif));
            sb.AppendLine();
            sb.AppendLine("--- LINII ANTET ---");

            int linesFound = 0;
            for (int i = 0; i < 9; i++)
            {
                string line = dude.ReadVar255("Header", i);
                if (line == null)
                {
                    sb.AppendLine(string.Format("Linia {0}: <eroare citire>", i + 1));
                    continue;
                }
                sb.AppendLine(string.Format("Linia {0}: {1}", i + 1, line));
                if (!string.IsNullOrWhiteSpace(line)) linesFound++;
            }

            sb.AppendLine();
            sb.AppendLine("--- LINII SUBSOL ---");
            for (int i = 0; i < 9; i++)
            {
                string line = dude.ReadVar255("Footer", i);
                if (line == null) continue;
                if (string.IsNullOrWhiteSpace(line)) continue;
                sb.AppendLine(string.Format("Linia {0}: {1}", i + 1, line));
            }

            try
            {
                File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
                log.Log("Antet salvat (" + linesFound + " linii): " + savePath, LogLevel.Success);
                return 0;
            }
            catch (Exception ex)
            {
                log.Log("Eroare scriere antet: " + ex.Message, LogLevel.Error);
                return -1;
            }
        }
    }
}
