using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace PredareAmef.Services
{
    /// <summary>
    /// Pas 1+2 — Citire raw a memoriei fiscale via CMD 116 (service_Get_FiscalMemory) si salvare ca .bin.
    ///
    /// CMD 116 descoperit:
    ///   Input:  Operation=0 \t Address=<HEX string> \t nBytes=<decimal, max 106>
    ///   Output: ErrorCode \t HexData (2 hex chars per byte)
    ///
    /// Aceeasi comanda folosita de FMI.exe intern. Byte-perfect compatibil cu outputul FMI.
    /// Durata tipica ~10 min pentru FM de 557 KB (5250 apeluri x ~100ms fiecare).
    /// </summary>
    public sealed class FiscalMemoryReader
    {
        private const int CMD_GET_FM = 116;
        private const int CHUNK_SIZE = 106;
        private const int MAX_FM_SIZE = 2 * 1024 * 1024;
        // Dupa ce am depasit estimarea cu marja, daca vedem un "coada" mare de 0xFF consecutivi
        // (zona nescrisa din flash), oprim: asta e EOF real al datelor, chiar daca CMD 116 mai
        // returneaza date. Pragul e mai mare decat cel mai mare gap intern (8556 bytes).
        private const int TAIL_FF_THRESHOLD = 16 * 1024; // 16 KB — mult peste orice gap intern

        /// <summary>
        /// Citeste intregul continut al memoriei fiscale si salveaza ca .bin.
        /// EOF e detectat STRICT prin eroarea CMD 116 (adresa in afara FM) — NU prin euristica 0xFF,
        /// fiindca FM are gap-uri interne (antet records, VAT changes) de 1000-8000 bytes de 0xFF
        /// care NU sunt EOF.
        /// Estimeaza dimensiunea bazat pe nZreport (fiecare Z ~254 bytes + overhead fix).
        /// Raporteaza progres fin prin ILogger.SubProgress.
        /// </summary>
        public int ReadFullFiscalMemory(DudeClient dude, string savePath, int nextZ, ILogger log, CancellationToken ct = default(CancellationToken))
        {
            // Estimare total bytes: 7222 overhead + nextZ * 254 (empiric pentru FP-700)
            long estimatedTotal = 7222L + (long)Math.Max(nextZ, 1) * 254L;
            if (estimatedTotal < 100000) estimatedTotal = 100000;

            log.Log("Citire raw Memorie Fiscala (CMD 116) — estimat ~" + (estimatedTotal / 1024) + " KB...", LogLevel.Info);
            log.SubProgress(0, estimatedTotal, "Pornire citire FM");

            var buffer = new byte[MAX_FM_SIZE];
            int offset = 0;
            int consecutiveErrors = 0;
            int tailFfRun = 0;            // 0xFF consecutivi la coada
            int lastDataOffset = 0;       // ultimul offset cu byte != 0xFF (exclusiv)
            var sw = Stopwatch.StartNew();
            DateTime lastUiUpdate = DateTime.MinValue;

            while (offset < MAX_FM_SIZE)
            {
                if (ct.IsCancellationRequested)
                {
                    log.Log("  Citire FM ANULATA de utilizator la offset 0x" + offset.ToString("X") + " (" + (offset / 1024) + " KB)", LogLevel.Warning);
                    break;
                }

                int n = Math.Min(CHUNK_SIZE, MAX_FM_SIZE - offset);
                byte[] chunk = ReadChunk(dude, offset, n, out int errCode);
                if (chunk == null)
                {
                    consecutiveErrors++;
                    if (consecutiveErrors >= 3 || offset == 0)
                    {
                        if (offset > 0)
                            log.Log("  EOF la offset 0x" + offset.ToString("X") + " (CMD 116 err=" + errCode + " x3)", LogLevel.Debug);
                        else
                            log.Log("CMD 116 esuat de la start err=" + errCode + " (" + (dude.LastError ?? "") + ")", LogLevel.Error);
                        break;
                    }
                    System.Threading.Thread.Sleep(50);
                    continue;
                }
                consecutiveErrors = 0;

                Array.Copy(chunk, 0, buffer, offset, chunk.Length);

                // Track tail 0xFF run si ultim offset cu date
                for (int i = 0; i < chunk.Length; i++)
                {
                    if (chunk[i] == 0xFF) tailFfRun++;
                    else { tailFfRun = 0; lastDataOffset = offset + i + 1; }
                }

                offset += chunk.Length;

                if ((DateTime.Now - lastUiUpdate).TotalMilliseconds >= 250)
                {
                    lastUiUpdate = DateTime.Now;
                    long displayTotal = Math.Max(estimatedTotal, (long)offset + 1);
                    double secElapsed = sw.Elapsed.TotalSeconds;
                    double eta = secElapsed > 0 && offset > 0
                        ? (secElapsed * displayTotal / offset) - secElapsed
                        : 0;
                    string label = string.Format("{0} KB / ~{1} KB  |  {2} scurs  |  ETA ~{3}",
                        offset / 1024, displayTotal / 1024, FormatDuration(secElapsed), FormatDuration(eta));
                    log.SubProgress(offset, displayTotal, label);
                }

                // EOF la coada: trecut de estimare + 16 KB continuu de 0xFF
                if (offset >= estimatedTotal && tailFfRun >= TAIL_FF_THRESHOLD)
                {
                    log.Log("  EOF (tail 0xFF " + (tailFfRun / 1024) + " KB la 0x" + (offset - tailFfRun).ToString("X") + ")", LogLevel.Debug);
                    offset = lastDataOffset;
                    break;
                }
            }

            sw.Stop();
            if (offset == 0)
            {
                log.Log("Nu s-au citit date din FM.", LogLevel.Error);
                return -1;
            }

            try
            {
                byte[] final = new byte[offset];
                Array.Copy(buffer, final, offset);
                File.WriteAllBytes(savePath, final);
                log.SubProgress(offset, offset, "Complet");
                log.Log("FM .bin salvat (" + offset + " bytes = " + (offset / 1024) + " KB, " + FormatDuration(sw.Elapsed.TotalSeconds) + "): " + savePath, LogLevel.Success);
                return offset;
            }
            catch (Exception ex)
            {
                log.Log("Eroare scriere .bin: " + ex.Message, LogLevel.Error);
                return -1;
            }
        }

        /// <summary>Citire un chunk via CMD 116.</summary>
        private byte[] ReadChunk(DudeClient dude, int address, int nBytes, out int errCode)
        {
            string addrHex = address.ToString("X");
            string param = "0\t" + addrHex + "\t" + nBytes + "\t";
            string output = "";
            errCode = dude.ExecuteCommand(CMD_GET_FM, param, ref output);
            if (errCode != 0) return null;

            var parts = (output ?? "").Split('\t');
            if (parts.Length < 2 || parts[0] != "0") return null;

            string hex = parts[1].Trim();
            if ((hex.Length & 1) != 0) return null;
            if (hex.Length == 0) return new byte[0];

            int byteCount = hex.Length / 2;
            var result = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                int hi = HexVal(hex[i * 2]);
                int lo = HexVal(hex[i * 2 + 1]);
                if (hi < 0 || lo < 0) return null;
                result[i] = (byte)((hi << 4) | lo);
            }
            return result;
        }

        private static int HexVal(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return -1;
        }

        private static string FormatDuration(double seconds)
        {
            if (seconds < 60) return ((int)seconds) + "s";
            int min = (int)(seconds / 60);
            int sec = (int)(seconds - min * 60);
            return min + "m " + sec.ToString("D2") + "s";
        }
    }
}
