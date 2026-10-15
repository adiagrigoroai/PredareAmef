using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PredareAmef.Services
{
    /// <summary>
    /// Post-procesare fisiere ANAF, ca in StatusAMEF:
    ///   1. DUKIntegrator.jar valideaza Perioada_raportare.p7b (declaratia A4200) -> PDF + raspuns.txt
    ///   2. 7za.exe arhiveaza toate .p7b -> Luna_An.zip
    ///
    /// Uneltele (duke/ + 7za.exe) se cauta intai pe PC (StatusAMEF sau FiscalArhive
    /// instalate), apoi in copia impachetata langa executabil (Tools\).
    /// </summary>
    public sealed class AnafArchiver
    {
        private static readonly string[] LuniRo = {
            "Ianuarie", "Februarie", "Martie", "Aprilie", "Mai", "Iunie",
            "Iulie", "August", "Septembrie", "Octombrie", "Noiembrie", "Decembrie"
        };

        /// <summary>Calea catre DUKIntegrator.jar, sau null daca nu e gasita nicaieri.</summary>
        public static string FindDukJar()
        {
            foreach (var baza in BazeCautare())
            {
                string jar = Path.Combine(baza, "duke", "DUKIntegrator.jar");
                if (File.Exists(jar)) return jar;
            }
            return null;
        }

        /// <summary>Calea catre 7za.exe, sau null daca nu e gasita nicaieri.</summary>
        public static string Find7za()
        {
            foreach (var baza in BazeCautare())
            {
                string z = Path.Combine(baza, "7za.exe");
                if (File.Exists(z)) return z;
            }
            return null;
        }

        /// <summary>
        /// Locuri unde pot sta duke/ si 7za.exe, in ordine: intai pe PC (StatusAMEF /
        /// FiscalArhive instalate), apoi copia impachetata langa executabil.
        /// </summary>
        private static System.Collections.Generic.IEnumerable<string> BazeCautare()
        {
            string pf   = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string exe  = AppDomain.CurrentDomain.BaseDirectory;

            // 1. Pe PC-ul tehnicianului
            yield return Path.Combine(pf86, "FiscalArhive");
            yield return Path.Combine(pf,   "ITSmart", "StatusAMEF");
            yield return Path.Combine(pf86, "ITSmart", "StatusAMEF");
            // 2. Impachetat cu PredareAmef
            yield return Path.Combine(exe, "Tools");
            yield return exe;
        }

        /// <summary>
        /// Ruleaza validarea (PDF) si arhivarea (ZIP) pe un folder cu .p7b.
        /// Best-effort: daca lipseste Java sau uneltele, .p7b raman oricum.
        /// </summary>
        public void PostProcess(string folder, DateTime luna, ILogger log)
        {
            ValidateAndCreatePdf(folder, log);
            CreateZip(folder, luna, log);
        }

        /// <summary>
        /// Genereaza PDF din Perioada_raportare.p7b cu DUKIntegrator.jar (profil A4200).
        /// Rezultat: {folder}\Perioada_raportare.p7b.pdf + raspuns.txt
        ///
        /// IMPORTANT: DUKIntegrator NU suporta spatii in cale (arunca
        /// "MalformedURLException: unknown protocol: c"). De aceea validarea ruleaza
        /// intr-un folder temporar cu cale scurta 8.3 (fara spatii), iar PDF-ul +
        /// raspuns.txt se copiaza inapoi in folderul real.
        /// </summary>
        public bool ValidateAndCreatePdf(string folder, ILogger log)
        {
            string dukJar = FindDukJar();
            if (dukJar == null)
            {
                log.Log("DUKIntegrator.jar negasit (nici instalat, nici impachetat) — fara PDF A4200.", LogLevel.Warning);
                return false;
            }

            string srcPerioada = Path.Combine(folder, "Perioada_raportare.p7b");
            if (!File.Exists(srcPerioada))
            {
                log.Log("Perioada_raportare.p7b lipseste — fara PDF A4200.", LogLevel.Warning);
                return false;
            }

            string resultFile = Path.Combine(folder, "raspuns.txt");
            string pdfFile = srcPerioada + ".pdf";
            try { if (File.Exists(resultFile)) File.Delete(resultFile); } catch { }
            try { if (File.Exists(pdfFile)) File.Delete(pdfFile); } catch { }

            // Folder de lucru temporar, garantat fara spatii (cale scurta 8.3).
            string tempRoot = CaleScurta(Path.GetTempPath());
            string work = Path.Combine(tempRoot, "amefduk_" + Guid.NewGuid().ToString("N").Substring(0, 8));

            try
            {
                Directory.CreateDirectory(work);
                // DUKIntegrator are nevoie de TOATE .p7b (zilele fiscale + declaratia)
                // in acelasi folder ca sa valideze A4200.
                foreach (var f in Directory.GetFiles(folder, "*.p7b"))
                    File.Copy(f, Path.Combine(work, Path.GetFileName(f)), true);

                string wInput = Path.Combine(work, "Perioada_raportare.p7b");
                string wResult = Path.Combine(work, "raspuns.txt");
                string wPdf = wInput + ".pdf";

                if (ContineSpatiu(wInput))
                {
                    log.Log("Nu am putut obtine o cale fara spatii pentru validare — fara PDF A4200.", LogLevel.Warning);
                    return false;
                }

                log.Log("DUKIntegrator: validare A4200 + PDF...", LogLevel.Info);
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = string.Format("-jar \"{0}\" -p A4200 \"{1}\" \"{2}\"", dukJar, wInput, wResult),
                    WorkingDirectory = Path.GetDirectoryName(dukJar),
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = System.Diagnostics.Process.Start(psi))
                {
                    proc.WaitForExit(60000);
                    if (!proc.HasExited)
                    {
                        try { proc.Kill(); } catch { }
                        log.Log("DUKIntegrator: timeout (60s)", LogLevel.Error);
                        return false;
                    }
                }

                // Adu rezultatele in folderul real.
                try { if (File.Exists(wResult)) File.Copy(wResult, resultFile, true); } catch { }
                try { if (File.Exists(wPdf)) File.Copy(wPdf, pdfFile, true); } catch { }

                if (File.Exists(wResult))
                {
                    string[] lines = File.ReadAllLines(wResult, Encoding.ASCII);
                    bool ok = false;
                    foreach (string line in lines)
                        if (!string.IsNullOrWhiteSpace(line) && line.Trim() == "ok") { ok = true; break; }

                    if (ok && File.Exists(pdfFile))
                    {
                        log.Log("PDF A4200 generat: " + Path.GetFileName(pdfFile), LogLevel.Success);
                        return true;
                    }
                    string raspuns = lines.Length > 0 ? string.Join("; ", lines) : "(gol)";
                    log.Log("DUKIntegrator raspuns: " + raspuns, LogLevel.Warning);
                    return false;
                }
                log.Log("DUKIntegrator: raspuns.txt nu a fost creat (Java instalat?)", LogLevel.Warning);
                return false;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                log.Log("Java nu este instalat — nu se poate genera PDF-ul A4200 (.p7b raman oricum).", LogLevel.Warning);
                return false;
            }
            catch (Exception ex)
            {
                log.Log("DUKIntegrator eroare: " + ex.Message, LogLevel.Error);
                return false;
            }
            finally
            {
                try { if (Directory.Exists(work)) Directory.Delete(work, true); } catch { }
            }
        }

        private static bool ContineSpatiu(string s) { return !string.IsNullOrEmpty(s) && s.IndexOf(' ') >= 0; }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer);

        /// <summary>Calea 8.3 (fara spatii) a unui folder existent; cea originala daca nu se poate.</summary>
        private static string CaleScurta(string cale)
        {
            try
            {
                var sb = new StringBuilder(300);
                int n = GetShortPathName(cale, sb, sb.Capacity);
                if (n > 0 && n < sb.Capacity) return sb.ToString();
            }
            catch { }
            return cale;
        }

        /// <summary>Arhiveaza toate .p7b din folder intr-un ZIP cu nume Luna_An.zip.</summary>
        public bool CreateZip(string folder, DateTime luna, ILogger log)
        {
            string sevenZip = Find7za();
            if (sevenZip == null)
            {
                log.Log("7za.exe negasit — fara arhiva ZIP.", LogLevel.Warning);
                return false;
            }

            string zipName = NumeZip(luna);
            string zipPath = Path.Combine(folder, zipName);
            string p7bPattern = Path.Combine(folder, "*.p7b");
            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }

            try
            {
                log.Log("Creare arhiva: " + zipName, LogLevel.Info);
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = sevenZip,
                    Arguments = string.Format("a -tzip \"{0}\" \"{1}\" -mx=9", zipPath, p7bPattern),
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using (var proc = System.Diagnostics.Process.Start(psi))
                {
                    proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(120000);
                    if (!proc.HasExited)
                    {
                        try { proc.Kill(); } catch { }
                        log.Log("7za.exe: timeout (120s)", LogLevel.Error);
                        return false;
                    }
                }

                if (File.Exists(zipPath))
                {
                    log.Log("ZIP creat: " + zipName + " (" + new FileInfo(zipPath).Length + " bytes)", LogLevel.Success);
                    return true;
                }
                log.Log("ZIP nu a fost creat", LogLevel.Error);
                return false;
            }
            catch (Exception ex)
            {
                log.Log("ZIP eroare: " + ex.Message, LogLevel.Error);
                return false;
            }
        }

        /// <summary>Numele arhivei: Luna_An.zip (ex: Septembrie_2026.zip).</summary>
        public static string NumeZip(DateTime data)
        {
            int idx = data.Month - 1;
            if (idx < 0 || idx >= LuniRo.Length) idx = 0;
            return LuniRo[idx] + "_" + data.Year + ".zip";
        }
    }
}
