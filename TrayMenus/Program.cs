using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TrayMenus
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string menu_file = null;
            bool setting = false;

            string[] args = Environment.GetCommandLineArgs();

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Compare("-setting", args[i], true) == 0)
                    {
                        setting = true;
                    }
                    else if (string.Compare("-menu", args[i], true) == 0)
                    {
                        if (i + 1 < args.Length)
                        {
                            menu_file = args[i + 1];
                            i++;
                        }
                    }
                }
            }

            TrayMenuMainFrm frm = new TrayMenuMainFrm(menu_file, setting);
            frm.Visible = false;
            Application.Run(frm);
        }
    }
}