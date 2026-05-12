using System;
using System.Linq;
using Interop.dude;

namespace PredareAmef.Services
{
    /// <summary>
    /// Wrapper subtire peste CFD_DUDE COM. Deschidere Serial/LAN, expune operatiile
    /// folosite de utilitarul de predare.
    /// </summary>
    public sealed class DudeClient : IDisposable
    {
        private CFD_DUDE _dude;

        public CFD_DUDE Raw => _dude;

        private DudeClient(CFD_DUDE d) { _dude = d; }

        public static DudeClient OpenSerial(string comPort, int baud, out string error)
        {
            return OpenSerial(comPort, baud, out error, null);
        }

        public static DudeClient OpenSerial(string comPort, int baud, out string error, ILogger log)
        {
            error = null;
            try
            {
                int pi = int.Parse(new string(comPort.Where(char.IsDigit).ToArray()));

                // Atempt 1: conexiune directa, fara pre-check
                var c = TryOpenSerialOnce(pi, baud, out error);
                if (c != null) return c;
                string err1 = error;

                // Recovery 1: kill dude.exe orfane si retry
                log?.Log("Conectare esuata (" + err1 + "). Incerc recovery: kill stale dude.exe...", LogLevel.Warning);
                int killed = PortConflictRecovery.KillStaleProcesses(log);
                if (killed > 0)
                {
                    log?.Log("  Inchise " + killed + " procese. Astept 1.5s eliberare port...", LogLevel.Debug);
                    System.Threading.Thread.Sleep(1500);
                    c = TryOpenSerialOnce(pi, baud, out error);
                    if (c != null)
                    {
                        log?.Log("  Recovery reusit dupa kill stale", LogLevel.Success);
                        return c;
                    }
                }

                // Recovery 2: reset PnP USB Datecs (echivalent unplug/replug)
                if (!PortConflictRecovery.IsPortAccessible(comPort, out string portErr))
                {
                    log?.Log("Portul " + comPort + " inca blocat (" + portErr + "). Incerc reset USB driver...", LogLevel.Warning);
                    if (PortConflictRecovery.ResetDatecsUsbDriver(log))
                    {
                        log?.Log("  Reset USB OK, astept stabilizare 3s...", LogLevel.Debug);
                        System.Threading.Thread.Sleep(3000);
                        c = TryOpenSerialOnce(pi, baud, out error);
                        if (c != null)
                        {
                            log?.Log("  Recovery reusit dupa reset USB", LogLevel.Success);
                            return c;
                        }
                    }
                }

                // Esec final
                error = (error ?? err1) + " (recovery: kill=" + killed + ", USB reset incercat)";
                return null;
            }
            catch (Exception ex) { error = ex.Message; return null; }
        }

        private static DudeClient TryOpenSerialOnce(int comIdx, int baud, out string error)
        {
            error = null;
            try
            {
                var dude = new CFD_DUDE();
                dude.set_TransportType(TTransportProtocol.ctc_RS232);
                dude.set_RS232((ushort)comIdx, baud);
                int rc = dude.open_Connection();
                if (rc != 0)
                {
                    error = dude.lastError_Message ?? ("err " + rc);
                    try { dude.close_Connection(); } catch { }
                    return null;
                }
                return new DudeClient(dude);
            }
            catch (Exception ex) { error = ex.Message; return null; }
        }

        public static DudeClient OpenLan(string ip, int port, out string error)
        {
            error = null;
            try
            {
                var dude = new CFD_DUDE();
                dude.set_TransportType(TTransportProtocol.ctc_TCPIP);
                dude.set_TCPIP(ip, (ushort)port);
                if (dude.open_Connection() != 0)
                {
                    error = dude.lastError_Message ?? "Eroare deschidere conexiune LAN";
                    try { dude.close_Connection(); } catch { }
                    return null;
                }
                return new DudeClient(dude);
            }
            catch (Exception ex) { error = ex.Message; return null; }
        }

        public int ExecuteCommand(int cmd, string param, ref string output)
        {
            return _dude.execute_Command(cmd, param, ref output);
        }

        public string LastAnswer => _dude?.last_AnswerList;
        public string LastError => _dude?.lastError_Message;

        public string SerialNumber => _dude?.device_Number_Serial;
        public string FmNumber => _dude?.device_Number_FMemory;
        public string ModelName => _dude?.device_Model_Name;

        public int SetDownloadPath(string path) => _dude.set_Download_Path(path);

        public string DateRangeStart { get => _dude.DateRange_StartValue; set => _dude.DateRange_StartValue = value; }
        public string DateRangeEnd   { get => _dude.DateRange_EndValue;   set => _dude.DateRange_EndValue   = value; }
        public int ZRangeStart       { get => _dude.zRange_StartValue;    set => _dude.zRange_StartValue    = value; }
        public int ZRangeEnd         { get => _dude.zRange_EndValue;      set => _dude.zRange_EndValue      = value; }

        public int DownloadAnafByDateRange() => _dude.download_ANAF_DTRange();
        public int DownloadAnafByZRange()    => _dude.download_ANAF_ZRange();

        public void EnableTracking(string dir, string fileName)
        {
            try
            {
                _dude.set_TrackingMode_Path(dir);
                _dude.set_TrackingMode_FileName(fileName);
                _dude.set_TrackingMode(true);
            }
            catch { }
        }

        public void DisableTracking()
        {
            try { _dude.set_TrackingMode(false); } catch { }
        }

        /// <summary>
        /// Citire variabila via CMD 255 (param: "VarName\t0\t\t").
        /// </summary>
        public string ReadVar255(string varName, int index = 0)
        {
            string output = "";
            int r = _dude.execute_Command(255, varName + "\t" + index + "\t\t", ref output);
            if (r != 0) return null;
            var parts = (output ?? "").Split('\t');
            return parts.Length >= 2 ? parts[1] : null;
        }

        public void Dispose()
        {
            if (_dude != null)
            {
                try { _dude.close_Connection(); } catch { }
                _dude = null;
            }
        }
    }
}
