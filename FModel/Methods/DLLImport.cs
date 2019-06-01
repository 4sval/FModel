using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FModel
{
    static class DLLImport
    {
        /// <summary>
        /// used to check if internet is turned on or off
        /// </summary>
        /// <param name="description"></param>
        /// <param name="reservedValue"></param>
        /// <returns> boolean about the internet </returns>
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int description, int reservedValue);
        public static bool IsInternetAvailable()
        {
            return InternetGetConnectedState(description: out _, reservedValue: 0);
        }

        /// <summary>
        /// used to set the theme on the TreeView
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="pszSubAppName"></param>
        /// <param name="pszSubIdList"></param>
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);
        public static void SetTreeViewTheme(IntPtr treeHandle)
        {
            SetWindowTheme(treeHandle, "explorer", null);
        }
    }
}
