using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;

namespace PredareAmef.Services
{
    /// <summary>
    /// Tipareste etichetele de predare pe imprimanta aleasa.
    ///
    /// CRM-ul le trimite ca imagine, randata din acelasi PDF pe care il da si pagina, la
    /// 300 de puncte pe tol. Aici doar o punem pe hartie la marimea ei adevarata: fara
    /// scalare, fara cititor de PDF instalat, fara dependinte.
    /// </summary>
    public static class EtichetaPrinter
    {
        /// <summary>Rezolutia la care CRM-ul randeaza eticheta.</summary>
        private const float DpiEticheta = 300f;

        /// <summary>Imprimantele instalate pe acest calculator.</summary>
        public static List<string> Imprimante()
        {
            var lista = new List<string>();
            try
            {
                foreach (string nume in PrinterSettings.InstalledPrinters) lista.Add(nume);
            }
            catch { }
            return lista;
        }

        /// <summary>Imprimanta implicita a calculatorului, sau null.</summary>
        public static string ImprimantaImplicita()
        {
            try
            {
                var s = new PrinterSettings();
                return s.PrinterName;
            }
            catch { return null; }
        }

        /// <summary>
        /// Trimite imaginea la imprimanta, la marimea ei fizica. Intoarce false si scrie
        /// motivul in <paramref name="eroare"/> daca nu a mers.
        /// </summary>
        public static bool Tipareste(string caleImagine, string numeImprimanta, out string eroare)
        {
            eroare = null;
            try
            {
                if (!File.Exists(caleImagine)) { eroare = "Eticheta nu exista pe disc."; return false; }

                // Imaginea se incarca in memorie: fisierul ramane blocat cat timp e deschis un
                // Bitmap peste el, iar tiparirea se face dupa ce iesim din aceasta metoda.
                Image imagine;
                using (var fs = new FileStream(caleImagine, FileMode.Open, FileAccess.Read))
                using (var temp = Image.FromStream(fs))
                    imagine = new Bitmap(temp);

                using (imagine)
                using (var doc = new PrintDocument())
                {
                    if (!string.IsNullOrWhiteSpace(numeImprimanta))
                        doc.PrinterSettings.PrinterName = numeImprimanta;

                    if (!doc.PrinterSettings.IsValid)
                    {
                        eroare = "Imprimanta \"" + numeImprimanta + "\" nu e disponibila.";
                        return false;
                    }

                    doc.DocumentName = Path.GetFileNameWithoutExtension(caleImagine);
                    doc.OriginAtMargins = false;
                    doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                    doc.PrintPage += (s, e) =>
                    {
                        // Marimea reala a etichetei, in sutimi de tol — cum lucreaza GDI+.
                        float latimeSutimi = imagine.Width / DpiEticheta * 100f;
                        float inaltimeSutimi = imagine.Height / DpiEticheta * 100f;

                        // Daca hartia e mai mica decat eticheta (imprimanta cu rola ingusta),
                        // micsoram proportional, ca sa nu taiem din ea.
                        float latimeHartie = e.PageBounds.Width;
                        float inaltimeHartie = e.PageBounds.Height;
                        float scara = 1f;
                        if (latimeSutimi > latimeHartie) scara = latimeHartie / latimeSutimi;
                        if (inaltimeSutimi * scara > inaltimeHartie) scara = inaltimeHartie / inaltimeSutimi;

                        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        e.Graphics.DrawImage(imagine, 0f, 0f, latimeSutimi * scara, inaltimeSutimi * scara);
                        e.HasMorePages = false;
                    };

                    doc.Print();
                }
                return true;
            }
            catch (Exception ex)
            {
                eroare = ex.Message;
                return false;
            }
        }
    }
}
