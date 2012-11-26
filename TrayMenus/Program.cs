using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TrayMenus
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        extern private static Int32 CreateEvent(Int32 attributes, Int32 bManual, Int32 bInitialState, string name);
        [DllImport("kernel32.dll")]
        extern private static Int32 GetLastError();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            CreateEvent(0, 0, 0, "Global/TrayMenus");

            if (GetLastError() == 183)
                return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string menu_file = null;
            bool setting = false;
            bool hidesetting = false;
            bool hideloadmenu = false;
            bool hideexit = false;
            string notify_icon_file = null;
            string notify_tooltip = null;

            string[] args = Environment.GetCommandLineArgs();

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Compare("-setting", args[i], true) == 0)
                    {
                        setting = true;
                    }
                    if (string.Compare("-hidesetting", args[i], true) == 0)
                    {
                        hidesetting = true;
                    }
                    if (string.Compare("-hideloadmenu", args[i], true) == 0)
                    {
                        hideloadmenu = true;
                    }
                    if (string.Compare("-hideexit", args[i], true) == 0)
                    {
                        hideexit = true;
                    }
                    else if (string.Compare("-menu", args[i], true) == 0)
                    {
                        if (i + 1 < args.Length)
                        {
                            menu_file = args[i + 1];
                            i++;
                        }
                    }
                    else if (string.Compare("-icon", args[i], true) == 0)
                    {
                        if (i + 1 < args.Length)
                        {
                            notify_icon_file = args[i + 1];
                            i++;
                        }
                    }
                    else if (string.Compare("-tip", args[i], true) == 0)
                    {
                        if (i + 1 < args.Length)
                        {
                            notify_tooltip = args[i + 1];
                            i++;
                        }
                    }
                }
            }

            TrayMenuMainFrm frm = new TrayMenuMainFrm(menu_file, setting, 
                hidesetting, hideloadmenu, hideexit,
                notify_icon_file, notify_tooltip);
            frm.Visible = false;
            Application.Run(frm);
        }
    }
}