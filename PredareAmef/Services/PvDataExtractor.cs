using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PredareAmef.Services
{
    /// <summary>
    /// Datele pentru procesul verbal de predare a memoriei, asa cum le cere CRM-ul.
    /// </summary>
    public sealed class PvData
    {
        public string SerieAparat { get; set; }
        public string SerieFiscala { get; set; }

        /// <summary>Numarul ultimului Raport Z din memorie ("2162").</summary>
        public string NumarRapoarteZ { get; set; }

        /// <summary>Valoarea totala a vanzarilor, pentru citit de om ("11.437.257,92").</summary>
        public string SumaTotala { get; set; }

        /// <summary>
        /// Aceeasi suma, asa cum o scrie aparatul ("11437257.92"). Asta se trimite CRM-ului:
        /// acolo intra si intr-un camp numeric din istoricul predarilor, deci nu suporta
        /// separatori de mii.
        /// </summary>
        public string SumaTotalaBruta { get; set; }

        /// <summary>Data primului Raport Z ("26-03-2019").</summary>
        public string UtilizareStart { get; set; }

        /// <summary>Data ultimului Raport Z ("18-03-2026").</summary>
        public string UtilizareEnd { get; set; }

        /// <summary>Data fiscalizarii, cand apare in raport.</summary>
        public string DataFiscalizare { get; set; }

        /// <summary>Cat s-a putut citi din raport: lipsa unui camp nu opreste restul.</summary>
        public bool AreDateleEsentiale
        {
            get
            {
                return !string.IsNullOrWhiteSpace(SumaTotalaBruta) &&
                       !string.IsNullOrWhiteSpace(NumarRapoarteZ);
            }
        }

        public string Rezumat()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Serie aparat    : " + (SerieAparat ?? "-"));
            sb.AppendLine("Serie fiscala   : " + (SerieFiscala ?? "-"));
            sb.AppendLine("Rapoarte Z      : " + (NumarRapoarteZ ?? "-"));
            sb.AppendLine("Total vanzari   : " + (SumaTotala ?? "-"));
            sb.AppendLine("Perioada        : " + (UtilizareStart ?? "-") + " → " + (UtilizareEnd ?? "-"));
            sb.AppendLine("Fiscalizat la   : " + (DataFiscalizare ?? "-"));
            return sb.ToString();
        }
    }

    /// <summary>
    /// Scoate din raportul sumar al memoriei fiscale (fisierul .txt facut la pasul 3/4,
    /// cu CMD 95 tip 10) datele care intra in procesul verbal de predare.
    ///
    /// Raportul e text tiparibil, cu eticheta la stanga si valoarea aliniata la dreapta:
    ///
    ///     DE LA RAP.Z:                     0001/26-03-2019
    ///     LA RAP.Z:                        2162/18-03-2026
    ///     VAL. TOTAL VANZARI                   11437257.92
    ///
    /// Suma totala e singurul camp pe care nu-l stim din aparat pe alta cale: comenzile
    /// obisnuite dau doar rulajul zilei curente, nu tot ce a trecut prin memorie.
    /// </summary>
    public static class PvDataExtractor
    {
        private static readonly Regex RxDeLa =
            new Regex(@"DE\s+LA\s+RAP\.?Z:?\s+(\d+)\s*/\s*(\d{2}-\d{2}-\d{4})", RegexOptions.IgnoreCase);

        private static readonly Regex RxPanaLa =
            new Regex(@"^\s*LA\s+RAP\.?Z:?\s+(\d+)\s*/\s*(\d{2}-\d{2}-\d{4})", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly Regex RxTotalVanzari =
            new Regex(@"VAL\.\s*TOTAL\s+VANZARI\s+(-?[\d.,]+)", RegexOptions.IgnoreCase);

        private static readonly Regex RxSerieFiscala =
            new Regex(@"SERIA\s+FISCALA:?\s+(\w+)", RegexOptions.IgnoreCase);

        private static readonly Regex RxFiscalizat =
            new Regex(@"AMEF\s+FISCALIZAT\s+(\d{2}-\d{2}-\d{4})", RegexOptions.IgnoreCase);

        private static readonly Regex RxSerieAparat =
            new Regex(@"S/N:\s*(\w+)", RegexOptions.IgnoreCase);

        /// <summary>Citeste raportul si intoarce ce a gasit. Nu arunca exceptii.</summary>
        public static PvData DinRaportFm(string caleTxt)
        {
            var d = new PvData();
            try
            {
                if (string.IsNullOrWhiteSpace(caleTxt) || !File.Exists(caleTxt)) return d;
                string text = File.ReadAllText(caleTxt, Encoding.UTF8);

                var mDeLa = RxDeLa.Match(text);
                if (mDeLa.Success) d.UtilizareStart = mDeLa.Groups[2].Value;

                var mLa = RxPanaLa.Match(text);
                if (mLa.Success)
                {
                    // "0001" la inceput, "2162" la final: numarul de rapoarte Z e cel din urma.
                    d.NumarRapoarteZ = mLa.Groups[1].Value.TrimStart('0');
                    if (string.IsNullOrEmpty(d.NumarRapoarteZ)) d.NumarRapoarteZ = "0";
                    d.UtilizareEnd = mLa.Groups[2].Value;
                }

                var mTotal = RxTotalVanzari.Match(text);
                if (mTotal.Success)
                {
                    d.SumaTotalaBruta = mTotal.Groups[1].Value.Trim();
                    d.SumaTotala = NormalizeazaSuma(d.SumaTotalaBruta);
                }

                var mNui = RxSerieFiscala.Match(text);
                if (mNui.Success) d.SerieFiscala = mNui.Groups[1].Value;

                var mFisc = RxFiscalizat.Match(text);
                if (mFisc.Success) d.DataFiscalizare = mFisc.Groups[1].Value;

                var mSerie = RxSerieAparat.Match(text);
                if (mSerie.Success) d.SerieAparat = mSerie.Groups[1].Value;
            }
            catch { }
            return d;
        }

        /// <summary>
        /// Suma din raport vine cu punct zecimal ("11437257.92"). O ducem in forma
        /// romaneasca, cu separator de mii, ca sa se poata pune direct in document:
        /// "11.437.257,92".
        /// </summary>
        private static string NormalizeazaSuma(string bruta)
        {
            string s = (bruta ?? "").Trim();
            if (s.Length == 0) return "";

            decimal val;
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out val))
                return val.ToString("N2", CultureInfo.GetCultureInfo("ro-RO"));

            return s;
        }
    }
}
