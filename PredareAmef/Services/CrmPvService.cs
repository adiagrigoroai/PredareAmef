using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace PredareAmef.Services
{
    /// <summary>Datele trimise CRM-ului pentru procesul verbal de predare a memoriei.</summary>
    public sealed class PvCerere
    {
        public string SerieAmef { get; set; }

        /// <summary>De ce se preda memoria — se tipareste in document. Obligatoriu.</summary>
        public string MotivPredare { get; set; }

        public string NumarRapoarteZ { get; set; }

        /// <summary>Valoarea totala a vanzarilor, cu punct zecimal ("11437257.92").</summary>
        public string SumaTotala { get; set; }

        public string UtilizareStart { get; set; }
        public string UtilizareEnd { get; set; }

        /// <summary>"DA" daca s-a montat memorie noua.</summary>
        public string MemorieNoua { get; set; } = "DA";

        public string PredatDe { get; set; }
        public string Observatii { get; set; }

        /// <summary>"IT SMART RETAIL", "IT SMART SUPORT" sau "IT SMART A.M.E.F" — alege sablonul.</summary>
        public string UnitateService { get; set; } = "IT SMART RETAIL";

        /// <summary>Lasat gol, CRM-ul pune adresa de instalare pe care o are in fisa aparatului.</summary>
        public string LocInstalare { get; set; }

        public string DataPredare { get; set; }
    }

    /// <summary>Ce stie CRM-ul despre un aparat, inainte de a genera documentul.</summary>
    public sealed class PvAparatCrm
    {
        public string SerieAmef;
        public string SerieFiscala;
        public string ModelAmef;
        public string NumeClient;
        public string Cui;
        public string LocInstalare;
    }

    /// <summary>
    /// Cere CRM-ului procesul verbal de predare a memoriei.
    ///
    /// CRM-ul tine sabloanele si istoricul predarilor, deci documentul se face acolo, nu aici:
    /// POST /api/predare_memorie/genereaza, cu seria aparatului. Tot acolo se salveaza si in
    /// dosarul clientului. Noi trimitem ce stim din memoria fiscala (rapoarte Z, total vanzari,
    /// perioada) si ce completeaza omul (motivul predarii).
    ///
    /// Autentificarea: acelasi cont de aplicatie ca StatusAMEF, cu drepturi mici — poate doar
    /// sa raporteze aparate, sa caute clientul si sa incarce documente.
    /// </summary>
    public sealed class CrmPvService
    {
        private const string AdresaImplicita = "https://crm.itsmartretail.ro";

        // Contul aplicatiei (obfuscat XOR 0x5A + Base64, ca in StatusAMEF).
        private const string UserOb = "KS47Li8pOzc/PHQ7Kio=";
        private const string PassOb = "LQodPis8DwloPz8/ETcKHAMRHDtvbCMjMCMqEA==";

        private readonly string _baza;
        private string _token;

        public CrmPvService(string adresaCrm = null)
        {
            _baza = (string.IsNullOrWhiteSpace(adresaCrm) ? AdresaImplicita : adresaCrm).TrimEnd('/');
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        }

        /// <summary>Ultima eroare, pentru afisare.</summary>
        public string UltimaEroare { get; private set; }

        private static string Deobfuscheaza(string enc)
        {
            var b = Convert.FromBase64String(enc);
            for (int i = 0; i < b.Length; i++) b[i] = (byte)(b[i] ^ 0x5A);
            return Encoding.UTF8.GetString(b);
        }

        /// <summary>Se autentifica si tine tokenul. Intoarce false si scrie UltimaEroare la esec.</summary>
        public bool Autentifica()
        {
            UltimaEroare = null;
            try
            {
                string corp = "{\"username\":\"" + Deobfuscheaza(UserOb) + "\",\"password\":\"" +
                              Escapa(Deobfuscheaza(PassOb)) + "\"}";
                string raspuns = Trimite(_baza + "/api/app/login", corp, null);
                _token = IaText(raspuns, "token");
                if (string.IsNullOrEmpty(_token)) { UltimaEroare = "CRM: autentificare respinsa."; return false; }
                return true;
            }
            catch (Exception ex)
            {
                UltimaEroare = "CRM: " + ex.Message;
                return false;
            }
        }

        /// <summary>Ce stie CRM-ul despre aparat. Null daca nu il gaseste (motivul e in UltimaEroare).</summary>
        public PvAparatCrm Verifica(string serie)
        {
            UltimaEroare = null;
            if (_token == null && !Autentifica()) return null;
            try
            {
                string raspuns = Trimite(_baza + "/api/predare_memorie/genereaza",
                    "{\"serie_amef\":\"" + Escapa(serie) + "\",\"verifica\":true}", _token);
                return new PvAparatCrm
                {
                    SerieAmef = IaText(raspuns, "serie_amef"),
                    SerieFiscala = IaText(raspuns, "serie_fiscala"),
                    ModelAmef = IaText(raspuns, "model_amef"),
                    NumeClient = IaText(raspuns, "nume_client"),
                    Cui = IaText(raspuns, "cui"),
                    LocInstalare = IaText(raspuns, "loc_instalare"),
                };
            }
            catch (Exception ex)
            {
                UltimaEroare = Motiv(ex);
                return null;
            }
        }

        /// <summary>
        /// Cere documentul si il salveaza in folderul dat. Intoarce calea fisierului, sau null.
        /// CRM-ul il pastreaza si in dosarul clientului, plus o inregistrare in istoricul predarilor.
        /// </summary>
        public string Genereaza(PvCerere c, string folderDestinatie)
        {
            UltimaEroare = null;
            if (c == null || string.IsNullOrWhiteSpace(c.SerieAmef))
            {
                UltimaEroare = "Lipseste seria aparatului.";
                return null;
            }
            if (_token == null && !Autentifica()) return null;

            try
            {
                var campuri = new Dictionary<string, string>
                {
                    { "serie_amef", c.SerieAmef },
                    { "motiv_predare", c.MotivPredare ?? "" },
                    { "numar_rapoarte_z", c.NumarRapoarteZ ?? "" },
                    { "suma_totala", c.SumaTotala ?? "" },
                    { "utilizare_start", c.UtilizareStart ?? "" },
                    { "utilizare_end", c.UtilizareEnd ?? "" },
                    { "memorie_noua", c.MemorieNoua ?? "" },
                    { "predat_de", c.PredatDe ?? "" },
                    { "observatii", c.Observatii ?? "" },
                    { "unitate_service", c.UnitateService ?? "" },
                    { "loc_instalare", c.LocInstalare ?? "" },
                    { "data_predare", string.IsNullOrWhiteSpace(c.DataPredare)
                        ? DateTime.Now.ToString("dd.MM.yyyy") : c.DataPredare },
                };

                var sb = new StringBuilder("{");
                bool prim = true;
                foreach (var kv in campuri)
                {
                    if (!prim) sb.Append(",");
                    prim = false;
                    sb.Append("\"").Append(kv.Key).Append("\":\"").Append(Escapa(kv.Value)).Append("\"");
                }
                sb.Append("}");

                Directory.CreateDirectory(folderDestinatie);
                string numeFisier = "Proces_verbal_predare_memorie_" + SafeName(c.SerieAmef) + "_" +
                                    DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".docx";
                string cale = Path.Combine(folderDestinatie, numeFisier);

                DescarcaPost(_baza + "/api/predare_memorie/genereaza", sb.ToString(), _token, cale);
                return cale;
            }
            catch (Exception ex)
            {
                UltimaEroare = Motiv(ex);
                return null;
            }
        }

        // ── ajutoare ──

        private static string Escapa(string s)
        {
            return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"")
                            .Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        }

        private static string SafeName(string s)
        {
            var sb = new StringBuilder();
            foreach (char ch in (s ?? "").Trim())
                sb.Append(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '_');
            return sb.Length == 0 ? "aparat" : sb.ToString();
        }

        /// <summary>Valoarea unui camp text dintr-un raspuns JSON simplu, fara dependinte externe.</summary>
        private static string IaText(string json, string cheie)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var m = System.Text.RegularExpressions.Regex.Match(
                json, "\"" + System.Text.RegularExpressions.Regex.Escape(cheie) + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            if (!m.Success) return null;
            return m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\/", "/");
        }

        private static string Motiv(Exception ex)
        {
            var we = ex as WebException;
            if (we != null && we.Response != null)
            {
                try
                {
                    using (var sr = new StreamReader(we.Response.GetResponseStream()))
                    {
                        string corp = sr.ReadToEnd();
                        string eroare = IaText(corp, "error");
                        if (!string.IsNullOrEmpty(eroare)) return "CRM: " + eroare;
                        return "CRM: " + (corp.Length > 200 ? corp.Substring(0, 200) : corp);
                    }
                }
                catch { }
            }
            return "CRM: " + ex.Message;
        }

        private static string Trimite(string url, string corpJson, string token)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/json; charset=utf-8";
            req.Timeout = 60000;
            if (token != null) req.Headers.Add("Authorization", "Bearer " + token);
            byte[] date = Encoding.UTF8.GetBytes(corpJson);
            req.ContentLength = date.Length;
            using (var st = req.GetRequestStream()) st.Write(date, 0, date.Length);
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }

        private static void DescarcaPost(string url, string corpJson, string token, string caleFisier)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/json; charset=utf-8";
            req.Timeout = 120000;
            if (token != null) req.Headers.Add("Authorization", "Bearer " + token);
            byte[] date = Encoding.UTF8.GetBytes(corpJson);
            req.ContentLength = date.Length;
            using (var st = req.GetRequestStream()) st.Write(date, 0, date.Length);
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var intrare = resp.GetResponseStream())
            using (var iesire = new FileStream(caleFisier, FileMode.Create, FileAccess.Write))
                intrare.CopyTo(iesire);
        }
    }
}
