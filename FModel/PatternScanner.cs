using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FModel
{
    class PatternScanner
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr handle);

        private const uint PROCESS_ALL_ACCESS = (0x00010000 | 0x00020000 | 0x00040000 | 0x00080000 | 0x00100000 | 0xFFF);
        private const string AES_PATTERN = "C7 45 D0 ?? ?? ?? ?? C7 45 D4 ?? ?? ?? ?? C7 45 D8 ?? ?? ?? ?? C7 45 DC ?? ?? ?? ?? ?? ?? ?? ?? C7 45 E0 ?? ?? ?? ?? C7 45 E4 ?? ?? ?? ?? C7 45 E8 ?? ?? ?? ?? C7 45 EC ?? ?? ?? ??";

        //pattern scanner code mostly taken from https://guidedhacking.com/threads/simple-c-pattern-scan.13981/

        private static bool CheckPattern(string pattern, byte[] array2check)
        {
            int len = array2check.Length;
            string[] strBytes = pattern.Split(' ');
            int x = 0;
            foreach (byte b in array2check)
            {
                if (strBytes[x] == "?" || strBytes[x] == "??")
                {
                    x++;
                }
                else if (byte.Parse(strBytes[x], NumberStyles.HexNumber) == b)
                {
                    x++;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private static byte[] Read(IntPtr MemoryAddress, IntPtr handle, uint bytesToRead, out int bytesRead)
        {
            byte[] buffer = new byte[bytesToRead];
            int ptrBytesRead = 0;
            ReadProcessMemory(handle, MemoryAddress, buffer, bytesToRead, ref ptrBytesRead);
            bytesRead = ptrBytesRead;

            return buffer;
        }

        public static string GetAesFromPattern(ProcessModule module, Process launcherProcess)
        {
            try
            {
                var baseAddy = module.BaseAddress;
                uint dwSize = (uint)module.ModuleMemorySize;
                int br;
                var handle = OpenProcess(PROCESS_ALL_ACCESS, true, launcherProcess.Id);
                byte[] memDump = Read(baseAddy, handle, dwSize, out br);
                string[] pBytes = AES_PATTERN.Split(' ');
            
                IntPtr address = IntPtr.Zero;
                for (int y = 0; y < memDump.Length; y++)
                {
                    if (memDump[y] == byte.Parse(pBytes[0], NumberStyles.HexNumber))
                    {
                        byte[] checkArray = new byte[pBytes.Length];
                        for (int x = 0; x < pBytes.Length; x++)
                        {
                            checkArray[x] = memDump[y + x];
                        }
                        if (CheckPattern(AES_PATTERN, checkArray))
                        {
                            address = baseAddy + y;
                        }
                        else
                        {
                            y += pBytes.Length - (pBytes.Length / 2);
                        }
                    }
                }

                int bytesRead = 0;
                byte[] buffer = new byte[60];
                ReadProcessMemory(handle, address, buffer, (uint)buffer.Length, ref bytesRead);
                CloseHandle(handle);

                string[] patternBytesStrings = { "C745D0", "C745D4", "C745D8", "C745DC", "0F1045D0", "C745E0", "C745E4", "C745E8", "C745EC" };
                string aesBytesString = BitConverter.ToString(buffer).Replace("-", string.Empty);
                foreach (string bytes in patternBytesStrings) 
                    aesBytesString = aesBytesString.Replace(bytes, string.Empty); //lazy

                return aesBytesString;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
