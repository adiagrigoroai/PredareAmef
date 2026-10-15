using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;

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

        /// <summary>
        /// Trimite arhiva (ZIP) exportata + PV-ul de interventie pe email.
        /// Atasamentele sunt caile fisierelor; cele inexistente se ignora.
        /// </summary>
        public bool TrimiteArhiva(string toAddr, string firma, string serie, string perioada,
            System.Collections.Generic.IEnumerable<string> atasamente, ILogger log)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(toAddr))
                {
                    log?.Log("Email: lipseste adresa destinatarului.", LogLevel.Warning);
                    return false;
                }

                firma = Branding.CurataNume(firma);

                var msg = new MailMessage();
                msg.From = new MailAddress(SMTP_USER, SMTP_DISPLAY);
                foreach (var adr in toAddr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var a = adr.Trim();
                    if (a.Length > 0) try { msg.To.Add(a); } catch { }
                }
                if (msg.To.Count == 0) { log?.Log("Email: nicio adresa valida.", LogLevel.Warning); return false; }

                msg.Subject = "Fisiere fiscale XML AMEF – " + firma + " (" + serie + ")";

                // Varianta text (pentru clienti de mail fara HTML)
                msg.Body =
                    "Buna ziua,\r\n\r\n" +
                    "Va transmitem atasat fisierele fiscale XML extrase de pe casa de marcat, pentru perioada " + perioada + ".\r\n\r\n" +
                    "Aparat: " + firma + ", seria " + serie + ".\r\n" +
                    "Fisierele sunt in format oficial (.p7b, declaratie A4200), validate cu aplicatia DUKIntegrator pusa la dispozitie de ANAF, gata de incarcat in SPV.\r\n\r\n" +
                    "Raman la dispozitie pentru orice nelamurire.\r\n\r\n" +
                    "O zi buna,\r\n" + Branding.Companie + " | " + Branding.Telefon + " | " + Branding.Site;

                // Varianta HTML, cu logo
                string html = ConstruiesteHtml(firma, serie, perioada);
                var view = AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, "text/html");
                byte[] logo = Branding.LogoPng();
                if (logo != null)
                {
                    try
                    {
                        var lr = new LinkedResource(new MemoryStream(logo), "image/png") { ContentId = "logoitsmart", TransferEncoding = System.Net.Mime.TransferEncoding.Base64 };
                        view.LinkedResources.Add(lr);
                    }
                    catch { }
                }
                msg.AlternateViews.Add(view);
                msg.IsBodyHtml = true;

                int nAtt = 0;
                if (atasamente != null)
                    foreach (var f in atasamente)
                        if (!string.IsNullOrWhiteSpace(f) && File.Exists(f))
                            try { msg.Attachments.Add(new Attachment(f)); nAtt++; } catch { }

                if (nAtt == 0) { log?.Log("Email: niciun atasament gasit — nu trimit.", LogLevel.Warning); return false; }

                if (TrySend(msg, 587)) { log?.Log("Email trimis (" + nAtt + " atasamente, port 587).", LogLevel.Success); return true; }
                if (TrySend(msg, 465)) { log?.Log("Email trimis (" + nAtt + " atasamente, port 465).", LogLevel.Success); return true; }
                log?.Log("Email: ambele porturi SMTP au esuat.", LogLevel.Warning);
                return false;
            }
            catch (Exception ex)
            {
                log?.Log("Email exceptie: " + ex.Message, LogLevel.Warning);
                return false;
            }
        }

        private static string ConstruiesteHtml(string firma, string serie, string perioada)
        {
            string data = DateTime.Now.ToString("dd.MM.yyyy");
            string c = Branding.Culoare;
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head>");
            sb.Append("<body style=\"margin:0;padding:0;background:#f4f6f9;font-family:Segoe UI,Arial,sans-serif;color:#243b53;\">");
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f4f6f9;padding:24px 0;\"><tr><td align=\"center\">");
            sb.Append("<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 10px rgba(0,0,0,.06);\">");
            // antet cu logo
            sb.Append("<tr><td style=\"padding:28px 32px 10px 32px;border-bottom:3px solid " + c + ";\">");
            sb.Append("<img src=\"cid:logoitsmart\" alt=\"IT Smart\" width=\"180\" style=\"display:block;\"/>");
            sb.Append("</td></tr>");
            // corp
            sb.Append("<tr><td style=\"padding:26px 32px 8px 32px;font-size:15px;line-height:1.6;\">");
            sb.Append("<p style=\"margin:0 0 14px 0;\">Bună ziua,</p>");
            sb.Append("<p style=\"margin:0 0 14px 0;\">Vă transmitem atașat <b>fișierele fiscale XML</b> extrase de pe casa de marcat, pentru perioada solicitată. ");
            sb.Append("Sunt în formatul oficial acceptat de ANAF (<b>.p7b</b>, declarație <b>A4200</b>), validate cu aplicația <b>DUKIntegrator</b> și gata de încărcat în SPV.</p>");
            // tabel detalii
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:8px 0 6px 0;font-size:14px;border-collapse:collapse;\">");
            sb.Append(Rand("Client", firma, c));
            sb.Append(Rand("Serie AMEF", serie, c));
            sb.Append(Rand("Perioada", perioada, c));
            sb.Append(Rand("Data export", data, c));
            sb.Append("</table>");
            sb.Append("<p style=\"margin:16px 0 0 0;\">Rămânem la dispoziție pentru orice nelămurire.</p>");
            sb.Append("<p style=\"margin:14px 0 0 0;\">O zi bună,<br><b>Echipa " + Esc(Branding.Companie) + "</b></p>");
            sb.Append("</td></tr>");
            // footer
            sb.Append("<tr><td style=\"padding:18px 32px 24px 32px;border-top:1px solid #e6ebf1;font-size:12px;color:#829ab1;line-height:1.7;\">");
            sb.Append("<b style=\"color:#486581;\">" + Esc(Branding.Companie) + "</b><br>");
            sb.Append(Esc(Branding.Adresa) + "<br>");
            sb.Append("Tel: " + Esc(Branding.Telefon) + " &nbsp;·&nbsp; " +
                      "<a href=\"mailto:" + Esc(Branding.Email) + "\" style=\"color:" + c + ";text-decoration:none;\">" + Esc(Branding.Email) + "</a>" +
                      " &nbsp;·&nbsp; <a href=\"https://" + Esc(Branding.Site) + "\" style=\"color:" + c + ";text-decoration:none;\">" + Esc(Branding.Site) + "</a>");
            sb.Append("</td></tr>");
            sb.Append("</table></td></tr></table></body></html>");
            return sb.ToString();
        }

        private static string Rand(string eticheta, string valoare, string culoare)
        {
            return "<tr>" +
                   "<td style=\"padding:7px 10px;background:#f4f8fc;border:1px solid #e6ebf1;width:130px;color:#486581;\">" + Esc(eticheta) + "</td>" +
                   "<td style=\"padding:7px 10px;border:1px solid #e6ebf1;font-weight:600;\">" + Esc(valoare) + "</td>" +
                   "</tr>";
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
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
