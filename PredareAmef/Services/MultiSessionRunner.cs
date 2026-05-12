using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PredareAmef.Services
{
    /// <summary>
    /// Status per aparat in flux multi-sesiune.
    /// </summary>
    public sealed class SessionStatus
    {
        public ScannedDevice Device { get; set; }
        public string OutputDir { get; set; }
        public string Status { get; set; } = "asteapta";
        public int Progress { get; set; } = 0;   // 0..100
        public string SubLabel { get; set; } = "";
        public bool Done { get; set; } = false;
        public bool Success { get; set; } = false;
        public string Error { get; set; } = "";
        public int AnafFileCount { get; set; } = -1;   // -1 = neexecutat, 0 = esuat, >0 = OK
        public bool NeedsAnafRetry => AnafFileCount == 0;
    }

    /// <summary>
    /// Ruleaza HandoverOrchestrator pe N aparate in paralel, cu concurenta limitata.
    /// Folder format: <OutputRoot>/Predari_<YYYYMMDD_HHmmss>/<Firma>_<Serie>/
    /// </summary>
    public sealed class MultiSessionRunner
    {
        public string SessionRootDir { get; private set; }
        public List<SessionStatus> Statuses { get; private set; } = new List<SessionStatus>();

        public delegate void StatusCb(SessionStatus s);

        /// <summary>
        /// Pregateste folderele si lista de sesiuni. Apelat sincron, returneaza lista live
        /// pe care RunSessions o actualizeaza in-place.
        /// </summary>
        public List<SessionStatus> InitSessions(List<ScannedDevice> devices, HandoverOptions baseOpt)
        {
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            SessionRootDir = Path.Combine(baseOpt.OutputRoot, "Predari_" + ts);
            Directory.CreateDirectory(SessionRootDir);

            Statuses = new List<SessionStatus>();
            foreach (var d in devices)
            {
                string folderName = SafeName(d.Firma) + "_" + SafeName(d.Serie);
                string outDir = Path.Combine(SessionRootDir, folderName);
                Directory.CreateDirectory(outDir);
                Statuses.Add(new SessionStatus { Device = d, OutputDir = outDir });
            }
            return Statuses;
        }

        /// <summary>
        /// Lanseaza tasks paralele (max N) si actualizeaza Statuses in-place.
        /// Blocheaza thread-ul apelant pana cand toate sesiunile s-au incheiat.
        /// </summary>
        public void RunSessions(HandoverOptions baseOpt, int maxParallel, string emailNotify, StatusCb onUpdate, CancellationToken ct)
        {
            if (maxParallel < 1) maxParallel = 1;
            using (var sem = new SemaphoreSlim(maxParallel, maxParallel))
            {
                var tasks = new List<Task>();
                foreach (var s in Statuses)
                {
                    if (ct.IsCancellationRequested) break;
                    var sLocal = s;
                    tasks.Add(Task.Run(() =>
                    {
                        try { sem.Wait(ct); } catch { return; }
                        try
                        {
                            if (ct.IsCancellationRequested)
                            {
                                sLocal.Status = "anulat"; sLocal.Done = true;
                                onUpdate?.Invoke(sLocal);
                                return;
                            }
                            RunOne(sLocal, baseOpt, emailNotify, onUpdate, ct);
                        }
                        finally
                        {
                            try { sem.Release(); } catch { }
                        }
                    }, ct));
                }
                try { Task.WaitAll(tasks.ToArray()); } catch { /* ct cancel etc */ }
            }
        }

        private void RunOne(SessionStatus s, HandoverOptions baseOpt, string emailNotify, StatusCb onUpdate, CancellationToken ct)
        {
            s.Status = "ruleaza"; s.Progress = 0; onUpdate?.Invoke(s);

            var opt = CloneOptions(baseOpt);
            opt.Transport = TransportKind.Serial;
            opt.ComPort   = s.Device.ComPort;
            opt.BaudRate  = s.Device.Baud;
            opt.ClientName = s.Device.Firma;
            opt.OutputDirOverride = s.OutputDir;
            opt.PrescannedSerial = s.Device.Serie;
            opt.PrescannedFirma  = s.Device.Firma;
            opt.PrescannedCif    = s.Device.CIF;
            opt.PrescannedFmNum  = s.Device.FmNum;

            var logger = new ActionLogger(
                (msg, lvl) => { /* per-device log scris in fisier de orchestrator */ },
                (step, total, name) =>
                {
                    s.Progress = total > 0 ? (100 * step) / total : 0;
                    if (!string.IsNullOrEmpty(name)) s.Status = name;
                    onUpdate?.Invoke(s);
                },
                (cur, total, label) => { s.SubLabel = label ?? ""; onUpdate?.Invoke(s); }
            );

            try
            {
                var orch = new HandoverOrchestrator();
                bool ok = orch.Run(opt, logger, ct);
                s.Success = ok;
                s.AnafFileCount = orch.LastAnafFileCount;
                s.Done = true;
                if (ct.IsCancellationRequested) s.Status = "anulat";
                else if (s.NeedsAnafRetry) s.Status = "reia ANAF";
                else if (ok) s.Status = "finalizat";
                else s.Status = "esuat";
                if (!ok && !ct.IsCancellationRequested) s.Error = "Predare partiala/esuata — vezi predare.log";
                onUpdate?.Invoke(s);

                if (!ok && !ct.IsCancellationRequested && !string.IsNullOrWhiteSpace(emailNotify))
                {
                    string logText = TryReadLog(s.OutputDir);
                    new EmailService().TrimiteEmailEsec(emailNotify, s.Device.Firma, s.Device.Serie, logText, s.OutputDir, logger);
                }
            }
            catch (Exception ex)
            {
                s.Success = false; s.Done = true; s.Status = "exceptie"; s.Error = ex.Message;
                onUpdate?.Invoke(s);
                if (!string.IsNullOrWhiteSpace(emailNotify))
                {
                    string logText = TryReadLog(s.OutputDir) + "\n\n[EXCEPTIE] " + ex;
                    new EmailService().TrimiteEmailEsec(emailNotify, s.Device.Firma, s.Device.Serie, logText, s.OutputDir, logger);
                }
            }
            finally
            {
                logger.Dispose();
            }
        }

        /// <summary>
        /// Reia DOAR Pas 5 (ANAF) pe sesiunile care au AnafFileCount==0.
        /// Apelat dupa ce user-ul a repornit fizic aparatele esuate.
        /// Ruleaza secvential (un aparat la un moment dat) pentru a evita stres.
        /// </summary>
        public void RetryAnafForFailedSessions(StatusCb onUpdate, CancellationToken ct)
        {
            foreach (var s in Statuses)
            {
                if (ct.IsCancellationRequested) break;
                if (!s.NeedsAnafRetry) continue;

                s.Status = "ruleaza ANAF retry"; s.Progress = 0; onUpdate?.Invoke(s);

                var opt = new HandoverOptions
                {
                    Transport = TransportKind.Serial,
                    ComPort = s.Device.ComPort,
                    BaudRate = s.Device.Baud,
                    OutputDirOverride = s.OutputDir,
                    PrescannedSerial = s.Device.Serie,
                    PrescannedFirma  = s.Device.Firma
                };

                var logger = new ActionLogger(
                    (msg, lvl) => { },
                    (step, total, name) =>
                    {
                        s.Progress = total > 0 ? (100 * step) / total : 0;
                        if (!string.IsNullOrEmpty(name)) s.Status = name;
                        onUpdate?.Invoke(s);
                    },
                    (cur, total, label) => { s.SubLabel = label ?? ""; onUpdate?.Invoke(s); }
                );

                try
                {
                    var orch = new HandoverOrchestrator();
                    bool ok = orch.RunAnafOnly(opt, logger, ct);
                    s.AnafFileCount = orch.LastAnafFileCount;
                    s.Success = ok;
                    s.Status = ok ? "finalizat" : (s.NeedsAnafRetry ? "ANAF inca esuat" : "esuat");
                    onUpdate?.Invoke(s);
                }
                catch (Exception ex)
                {
                    s.Status = "exceptie ANAF retry"; s.Error = ex.Message;
                    onUpdate?.Invoke(s);
                }
                finally
                {
                    logger.Dispose();
                }
            }
        }

        private static string TryReadLog(string outputDir)
        {
            try
            {
                string logPath = Path.Combine(outputDir, "predare.log");
                return File.Exists(logPath) ? File.ReadAllText(logPath) : "";
            }
            catch { return ""; }
        }

        private static HandoverOptions CloneOptions(HandoverOptions src)
        {
            return new HandoverOptions
            {
                Transport = src.Transport,
                ComPort   = src.ComPort,
                BaudRate  = src.BaudRate,
                LanIp     = src.LanIp,
                LanPort   = src.LanPort,
                Step1_DumpFm = src.Step1_DumpFm,
                Step3_GenerateTxt = src.Step3_GenerateTxt,
                Step5_PrintSummary = src.Step5_PrintSummary,
                Step6_ExportXml = src.Step6_ExportXml,
                Step7_ExportHeader = src.Step7_ExportHeader,
                OutputRoot = src.OutputRoot,
                ClientName = src.ClientName
            };
        }

        private static string SafeName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "UNKNOWN";
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Trim();
        }
    }
}
