using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.sysyemProxy
{
    class SystemProxy
    {
        #region system proxy management
        //https://social.msdn.microsoft.com/Forums/vstudio/en-US/19517edf-8348-438a-a3da-5fbe7a46b61a/how-to-change-global-windows-proxy-using-c-net-with-immediate-effect?forum=csharpgeneral
        [DllImport("wininet.dll")]
        static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        const int INTERNET_OPTION_REFRESH = 37;
        const int INTERNET_OPTION_PROXY_SETTINGS_CHANGED = 95;
        const int ManualMode = 1;
        const int PACMode = 2;
        const int GlobalMode = 3;
        static Random paccounter = new Random(); // to force windows refresh pac files

        public static void UpdateSystemProxy (int proxyMode)
        {
            if (proxyMode == ManualMode)
            {
                return;
            }
            RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
            if (proxyMode == PACMode)
            {
                registry.SetValue("ProxyEnable", 0);
                registry.SetValue("AutoConfigURL", $"http://127.0.0.1:19000/proxy.pac/{paccounter.Next()}", RegistryValueKind.String);
            }
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
        
        #endregion

        public SystemProxy ()
        {

        }

    }
}