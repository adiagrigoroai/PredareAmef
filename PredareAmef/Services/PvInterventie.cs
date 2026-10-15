using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

namespace PredareAmef.Services
{
    /// <summary>O luna (sau folderul plat, la export pe Z) exportata, cu ce s-a produs.</summary>
    public sealed class PvLinieExport
    {
        public string Eticheta { get; set; }   // ex: "2026-09" sau "Rapoarte Z 100-120"
        public int Fisiere { get; set; }        // nr. rapoarte Z
        public string Arhiva { get; set; }      // numele ZIP, daca exista
    }

    /// <summary>Datele pentru procesul verbal de interventie (export XML la cerere).</summary>
    public sealed class PvInterventieData
    {
        public string Firma { get; set; }
        public string Cif { get; set; }
        public string Model { get; set; }
        public string Serie { get; set; }
        public string Nui { get; set; }          // numar unic identificare (FM)
        public string UnitateService { get; set; } = "IT SMART RETAIL";
        public string Reprezentant { get; set; }
        public string LocInstalare { get; set; }

        public bool DupaData { get; set; }
        public DateTime DataDe { get; set; }
        public DateTime DataPana { get; set; }
        public int ZDe { get; set; }
        public int ZPana { get; set; }

        public string NotificareSpv { get; set; } // nr./data notificarii, optional
        public DateTime DataInterventie { get; set; } = DateTime.Now;
        public List<PvLinieExport> Linii { get; set; } = new List<PvLinieExport>();
        public int TotalFisiere { get; set; }
    }

    /// <summary>
    /// Genereaza un proces-verbal de interventie (export fisiere XML la cerere) in format RTF,
    /// care se deschide direct in Word / WordPad si poate fi tiparit sau salvat ca PDF.
    /// Este un document DISTINCT de procesul verbal de predare a memoriei fiscale.
    /// </summary>
    public static class PvInterventie
    {
        /// <summary>
        /// Genereaza procesul verbal ca PDF (MigraDoc) — arata bine si pe telefon,
        /// cu logo IT Smart, diacritice si semnaturi. Returneaza calea fisierului.
        /// </summary>
        public static string GenereazaPdf(PvInterventieData d, string folder)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            d.Firma = Branding.CurataNume(d.Firma);
            d.Model = Branding.CurataNume(d.Model);
            d.LocInstalare = Branding.CurataNume(d.LocInstalare);

            string nume = "PV interventie_" + SafeNume(d.Firma) + "_" + SafeNume(d.Serie) + "_" +
                          d.DataInterventie.ToString("yyyy-MM-dd") + ".pdf";
            string cale = Path.Combine(folder, nume);

            string perioada = d.DupaData
                ? "perioadei " + d.DataDe.ToString("dd.MM.yyyy") + " - " + d.DataPana.ToString("dd.MM.yyyy")
                : "intervalului de rapoarte Z de la Z" + d.ZDe + " la Z" + d.ZPana;
            string notif = string.IsNullOrWhiteSpace(d.NotificareSpv) ? "" : " (notificarea " + d.NotificareSpv.Trim() + ")";

            var albastru = new Color(11, 92, 173);
            var gri = new Color(120, 120, 120);

            var doc = new Document();
            doc.Info.Title = "Proces-verbal de interventie";
            var normal = doc.Styles["Normal"];
            normal.Font.Name = "Calibri";
            normal.Font.Size = 10.5;
            normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(6);

            var sec = doc.AddSection();
            sec.PageSetup.PageFormat = PageFormat.A4;
            sec.PageSetup.TopMargin = Unit.FromCentimeter(1.6);
            sec.PageSetup.BottomMargin = Unit.FromCentimeter(1.6);
            sec.PageSetup.LeftMargin = Unit.FromCentimeter(2);
            sec.PageSetup.RightMargin = Unit.FromCentimeter(2);

            // logo (scris temporar pe disc — MigraDoc il citeste la randare)
            string logoTmp = null;
            byte[] logo = Branding.LogoPng();
            if (logo != null)
            {
                try
                {
                    logoTmp = Path.Combine(Path.GetTempPath(), "itsmart_logo_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".png");
                    File.WriteAllBytes(logoTmp, logo);
                    var pImg = sec.AddParagraph();
                    pImg.Format.Alignment = ParagraphAlignment.Center;
                    var im = pImg.AddImage(logoTmp);
                    im.Width = Unit.FromCentimeter(5.2);
                    im.LockAspectRatio = true;
                }
                catch { logoTmp = null; }
            }

            AddCentrat(sec, Branding.Companie, 8.5, gri, false);
            AddCentrat(sec, Branding.Adresa, 8.5, gri, false);
            AddCentrat(sec, "Tel: " + Branding.Telefon + "  ·  " + Branding.Email + "  ·  " + Branding.Site, 8.5, gri, false);

            var titlu = sec.AddParagraph("PROCES-VERBAL DE INTERVENTIE");
            titlu.Format.Alignment = ParagraphAlignment.Center;
            titlu.Format.Font.Bold = true; titlu.Format.Font.Size = 16; titlu.Format.Font.Color = albastru;
            titlu.Format.SpaceBefore = Unit.FromPoint(10);
            AddCentrat(sec, "Export fisiere fiscale XML la cerere", 10.5, albastru, false);
            AddCentrat(sec, "Incheiat astazi, " + d.DataInterventie.ToString("dd.MM.yyyy HH:mm"), 10, null, false);

            var p1 = sec.AddParagraph();
            p1.Format.SpaceBefore = Unit.FromPoint(10);
            p1.Format.Alignment = ParagraphAlignment.Justify;
            p1.AddText("Subscrisa " + d.UnitateService + ", in calitate de unitate de service acreditata, am efectuat operatiunea de export al fisierelor fiscale XML de pe aparatul de marcat electronic fiscal (AMEF), la solicitarea " + NumeFirma(d.Firma) + (string.IsNullOrWhiteSpace(d.Cif) ? "" : ", CUI " + d.Cif) + ".");

            var p2 = sec.AddParagraph();
            p2.Format.Alignment = ParagraphAlignment.Justify;
            p2.AddText("Operatiunea a fost efectuata ca urmare a notificarii primite in Spatiul Privat Virtual (SPV)" + notif + " privind lipsa rapoartelor Z aferente " + perioada + ".");

            var p3 = sec.AddParagraph();
            p3.Format.Alignment = ParagraphAlignment.Justify;
            p3.AddText("Prin mijloacele tehnice puse la dispozitie de producatorul aparatului s-au exportat fisierele XML (format .p7b semnate digital, declaratie A4200) de catre unitatea de service, dupa cum urmeaza:");

            var pa = sec.AddParagraph(); pa.Format.SpaceBefore = Unit.FromPoint(4);
            pa.AddFormattedText("Aparat: ", TextFormat.Bold); pa.AddText(NeMinus(d.Model) + "      ");
            pa.AddFormattedText("Serie: ", TextFormat.Bold); pa.AddText(NeMinus(d.Serie) + "      ");
            pa.AddFormattedText("NUI (FM): ", TextFormat.Bold); pa.AddText(NeMinus(d.Nui));
            if (!string.IsNullOrWhiteSpace(d.LocInstalare))
            {
                var pl = sec.AddParagraph();
                pl.AddFormattedText("Loc instalare: ", TextFormat.Bold); pl.AddText(d.LocInstalare);
            }

            if (d.Linii != null && d.Linii.Count > 0)
            {
                foreach (var l in d.Linii)
                {
                    var pb = sec.AddParagraph();
                    pb.Format.LeftIndent = Unit.FromCentimeter(0.6);
                    pb.Format.SpaceAfter = Unit.FromPoint(2);
                    string ad = string.IsNullOrWhiteSpace(l.Arhiva) ? "" : "  -  arhiva " + l.Arhiva;
                    pb.AddText("•  " + l.Eticheta + ": " + l.Fisiere + " rapoarte Z" + ad);
                }
            }
            var pt = sec.AddParagraph();
            pt.AddFormattedText("Total fisiere exportate: " + d.TotalFisiere, TextFormat.Bold);

            var p4 = sec.AddParagraph(); p4.Format.SpaceBefore = Unit.FromPoint(6);
            p4.Format.Alignment = ParagraphAlignment.Justify;
            p4.AddText("Fisierele au fost validate cu aplicatia DUKIntegrator pusa la dispozitie de ANAF si arhivate pentru transmitere.");
            var p5 = sec.AddParagraph();
            p5.AddText("Prezentul proces-verbal a fost incheiat in 2 (doua) exemplare, cate unul pentru fiecare parte.");

            var tbl = sec.AddTable();
            tbl.Borders.Width = 0;
            tbl.Format.SpaceBefore = Unit.FromPoint(28);
            tbl.AddColumn(Unit.FromCentimeter(8.5));
            tbl.AddColumn(Unit.FromCentimeter(8.5));
            var r1 = tbl.AddRow();
            r1.Cells[0].AddParagraph().AddFormattedText("Unitate de service", TextFormat.Bold);
            r1.Cells[1].AddParagraph().AddFormattedText("Beneficiar", TextFormat.Bold);
            var r2 = tbl.AddRow();
            r2.Cells[0].AddParagraph(d.UnitateService);
            r2.Cells[1].AddParagraph(NumeFirma(d.Firma));
            if (!string.IsNullOrWhiteSpace(d.Reprezentant))
            {
                var r3 = tbl.AddRow();
                r3.Cells[0].AddParagraph("Reprezentant: " + d.Reprezentant);
            }
            var r4 = tbl.AddRow(); r4.TopPadding = Unit.FromPoint(18);
            r4.Cells[0].AddParagraph("Semnatura: ______________");
            r4.Cells[1].AddParagraph("Semnatura: ______________");

            var renderer = new PdfDocumentRenderer(true) { Document = doc };
            renderer.RenderDocument();
            renderer.PdfDocument.Save(cale);

            try { if (logoTmp != null && File.Exists(logoTmp)) File.Delete(logoTmp); } catch { }
            return cale;
        }

        private static void AddCentrat(Section sec, string text, double size, Color? culoare, bool bold)
        {
            var p = sec.AddParagraph(text);
            p.Format.Alignment = ParagraphAlignment.Center;
            p.Format.Font.Size = size;
            p.Format.Font.Bold = bold;
            p.Format.SpaceAfter = Unit.FromPoint(1);
            if (culoare.HasValue) p.Format.Font.Color = culoare.Value;
        }

        public static string GenereazaRtf(PvInterventieData d, string folder)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            d.Firma = Branding.CurataNume(d.Firma);
            d.Model = Branding.CurataNume(d.Model);
            d.LocInstalare = Branding.CurataNume(d.LocInstalare);

            string nume = "PV interventie_" + SafeNume(d.Firma) + "_" + SafeNume(d.Serie) + "_" +
                          d.DataInterventie.ToString("yyyy-MM-dd") + ".rtf";
            string cale = Path.Combine(folder, nume);

            string perioada = d.DupaData
                ? "perioadei " + d.DataDe.ToString("dd.MM.yyyy") + " - " + d.DataPana.ToString("dd.MM.yyyy")
                : "intervalului de rapoarte Z de la Z" + d.ZDe + " la Z" + d.ZPana;

            string notif = string.IsNullOrWhiteSpace(d.NotificareSpv)
                ? ""
                : " (notificarea " + d.NotificareSpv.Trim() + ")";

            var sb = new StringBuilder();
            sb.Append(@"{\rtf1\ansi\ansicpg1250\deff0{\fonttbl{\f0 Calibri;}}");
            sb.Append(@"{\colortbl;\red11\green92\blue173;\red130\green154\blue177;}");
            sb.Append(@"\viewkind4\uc1\fs22 ");

            // Antet cu logo IT Smart
            string logo = EmbedLogoRtf();
            if (logo != null) Par(sb, @"\qc " + logo);
            Par(sb, @"\qc\cf2\fs16 " + Rtf(Branding.Companie) + @"\cf0\fs22");
            Par(sb, @"\qc\cf2\fs16 " + Rtf(Branding.Adresa) + @"\cf0\fs22");
            Par(sb, @"\qc\cf2\fs16 " + Rtf("Tel: " + Branding.Telefon + "  ·  " + Branding.Email + "  ·  " + Branding.Site) + @"\cf0\fs22");
            Par(sb, @"\qc\sa60 ");
            Par(sb, @"\qc\cf1\b\fs32 PROCES-VERBAL DE INTERVENTIE\cf0\b0\fs22");
            Par(sb, @"\qc\cf2 Export fisiere fiscale XML la cerere\cf0");
            Par(sb, @"\qc Incheiat astazi, " + Rtf(d.DataInterventie.ToString("dd.MM.yyyy HH:mm")));
            Par(sb, @"\sa40 ");

            // Corp
            Par(sb, @"\qj Subscrisa " + Rtf(d.UnitateService) + @", in calitate de unitate de service acreditata, " +
                    @"am efectuat operatiunea de export al fisierelor fiscale XML de pe aparatul de marcat electronic fiscal (AMEF), " +
                    @"la solicitarea " + Rtf(NumeFirma(d.Firma)) + (string.IsNullOrWhiteSpace(d.Cif) ? "" : ", CUI " + Rtf(d.Cif)) + ".");

            Par(sb, @"\qj Operatiunea a fost efectuata ca urmare a notificarii primite in Spatiul Privat Virtual (SPV)" +
                    Rtf(notif) + @" privind lipsa rapoartelor Z aferente " + Rtf(perioada) + ".");

            Par(sb, @"\qj Prin mijloacele tehnice puse la dispozitie de producatorul aparatului s-au exportat fisierele XML " +
                    @"(format .p7b semnate digital, declaratie A4200) de catre unitatea de service, dupa cum urmeaza:");

            // Date aparat
            Par(sb, "");
            Par(sb, @"\b Aparat:\b0  " + Rtf(NeMinus(d.Model)) + "   " +
                    @"\b Serie:\b0  " + Rtf(NeMinus(d.Serie)) + "   " +
                    @"\b NUI (FM):\b0  " + Rtf(NeMinus(d.Nui)));
            if (!string.IsNullOrWhiteSpace(d.LocInstalare))
                Par(sb, @"\b Loc instalare:\b0  " + Rtf(d.LocInstalare));

            // Lista exportata
            Par(sb, "");
            if (d.Linii != null && d.Linii.Count > 0)
            {
                foreach (var l in d.Linii)
                {
                    string ad = string.IsNullOrWhiteSpace(l.Arhiva) ? "" : "  -  arhiva " + l.Arhiva;
                    Par(sb, @"\bullet  " + Rtf(l.Eticheta) + ": " + l.Fisiere + " rapoarte Z" + Rtf(ad));
                }
            }
            Par(sb, @"\b Total fisiere exportate: " + d.TotalFisiere + @"\b0");
            Par(sb, "");
            Par(sb, @"\qj Fisierele au fost validate cu aplicatia DUKIntegrator pusa la dispozitie de ANAF si arhivate pentru transmitere.");
            Par(sb, @"\qj Prezentul proces-verbal a fost incheiat in 2 (doua) exemplare, cate unul pentru fiecare parte.");

            // Semnaturi
            Par(sb, "");
            Par(sb, "");
            Par(sb, @"\b Unitate de service\b0\tab\tab\tab\b Beneficiar\b0");
            Par(sb, Rtf(d.UnitateService) + @"\tab\tab\tab\tab " + Rtf(NumeFirma(d.Firma)));
            string rep = string.IsNullOrWhiteSpace(d.Reprezentant) ? "" : "Reprezentant: " + d.Reprezentant;
            Par(sb, Rtf(rep) + @"\tab\tab\tab\tab Semnatura: ______________");
            Par(sb, @"Semnatura: ______________");

            sb.Append("}");

            File.WriteAllText(cale, sb.ToString(), Encoding.ASCII);
            return cale;
        }

        private static void Par(StringBuilder sb, string continut)
        {
            sb.Append(continut);
            sb.Append(@"\par ");
        }

        /// <summary>Text -> RTF: escapeaza \ { } si transforma diacriticele in \uN?.</summary>
        private static string Rtf(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 16);
            foreach (char c in s)
            {
                if (c == '\\' || c == '{' || c == '}') { sb.Append('\\').Append(c); }
                else if (c < 128) { sb.Append(c); }
                else { sb.Append(@"\u").Append((int)c).Append('?'); }
            }
            return sb.ToString();
        }

        /// <summary>Logo-ul IT Smart ca imagine RTF ({\pict\pngblip ...}), sau null.</summary>
        private static string EmbedLogoRtf()
        {
            byte[] png = Branding.LogoPng();
            if (png == null || png.Length == 0) return null;
            // Latime afisata ~200px (3000 twips), inaltime proportionala (1000x412).
            var sb = new StringBuilder(png.Length * 2 + 64);
            sb.Append(@"{\pict\pngblip\picwgoal3000\pichgoal1236 ");
            foreach (byte b in png) sb.Append(b.ToString("x2"));
            sb.Append("}");
            return sb.ToString();
        }

        private static string NeMinus(string s)
        {
            s = (s ?? "").Trim();
            return (s == "" || s == "-") ? "necunoscut" : s;
        }

        private static string NumeFirma(string firma)
        {
            firma = (firma ?? "").Trim();
            if (firma == "" || firma == "-") return "beneficiar";
            string u = firma.ToUpperInvariant();
            if (u.Contains("SRL") || u.Contains("S.R.L") || u.StartsWith("SC ") || u.StartsWith("S.C"))
                return firma;
            return "S.C. " + firma + " S.R.L.";
        }

        private static string SafeNume(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "necunoscut";
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Trim();
        }
    }
}
