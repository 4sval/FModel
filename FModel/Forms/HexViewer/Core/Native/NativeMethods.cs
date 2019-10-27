using System.Runtime.InteropServices;
using System.Text;

namespace WpfHexaEditor.Core.Native
{
    /// <summary>
    /// Used for key detection
    /// </summary>
    internal static class NativeMethods
    {
        internal enum MapType : uint
        {
            MapvkVkToVsc = 0x0,
            MapvkVscToVk = 0x1,
            MapvkVkToChar = 0x2,
            MapvkVscToVkEx = 0x3,
        }

        [DllImport("user32.dll")]
        internal static extern int ToUnicode(uint wVirtKey,
                                             uint wScanCode,
                                             byte[] lpKeyState,
                                             [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff,
                                             int cchBuff,
                                             uint wFlags);

        [DllImport("user32.dll")]
        internal static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        internal static extern uint MapVirtualKey(uint uCode, MapType uMapType);
    }
}