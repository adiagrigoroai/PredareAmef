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
        }

        public static Version GetCurrentVersion()
        {
            try { return Assembly.GetExecutingAssembly().GetName().Version; }
            catch { return new Version(0, 0, 0, 0); }
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
                    if (!string.IsNullOrWhiteSpace(githubToken))
                        wc.Headers.Add("Authorization", "Bearer " + githubToken);
                    string json = wc.DownloadString(url);
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
                Trace.WriteLine("UpdateService.CheckForUpdate: " + ex.Message);
            }
            return info;
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
                if (File.Exists(tempPath)) File.Delete(tempPath);

                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", USER_AGENT);
                    // Pentru asset-uri private/public: GitHub API cere Accept: application/octet-stream
                    wc.Headers.Add("Accept", "application/octet-stream");
                    if (!string.IsNullOrWhiteSpace(githubToken))
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
                    return tempPath;
            }
            catch (Exception ex)
            {
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

            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("timeout /t 2 /nobreak >nul");
            sb.AppendLine("set RETRY=0");
            sb.AppendLine(":COPY");
            sb.AppendLine("copy /Y \"" + newExePath + "\" \"" + currentExe + "\" >nul 2>&1");
            sb.AppendLine("if errorlevel 1 (");
            sb.AppendLine("  set /a RETRY+=1");
            sb.AppendLine("  if !RETRY! lss 5 (");
            sb.AppendLine("    timeout /t 1 /nobreak >nul");
            sb.AppendLine("    goto COPY");
            sb.AppendLine("  )");
            sb.AppendLine(")");
            sb.AppendLine("start \"\" \"" + currentExe + "\"");
            sb.AppendLine("del \"" + newExePath + "\" >nul 2>&1");
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
