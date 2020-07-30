using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HotAirBalloon.sysyemProxy
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
                RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
                registry.SetValue("ProxyEnable", 0);
                registry.DeleteValue("AutoConfigURL", false);
            }
            
            if (proxyMode == PACMode)
            {
                RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
                registry.SetValue("ProxyEnable", 0);
                registry.SetValue("AutoConfigURL", $"http://127.0.0.1:8008/pac/{paccounter.Next()}", RegistryValueKind.String);
            }
            if (proxyMode == GlobalMode)
            {
                RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
                registry.SetValue("ProxyEnable", 1);
                var proxyServer = $"http://127.0.0.1:8009";
                var proxyOverride = "<local>;localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;172.32.*;192.168.*";
                registry.SetValue("ProxyServer", proxyServer);
                registry.SetValue("ProxyOverride", proxyOverride);
                registry.DeleteValue("AutoConfigURL", false);
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