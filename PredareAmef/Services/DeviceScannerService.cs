using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace PredareAmef.Services
{
    /// <summary>
    /// Identitate aparat detectat pe un port serial.
    /// </summary>
    public sealed class ScannedDevice
    {
        public string ComPort { get; set; }
        public int    Baud    { get; set; }
        public string Model   { get; set; } = "-";
        public string Serie   { get; set; } = "-";
        public string Firma   { get; set; } = "-";
        public string CIF     { get; set; } = "-";
        public string FmNum   { get; set; } = "-";
        public bool   Selected { get; set; } = true;

        public string Display =>
            ComPort + " @ " + Baud + " | " + Model + " | " + Serie + " | " + Firma;
    }

    /// <summary>
    /// Scaneaza porturile seriale, cauta aparate AMEF si returneaza lista
    /// cu identitatea (firma, serie, model). Adaptat dupa StatusAMEF.DeviceScannerService.
    /// </summary>
    public sealed class DeviceScannerService
    {
        private static readonly int[] DEFAULT_BAUDS = { 115200, 38400, 9600 };

        public List<ScannedDevice> ScanAllPorts(ILogger log, CancellationToken ct = default(CancellationToken))
        {
            var found = new List<ScannedDevice>();
            string[] ports = SerialPort.GetPortNames();
            log?.Log("Porturi detectate: " + (ports.Length == 0 ? "[nimic]" : string.Join(", ", ports)), LogLevel.Info);

            foreach (var pn in ports)
            {
                if (ct.IsCancellationRequested) break;
                if (!pn.StartsWith("COM", StringComparison.OrdinalIgnoreCase)) continue;

                ScannedDevice sd = TryProbePort(pn, log, ct);
                if (sd != null) found.Add(sd);
            }
            log?.Log("Scan complet: " + found.Count + " AMEF gasit(e).", LogLevel.Success);
            return found;
        }

        private ScannedDevice TryProbePort(string port, ILogger log, CancellationToken ct)
        {
            foreach (int baud in DEFAULT_BAUDS)
            {
                if (ct.IsCancellationRequested) return null;
                log?.Log(port + " @ " + baud + "...", LogLevel.Debug);

                string err;
                using (var dude = DudeClient.OpenSerial(port, baud, out err, log))
                {
                    if (dude == null) continue;

                    string o = "";
                    int r = dude.ExecuteCommand(123, "1\t", ref o);
                    if (r != 0) continue;

                    var f = (o ?? "").Split('\t');
                    if (f.Length < 6) continue;

                    var sd = new ScannedDevice
                    {
                        ComPort = port,
                        Baud    = baud,
                        Model   = dude.ModelName ?? (f.Length > 0 ? f[0] : "-"),
                        Serie   = NullIfDash(f[1]) ?? dude.SerialNumber ?? "-",
                        Firma   = NullIfDash(f[3]) ?? "-",
                        FmNum   = NullIfDash(f[4]) ?? dude.FmNumber ?? "-",
                        CIF     = NullIfDash(f[5]) ?? "-",
                    };
                    if (sd.CIF.StartsWith("CIF:", StringComparison.OrdinalIgnoreCase))
                        sd.CIF = sd.CIF.Substring(4).Trim();

                    log?.Log("  AMEF: " + sd.Model + " | " + sd.Serie + " | " + sd.Firma, LogLevel.Success);
                    return sd;
                }
            }
            return null;
        }

        private static string NullIfDash(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            return (s == "-" || s == "?") ? null : s;
        }
    }
}
