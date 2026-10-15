using System.IO;
using System.Reflection;
using System.Text;

namespace PredareAmef.Services
{
    /// <summary>Elemente de identitate IT Smart + curatare text venit de la aparat.</summary>
    public static class Branding
    {
        public const string Companie   = "IT SMART RETAIL S.R.L.";
        public const string Adresa     = "Bd. Ferdinand nr. 133, Bl. E3, Sc. 7, Parter, Sector 2, București";
        public const string Telefon    = "031 433 66 85 / 0722 999 755";
        public const string Email      = "sales@itsmartretail.ro";
        public const string Site       = "www.itsmartretail.ro";
        public const string Culoare    = "#0B5CAD"; // albastru IT Smart

        private static byte[] _logo;

        /// <summary>Logo-ul IT Smart (PNG) din resursele aplicatiei; null daca lipseste.</summary>
        public static byte[] LogoPng()
        {
            if (_logo != null) return _logo;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var s = asm.GetManifestResourceStream("PredareAmef.Resources.logo-itsmart.png"))
                {
                    if (s == null) return null;
                    using (var ms = new MemoryStream())
                    {
                        s.CopyTo(ms);
                        _logo = ms.ToArray();
                    }
                }
            }
            catch { _logo = null; }
            return _logo;
        }

        /// <summary>
        /// Curata textul venit de la aparat (antetul fiscal): aparatul pune un '^'
        /// inaintea fiecarui caracter (ex: "^I^T^ ^S^M^A^R^T" = "IT SMART") si poate
        /// contine caractere de control. Rezultatul e numele citibil.
        /// </summary>
        public static string CurataNume(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (c == '^') continue;   // escape-ul Datecs dinaintea fiecarui caracter
                if (c < 32) continue;     // caractere de control
                sb.Append(c);
            }
            // spatii multiple -> unul singur
            string rez = sb.ToString().Trim();
            while (rez.Contains("  ")) rez = rez.Replace("  ", " ");
            return rez;
        }
    }
}
