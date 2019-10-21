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
        /// open file with the default program
        /// </summary>
        /// <param name="path"></param>
        public static void OpenWithDefaultProgram(string path)
        {
            if (Directory.Exists(Path.GetDirectoryName(path)))
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        Arguments = "\"" + path + "\"",
                        FileName = "explorer.exe"
                    };
                    Process.Start(startInfo);
                }
                catch (Exception)
                {
                    new UpdateMyConsole("Error while trying to open ", CColors.White).Append();
                    new UpdateMyConsole(path, CColors.Red, true).Append();
                }
            }
            else
            {
                new UpdateMyConsole(path, CColors.Blue).Append();
                new UpdateMyConsole(" Directory does not exist!", CColors.White, true).Append();
            }
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
            //if (Directory.Exists(FProp.Default.FOutput_Path + "\\Extracted\\")) { Directory.Delete(FProp.Default.FOutput_Path + "\\Extracted\\", true); }
            //if (Directory.Exists(FProp.Default.FOutput_Path + "\\Saved_JSON\\")) { Directory.Delete(FProp.Default.FOutput_Path + "\\Saved_JSON\\", true); }

            Directory.CreateDirectory(FProp.Default.FOutput_Path + "\\Backups\\");
            Directory.CreateDirectory(FProp.Default.FOutput_Path + "\\Exports\\");
            Directory.CreateDirectory(FProp.Default.FOutput_Path + "\\Icons\\");
            Directory.CreateDirectory(FProp.Default.FOutput_Path + "\\Sounds\\");
            Directory.CreateDirectory(FProp.Default.FOutput_Path + "\\JSONs\\");
        }

        public static string GetFullPathWithoutExtension(string path)
        {
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)).Replace("\\", "/");
        }

        public static string FixFortnitePath(string path)
        {
            string fixedPath = path.Replace("Game", "FortniteGame/Content");
            int sep = fixedPath.LastIndexOf('.');
            return fixedPath.Substring(0, sep > 0 ? sep : fixedPath.Length);
        }

        public static void CheckWatermark()
        {
            if (!string.IsNullOrEmpty(FProp.Default.FWatermarkFilePath) &&
                !File.Exists(FProp.Default.FWatermarkFilePath))
            {
                FProp.Default.FWatermarkFilePath = string.Empty;
                FProp.Default.FUseWatermark = false;
                new UpdateMyConsole("Watermark file not found, watermarking disabled.", CColors.Blue, true).Append();
            }

            if (!string.IsNullOrEmpty(FProp.Default.FBannerFilePath) &&
                !File.Exists(FProp.Default.FBannerFilePath))
            {
                FProp.Default.FBannerFilePath = string.Empty;
                FProp.Default.FUseChallengeWatermark = false;
                new UpdateMyConsole("Banner file not found, challenges custom theme disabled.", CColors.Blue, true).Append();
            }

            FProp.Default.Save();
        }
    }
}
