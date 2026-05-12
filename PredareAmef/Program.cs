using System;
using System.Windows.Forms;
using PredareAmef.Forms;

namespace PredareAmef
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK) return;
            }

            // Check pentru update se face din MainForm.Shown (vezi MainForm.cs)
            Application.Run(new MainForm());
        }
    }
}
