using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using PredareAmef.Forms;

namespace PredareAmef
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // DLL-urile PdfSharp/MigraDoc sunt inglobate in exe (resurse), ca sa mearga
            // PDF-ul si dupa un auto-update care inlocuieste doar PredareAmef.exe.
            AppDomain.CurrentDomain.AssemblyResolve += RezolvaDinResurse;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK) return;
            }

            // Check pentru update se face din MainForm.Shown (vezi MainForm.cs)
            // Interfata noua; ferestrele vechi raman in cod pentru cazuri speciale.
            Application.Run(new ShellForm());
        }

        /// <summary>Incarca din resursele inglobate DLL-urile care nu sunt pe disc (PdfSharp/MigraDoc).</summary>
        private static Assembly RezolvaDinResurse(object sender, ResolveEventArgs args)
        {
            try
            {
                string nume = new AssemblyName(args.Name).Name; // ex: "PdfSharp-gdi"
                var self = Assembly.GetExecutingAssembly();
                foreach (var res in self.GetManifestResourceNames())
                {
                    if (res.EndsWith(nume + ".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var s = self.GetManifestResourceStream(res))
                        {
                            if (s == null) return null;
                            var bytes = new byte[s.Length];
                            int off = 0, n;
                            while (off < bytes.Length && (n = s.Read(bytes, off, bytes.Length - off)) > 0) off += n;
                            return Assembly.Load(bytes);
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
