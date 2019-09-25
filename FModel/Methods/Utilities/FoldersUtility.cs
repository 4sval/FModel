using System;
using System.Diagnostics;
using System.IO;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Utilities
{
    static class FoldersUtility
    {
        /// <summary>
        /// FOutput_Path should never be empty so no need to check
        /// </summary>
        public static void OpenOutputFolder()
        {
            Process.Start(@"" + FProp.Default.FOutput_Path);
        }

        /// <summary>
        /// at startup
        /// </summary>
        public static void LoadFolders()
        {
            FixNoOutputPath();
            CreateDefaultSubFolders();
        }

        /// <summary>
        /// if FOutput_Path is empty the .Exe directory will be FOutput_Path
        /// </summary>
        private static void FixNoOutputPath()
        {
            if (string.IsNullOrEmpty(FProp.Default.FOutput_Path))
            {
                FProp.Default.FOutput_Path = AppDomain.CurrentDomain.BaseDirectory;
                FProp.Default.Save();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void CreateDefaultSubFolders()
        {
            //THIS WILL STAY FOR INITIAL LAUNCH ONLY
            //if (File.Exists(FProp.Default.FOutput_Path + "\\AESManager.xml")) { File.Delete(FProp.Default.FOutput_Path + "\\AESManager.xml"); }

            Directory.CreateDirectory(FProp.Default.FOutput_Path + "\\Backup\\");
            Directory.CreateDirectory(FProp.Default.FOutput_Path + "\\Extracted\\");
            Directory.CreateDirectory(FProp.Default.FOutput_Path + "\\Icons\\");
            Directory.CreateDirectory(FProp.Default.FOutput_Path + "\\Sounds\\");
            Directory.CreateDirectory(FProp.Default.FOutput_Path + "\\Saved_JSON\\");
        }

        public static string GetFullPathWithoutExtension(string path)
        {
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)).Replace("\\", "/");
        }

        public static string FixFortnitePath(string path)
        {
            string fixedPath = path.Replace("Game", "FortniteGame/Content");
            int sep = fixedPath.LastIndexOf('.');
            return fixedPath.Substring(0, sep);
        }
    }
}
