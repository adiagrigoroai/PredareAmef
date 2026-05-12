using System;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace PredareAmef.Services
{
    /// <summary>
    /// Notificare email pentru aparate care au esuat in flux multi-sesiune.
    /// Copy simplificat din StatusAMEF cu acelasi SMTP itsmart.ro.
    /// </summary>
    public sealed class EmailService
    {
        private const string SMTP_HOST    = "mail.itsmart.ro";
        private const string SMTP_USER    = "no_reply@itsmart.ro";
        private const string SMTP_PASS    = "muxpup-8Poqxa-sigsih";
        private const string SMTP_DISPLAY = "IT Smart | Predare AMEF";

        public bool TrimiteEmailEsec(string toAddr, string firma, string serie, string logText, string outputDir, ILogger log)
        {
            try
            {
                var msg = new MailMessage();
                msg.From = new MailAddress(SMTP_USER, SMTP_DISPLAY);
                if (string.IsNullOrWhiteSpace(toAddr)) toAddr = "sales@itsmartretail.ro";
                msg.To.Add(toAddr);
                msg.Subject = "Predare AMEF ESUAT | " + firma + " | " + serie + " | " + DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                msg.Body = "Predare memorie fiscala AMEF a esuat (sau partial) pe aparatul:\n\n" +
                           "Firma   : " + firma + "\n" +
                           "Serie   : " + serie + "\n" +
                           "Data    : " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "\n" +
                           "PC      : " + Environment.MachineName + " (" + Environment.UserName + ")\n" +
                           "Output  : " + outputDir + "\n\n" +
                           "Atasat: predare.log\n\n" +
                           "--- LOG ---\n\n" + (logText ?? "(gol)");
                msg.IsBodyHtml = false;

                string logPath = string.IsNullOrWhiteSpace(outputDir) ? null : Path.Combine(outputDir, "predare.log");
                if (logPath != null && File.Exists(logPath))
                {
                    try { msg.Attachments.Add(new Attachment(logPath)); } catch { }
                }

                if (TrySend(msg, 587)) { log?.Log("Email esec trimis (port 587).", LogLevel.Success); return true; }
                if (TrySend(msg, 465)) { log?.Log("Email esec trimis (port 465).", LogLevel.Success); return true; }
                log?.Log("Email esec: ambele porturi SMTP au esuat.", LogLevel.Warning);
                return false;
            }
            catch (Exception ex)
            {
                log?.Log("Email esec exceptie: " + ex.Message, LogLevel.Warning);
                return false;
            }
        }

        private static bool TrySend(MailMessage msg, int port)
        {
            try
            {
                using (var smtp = new SmtpClient(SMTP_HOST, port)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(SMTP_USER, SMTP_PASS),
                    Timeout = 15000
                })
                {
                    smtp.Send(msg);
                    return true;
                }
            }
            catch { return false; }
        }
    }
}
