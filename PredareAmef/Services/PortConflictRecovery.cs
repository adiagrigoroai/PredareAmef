using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace PredareAmef.Services
{
    /// <summary>
    /// Recovery utility pentru cazurile in care portul COM e blocat de procese DUDE orfane.
    /// Apelat DUPA un esec de open_Connection — NU preventiv (pre-check-urile interfereaza cu DUDE).
    /// </summary>
    internal static class PortConflictRecovery
    {
        // Procese candidat care pot tine portul COM ocupat
        private static readonly string[] ConflictNames = { "dude", "FMI", "FiscalArhive" };

        /// <summary>
        /// Inchide procesele DUDE orfane (alte instante decat ale noastre).
        /// NU inchide procese imposibil de killat (Access denied — rulate ca SYSTEM).
        /// Returneaza numarul de procese inchise.
        /// </summary>
        public static int KillStaleProcesses(ILogger log)
        {
            int killed = 0;
            foreach (var name in ConflictNames)
            {
                Process[] procs;
                try { procs = Process.GetProcessesByName(name); } catch { continue; }
                foreach (var p in procs)
                {
                    try
                    {
                        log?.Log("  Inchid " + name + ".exe PID " + p.Id, LogLevel.Debug);
                        p.Kill();
                        if (p.WaitForExit(2000)) killed++;
                    }
                    catch (Exception ex)
                    {
                        log?.Log("  Nu pot inchide " + name + ".exe PID " + p.Id + ": " + ex.Message, LogLevel.Debug);
                    }
                }
            }
            return killed;
        }

        /// <summary>
        /// Verifica rapid daca portul COM e accesibil la nivel OS (System.IO.Ports).
        /// Folosit DOAR pentru diagnostic (mesaj de eroare), NU ca pre-check de conectare.
        /// </summary>
        public static bool IsPortAccessible(string comPort, out string err)
        {
            err = null;
            try
            {
                using (var sp = new SerialPort(comPort))
                {
                    sp.Open();
                    sp.Close();
                }
                return true;
            }
            catch (Exception ex) { err = ex.Message; return false; }
        }

        /// <summary>
        /// Asteapta ca portul sa devina accesibil, max timeoutMs.
        /// </summary>
        public static bool WaitForPortRelease(string comPort, int timeoutMs)
        {
            var deadline = DateTime.Now.AddMilliseconds(timeoutMs);
            while (DateTime.Now < deadline)
            {
                if (IsPortAccessible(comPort, out _)) return true;
                Thread.Sleep(200);
            }
            return false;
        }

        /// <summary>
        /// Reset hardware al driver-ului USB Datecs Virtual Serial Port via PowerShell.
        /// Echivalent unplug/replug fizic — necesita drepturi standard user (NU admin) pe Windows 10/11
        /// pentru device-uri user-mode.
        /// Returneaza true daca cel putin un device a fost re-enabled.
        /// </summary>
        public static bool ResetDatecsUsbDriver(ILogger log)
        {
            try
            {
                // Construct PowerShell command care face disable + sleep + enable
                string psCmd =
                    "$d = Get-PnpDevice | Where-Object { $_.FriendlyName -match 'Datecs Virtual Serial' }; " +
                    "if ($d) { " +
                    "  $d | ForEach-Object { Disable-PnpDevice -InstanceId $_.InstanceId -Confirm:$false -ErrorAction SilentlyContinue }; " +
                    "  Start-Sleep -Seconds 2; " +
                    "  $d | ForEach-Object { Enable-PnpDevice -InstanceId $_.InstanceId -Confirm:$false -ErrorAction SilentlyContinue }; " +
                    "  Write-Host 'OK' " +
                    "} else { Write-Host 'NO_DEVICE' }";

                var psi = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -Command \"" + psCmd.Replace("\"", "\\\"") + "\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var p = Process.Start(psi))
                {
                    if (p == null) return false;
                    bool finished = p.WaitForExit(15000);
                    if (!finished) { try { p.Kill(); } catch { } return false; }
                    string output = p.StandardOutput.ReadToEnd().Trim();
                    log?.Log("  Reset USB Datecs: " + output, LogLevel.Debug);
                    return output.Contains("OK");
                }
            }
            catch (Exception ex)
            {
                log?.Log("  Reset USB Datecs esuat: " + ex.Message, LogLevel.Debug);
                return false;
            }
        }
    }
}
