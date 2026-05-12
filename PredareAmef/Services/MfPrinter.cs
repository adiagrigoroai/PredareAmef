using System;

namespace PredareAmef.Services
{
    /// <summary>
    /// Pas 5 — Printare raport sumar pe aparat, interval Z [start..end].
    /// CMD 95 cu tip "0" = sumar pe imprimanta, "1" = detaliat pe imprimanta.
    /// </summary>
    public sealed class MfPrinter
    {
        private const int CMD_FM_BY_Z = 95;

        public int PrintFmSummaryByZRange(DudeClient dude, int zStart, int zEnd, ILogger log)
        {
            log.Log("Printare sumar Z" + zStart + " -> Z" + zEnd + " pe aparat...", LogLevel.Info);

            string o = "";
            // "0" = sumar pe imprimanta
            int r = dude.ExecuteCommand(CMD_FM_BY_Z, "0\t" + zStart + "\t" + zEnd + "\t", ref o);
            if (r == 0)
            {
                log.Log("Raport sumar tiparit cu succes pe aparat.", LogLevel.Success);
                return 0;
            }

            log.Log("CMD 95 print tab err=" + r + " (" + (dude.LastError ?? "") + ")", LogLevel.Warning);

            // Fallback: comma-separated
            r = dude.ExecuteCommand(CMD_FM_BY_Z, "0," + zStart + "," + zEnd, ref o);
            if (r == 0)
            {
                log.Log("Raport sumar tiparit (fallback comma).", LogLevel.Success);
                return 0;
            }

            // Fallback: DocumentNumber properties
            try
            {
                dude.Raw.DocumentNumber_StartValue = zStart;
                dude.Raw.DocumentNumber_EndValue = zEnd;
                r = dude.ExecuteCommand(CMD_FM_BY_Z, "0", ref o);
                if (r == 0)
                {
                    log.Log("Raport sumar tiparit (fallback props).", LogLevel.Success);
                    return 0;
                }
            }
            catch { }

            log.Log("Printare sumar ESUATA err=" + r, LogLevel.Error);
            return r;
        }
    }
}
