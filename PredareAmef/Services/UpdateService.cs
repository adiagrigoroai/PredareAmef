using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace PredareAmef.Services
{
    /// <summary>
    /// Auto-update via GitHub Releases.
    /// Verifica /repos/{owner}/{repo}/releases/latest si descarca asset-ul PredareAmef.exe.
    /// Pentru repo privat foloseste token-ul din config (GitHubToken).
    /// </summary>
    public static class UpdateService
    {
        // ── Config (poate fi suprascris din App.config) ────────────────────────
        public const string OWNER = "adiagrigoroai";
        public const string REPO  = "PredareAmef";
        public const string ASSET_NAME = "PredareAmef.exe";
        public const string USER_AGENT = "PredareAmef-Updater";

        public class UpdateInfo
        {
            public Version CurrentVersion { get; set; }
            public Version LatestVersion { get; set; }
            public string TagName { get; set; }
            public string ReleaseName { get; set; }
            public string ReleaseNotes { get; set; }
            public DateTime PublishedAt { get; set; }
            public string AssetDownloadUrl { get; set; }
            public long AssetSize { get; set; }
            public bool HasUpdate { get { return LatestVersion != null && CurrentVersion != null && LatestVersion > CurrentVersion; } }

            /// <summary>Daca am apucat sa intrebam GitHub si am inteles raspunsul.</summary>
            public bool Verificat { get; set; }

            /// <summary>De ce nu s-a putut verifica, ca sa nu spunem ca e totul in regula.</summary>
            public string Eroare { get; set; }
        }

        /// <summary>
        /// Cere raspunsul de la GitHub. Daca avem un jeton si el nu mai e bun, GitHub
        /// raspunde 401 si pana acum verificarea se oprea acolo: pe calculatoarele unde
        /// ramasese un jeton expirat in config nu se mai vedea nicio versiune noua.
        /// Depozitul e public, asa ca mai incercam o data fara jeton.
        /// </summary>
        private static string CereCuSauFaraJeton(WebClient wc, string url, string jeton)
        {
            if (!string.IsNullOrWhiteSpace(jeton))
            {
                try
                {
                    wc.Headers["Authorization"] = "Bearer " + jeton;
                    return wc.DownloadString(url);
                }
                catch (WebException ex)
                {
                    Trace.WriteLine("UpdateService: jetonul nu merge (" + ex.Message + "), incerc fara el.");
                }
            }
            // Pentru a doua incercare facem un client nou: cel dinainte a ramas cu
            // antetul de autorizare in el si GitHub raspunde tot cu refuz.
            using (var curat = new WebClient())
            {
                curat.Headers.Add("User-Agent", USER_AGENT);
                curat.Headers.Add("Accept", "application/vnd.github+json");
                return curat.DownloadString(url);
            }
        }

        public static Version GetCurrentVersion()
        {
            try { return Assembly.GetExecutingAssembly().GetName().Version; }
            catch { return new Version(0, 0, 0, 0); }
        }

        // ── Anti-bucla: stocheaza ultima tentativa de update intr-un fisier
        // in %APPDATA%\PredareAmef\ (sigur scrieabil, evita UAC/Desktop blocking).
        // Indiferent daca user apasa Update sau Mai tarziu, versiunea e marcata "tried"
        // pentru 6 ore, ca sa evitam bucle de update cand copy-ul esueaza.
        private static string StateDir
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dir = Path.Combine(appData, "PredareAmef");
                if (!Directory.Exists(dir))
                {
                    try { Directory.CreateDirectory(dir); } catch { }
                }
                return dir;
            }
        }

        private static string StateFile { get { return Path.Combine(StateDir, "update_state.txt"); } }

        /// <summary>Fisierul de log al actualizarilor (%APPDATA%\PredareAmef\update.log).</summary>
        public static string LogPath { get { return Path.Combine(StateDir, "update.log"); } }

        /// <summary>Scrie o linie in logul de update (best-effort).</summary>
        public static void Log(string mesaj)
        {
            try
            {
                File.AppendAllText(LogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + mesaj + Environment.NewLine);
                // nu lasam logul sa creasca la infinit
                var fi = new FileInfo(LogPath);
                if (fi.Exists && fi.Length > 200 * 1024)
                {
                    var linii = File.ReadAllLines(LogPath);
                    File.WriteAllLines(LogPath, linii.Length > 400 ? Skip(linii, linii.Length - 400) : linii);
                }
            }
            catch { }
        }

        private static string[] Skip(string[] a, int n)
        {
            if (n <= 0) return a;
            var r = new string[a.Length - n];
            Array.Copy(a, n, r, 0, r.Length);
            return r;
        }

        /// <summary>Aplicatia ruleaza dintr-un folder OneDrive (unde update-ul in loc e nesigur)?</summary>
        public static bool RuleazaDinOneDrive()
        {
            try
            {
                string exe = Assembly.GetExecutingAssembly().Location ?? "";
                return exe.IndexOf("OneDrive", StringComparison.OrdinalIgnoreCase) >= 0
                    || exe.IndexOf("\\Dropbox\\", StringComparison.OrdinalIgnoreCase) >= 0
                    || exe.IndexOf("\\Google Drive", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }

        public static void MarkVersionIgnored(string tagName)
        {
            try
            {
                File.WriteAllText(StateFile, tagName + "|" + DateTime.UtcNow.ToString("o"));
            }
            catch { /* best-effort */ }
        }

        /// <summary>Marcheaza versiunea ca tentativa de update — pentru anti-bucla pe 6h.</summary>
        public static void MarkVersionTried(string tagName)
        {
            try
            {
                File.WriteAllText(StateFile, tagName + "|" + DateTime.UtcNow.ToString("o"));
            }
            catch { /* best-effort */ }
        }

        /// <summary>Ultima versiune la care s-a incercat update (tag + cand), sau false.</summary>
        public static bool UltimaTentativa(out string tag, out DateTime cand)
        {
            tag = null; cand = DateTime.MinValue;
            try
            {
                if (!File.Exists(StateFile)) return false;
                var parts = File.ReadAllText(StateFile).Trim().Split('|');
                if (parts.Length != 2) return false;
                DateTime ts;
                if (!DateTime.TryParse(parts[1], null, System.Globalization.DateTimeStyles.RoundtripKind, out ts)) return false;
                tag = parts[0]; cand = ts;
                return true;
            }
            catch { return false; }
        }

        public static bool WasVersionRecentlyIgnored(string tagName)
        {
            try
            {
                if (!File.Exists(StateFile)) return false;
                string s = File.ReadAllText(StateFile).Trim();
                var parts = s.Split('|');
                if (parts.Length != 2) return false;
                if (!string.Equals(parts[0], tagName, StringComparison.OrdinalIgnoreCase)) return false;
                DateTime ts;
                if (!DateTime.TryParse(parts[1], null, System.Globalization.DateTimeStyles.RoundtripKind, out ts)) return false;
                // Skip prompt timp de 6h pentru aceeasi versiune (ignorata, esuata sau in progres)
                return (DateTime.UtcNow - ts).TotalHours < 6;
            }
            catch { return false; }
        }

        private static string GetString(Dictionary<string, object> d, string key)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v) && v != null) return v.ToString();
            return "";
        }

        /// <summary>
        /// Verifica daca exista versiune noua pe GitHub. Returneaza UpdateInfo (HasUpdate true/false).
        /// </summary>
        public static UpdateInfo CheckForUpdate(string githubToken)
        {
            var info = new UpdateInfo { CurrentVersion = GetCurrentVersion() };
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string url = "https://api.github.com/repos/" + OWNER + "/" + REPO + "/releases/latest";
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", USER_AGENT);
                    wc.Headers.Add("Accept", "application/vnd.github+json");
                    string json = CereCuSauFaraJeton(wc, url, githubToken);
                    var ser = new JavaScriptSerializer { MaxJsonLength = 10 * 1024 * 1024 };
                    var jo = ser.Deserialize<Dictionary<string, object>>(json);
                    info.TagName = GetString(jo, "tag_name");
                    info.ReleaseName = GetString(jo, "name");
                    info.ReleaseNotes = GetString(jo, "body");
                    DateTime pubAt;
                    DateTime.TryParse(GetString(jo, "published_at"), out pubAt);
                    info.PublishedAt = pubAt;

                    string ver = info.TagName.TrimStart('v', 'V');
                    Version v;
                    if (Version.TryParse(ver, out v))
                    {
                        info.LatestVersion = new Version(
                            v.Major, v.Minor,
                            v.Build < 0 ? 0 : v.Build,
                            v.Revision < 0 ? 0 : v.Revision);
                    }

                    info.Verificat = info.LatestVersion != null;

                    // Cauta asset-ul PredareAmef.exe
                    object assetsObj;
                    if (jo.TryGetValue("assets", out assetsObj) && assetsObj is System.Collections.ArrayList al)
                    {
                        foreach (var item in al)
                        {
                            var a = item as Dictionary<string, object>;
                            if (a == null) continue;
                            string name = GetString(a, "name");
                            if (name.Equals(ASSET_NAME, StringComparison.OrdinalIgnoreCase))
                            {
                                info.AssetDownloadUrl = GetString(a, "url");
                                long sz; long.TryParse(GetString(a, "size"), out sz);
                                info.AssetSize = sz;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                info.Eroare = ex.Message;
                Trace.WriteLine("UpdateService.CheckForUpdate: " + ex.Message);
            }
            if (!string.IsNullOrEmpty(info.Eroare))
                Log("Verificare ESUATA: " + info.Eroare + " (curent v" + info.CurrentVersion + ")");
            else
                Log("Verificare: curent v" + info.CurrentVersion + ", ultima " +
                    (info.LatestVersion != null ? "v" + info.LatestVersion : "necunoscuta") +
                    ", update=" + info.HasUpdate);
            return info;
        }

        /// <summary>Daca jetonul mai e primit de GitHub. Intrebam o data, scurt.</summary>
        private static bool JetonBun(string jeton)
        {
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", USER_AGENT);
                    wc.Headers.Add("Accept", "application/vnd.github+json");
                    wc.Headers.Add("Authorization", "Bearer " + jeton);
                    wc.DownloadString("https://api.github.com/repos/" + OWNER + "/" + REPO);
                    return true;
                }
            }
            catch { return false; }
        }

        /// <summary>
        /// Descarca exe-ul nou intr-un fisier temporar. Returneaza calea sau null.
        /// </summary>
        public static string DownloadUpdate(UpdateInfo info, string githubToken, Action<long, long> onProgress = null)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.AssetDownloadUrl)) return null;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string tempPath = Path.Combine(Path.GetTempPath(), "PredareAmef_" + info.TagName + ".exe");
                Log("Descarc " + info.TagName + " (" + (info.AssetSize / 1024) + " KB) in " + tempPath);
                if (File.Exists(tempPath)) File.Delete(tempPath);

                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", USER_AGENT);
                    // Pentru asset-uri private/public: GitHub API cere Accept: application/octet-stream
                    wc.Headers.Add("Accept", "application/octet-stream");
                    // Acelasi lucru ca la verificare: un jeton stricat ar opri descarcarea,
                    // desi fisierul se poate lua si fara el.
                    if (!string.IsNullOrWhiteSpace(githubToken) && JetonBun(githubToken))
                        wc.Headers.Add("Authorization", "Bearer " + githubToken);

                    if (onProgress != null)
                    {
                        wc.DownloadProgressChanged += (s, e) =>
                            onProgress(e.BytesReceived, e.TotalBytesToReceive > 0 ? e.TotalBytesToReceive : info.AssetSize);
                    }

                    var done = new ManualResetEventSlim(false);
                    Exception dlEx = null;
                    wc.DownloadFileCompleted += (s, e) => { dlEx = e.Error; done.Set(); };
                    wc.DownloadFileAsync(new Uri(info.AssetDownloadUrl), tempPath);
                    done.Wait();
                    if (dlEx != null) throw dlEx;
                }

                if (File.Exists(tempPath) && new FileInfo(tempPath).Length > 0)
                {
                    Log("Descarcat OK (" + (new FileInfo(tempPath).Length / 1024) + " KB).");
                    return tempPath;
                }
                Log("Descarcare ESUATA: fisierul temporar lipseste sau e gol.");
            }
            catch (Exception ex)
            {
                Log("Descarcare ESUATA: " + ex.Message);
                Trace.WriteLine("UpdateService.DownloadUpdate: " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Inlocuieste exe-ul curent cu noul exe printr-un script .bat care:
        ///   1) asteapta 2 sec (procesul curent sa se inchida)
        ///   2) copiaza newExe peste exe-ul curent
        ///   3) reporneste aplicatia
        /// Apoi inchide aplicatia curenta.
        /// </summary>
        public static void ApplyUpdateAndRestart(string newExePath)
        {
            string currentExe = Assembly.GetExecutingAssembly().Location;
            string bat = Path.Combine(Path.GetTempPath(), "PredareAmef_update.bat");
            string log = LogPath;

            Log("Aplicare update: copiez \"" + Path.GetFileName(newExePath) + "\" peste \"" + currentExe + "\"" +
                (RuleazaDinOneDrive() ? " [ATENTIE: folder OneDrive — copierea poate fi blocata/revenita]" : ""));

            // Comparam dimensiunile ca sa stim sigur daca s-a scris (OneDrive poate reveni
            // la fisierul vechi, caz in care 'copy' da succes dar continutul ramane vechi).
            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("setlocal EnableDelayedExpansion");
            sb.AppendLine("set \"LOG=" + log + "\"");
            sb.AppendLine("set \"NEW=" + newExePath + "\"");
            sb.AppendLine("set \"CUR=" + currentExe + "\"");
            sb.AppendLine("timeout /t 3 /nobreak >nul");
            sb.AppendLine("for %%A in (\"!NEW!\") do set SRC=%%~zA");
            sb.AppendLine("set RETRY=0");
            sb.AppendLine(":COPY");
            sb.AppendLine("copy /Y /B \"!NEW!\" \"!CUR!\" >nul 2>&1");
            sb.AppendLine("set DST=0");
            sb.AppendLine("for %%A in (\"!CUR!\") do set DST=%%~zA");
            sb.AppendLine("if \"!DST!\"==\"!SRC!\" goto OK");
            sb.AppendLine("set /a RETRY+=1");
            sb.AppendLine("if !RETRY! lss 20 ( timeout /t 2 /nobreak >nul & goto COPY )");
            sb.AppendLine("echo %date% %time%  COPIERE ESUATA dupa 20 incercari (src=!SRC! dst=!DST!; fisier blocat / OneDrive / permisiuni).>> \"!LOG!\"");
            sb.AppendLine("goto RESTART");
            sb.AppendLine(":OK");
            sb.AppendLine("echo %date% %time%  Copiere reusita (!DST! bytes).>> \"!LOG!\"");
            sb.AppendLine(":RESTART");
            sb.AppendLine("start \"\" \"!CUR!\"");
            sb.AppendLine("del \"!NEW!\" >nul 2>&1");
            sb.AppendLine("(goto) 2>nul & del \"%~f0\"");

            File.WriteAllText(bat, sb.ToString(), Encoding.Default);

            var psi = new ProcessStartInfo
            {
                FileName = bat,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = ""
            };
            Process.Start(psi);

            // Inchide aplicatia curenta — batch va astepta 2 sec apoi copiaza
            Environment.Exit(0);
        }
    }
}
