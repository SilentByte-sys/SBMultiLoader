using System;
using System.Drawing;
using System.Windows.Forms;

namespace FireLoader
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Always run this first before creating any form
            ApplicationConfiguration.Initialize();

            // Load icon from file (.ico only)
            Icon appIcon = new Icon(@"C:\Users\mac98\Desktop\Devs Projects\FireLoader\FireLoader\Assets\FiveM.ico");

            // Create splash form and set icon
            SplashForm splash = new SplashForm();
            splash.Icon = appIcon;

            // Run the form
            Application.Run(splash);
        }
    }
}
