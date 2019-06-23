using FModel.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace FModel
{
    static class Utilities
    {
        /// <summary>
        /// OpenWithDefaultProgramAndNoFocus is used to automatically open sound file once they are converted
        /// </summary>
        /// <param name="path"> path is the path to the converted sound file (in "Sounds" subfolder) </param>
        public static void OpenWithDefaultProgramAndNoFocus(string path)
        {
            Process fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
        }

        /// <summary>
        /// just create the default folders FModel needs to work
        /// </summary>
        public static void CreateDefaultFolders()
        {
            Directory.CreateDirectory(App.DefaultOutputPath + "\\Backup\\");
            Directory.CreateDirectory(App.DefaultOutputPath + "\\Extracted\\");
            Directory.CreateDirectory(App.DefaultOutputPath + "\\Icons\\");
            Directory.CreateDirectory(App.DefaultOutputPath + "\\Sounds\\");
            Directory.CreateDirectory(App.DefaultOutputPath + "\\Saved JSON\\");
        }

        /// <summary>
        /// actually idk if it's useful, i added this for Admin windows account, when FModel can't even use CreateDefaultFolders 
        /// </summary>
        /// <param name="folderPath"></param>
        public static void SetFolderPermission(string folderPath)
        {
            var directoryInfo = new DirectoryInfo(folderPath);
            var directorySecurity = directoryInfo.GetAccessControl();
            var currentUserIdentity = WindowsIdentity.GetCurrent();
            var fileSystemRule = new FileSystemAccessRule(currentUserIdentity.Name,
                                                          FileSystemRights.Read,
                                                          InheritanceFlags.ObjectInherit |
                                                          InheritanceFlags.ContainerInherit,
                                                          PropagationFlags.None,
                                                          AccessControlType.Allow);

            directorySecurity.AddAccessRule(fileSystemRule);
            directoryInfo.SetAccessControl(directorySecurity);
        }

        /// <summary>
        /// By default the output folder is the Documents folder, however if user wanna change, once FModel restart SetOutputFolder is called
        /// </summary>
        public static void SetOutputFolder()
        {
            App.DefaultOutputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\FModel"; //DOCUMENTS FOLDER BY DEFAULT

            if (string.IsNullOrEmpty(Settings.Default.ExtractOutput))
            {
                Settings.Default.ExtractOutput = App.DefaultOutputPath;
                Settings.Default.Save();
            }
            else
            {
                App.DefaultOutputPath = Settings.Default.ExtractOutput;
            }

            Directory.CreateDirectory(App.DefaultOutputPath);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void JohnWickCheck()
        {
            if (File.Exists(App.DefaultOutputPath + "\\john-wick-parse-modded.exe")) //2.0-
            {
                File.Delete(App.DefaultOutputPath + "\\john-wick-parse-modded.exe");
            }
            if (File.Exists(App.DefaultOutputPath + "\\john-wick-parse_custom.exe")) //2.0+
            {
                File.Delete(App.DefaultOutputPath + "\\john-wick-parse_custom.exe");
            }
            if (Directory.Exists(App.DefaultOutputPath + "\\LocRes\\"))
            {
                Directory.Delete(App.DefaultOutputPath + "\\LocRes\\");
            }
        }

        /// <summary>
        /// this should tell me if i can read the file, to avoid crash when a pak is being written by the launcher
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        /// <summary>
        /// filter text with case insensitive support
        /// </summary>
        /// <param name="text"></param>
        /// <param name="value"></param>
        /// <param name="stringComparison"></param>
        /// <returns></returns>
        public static bool CaseInsensitiveContains(string text, string value, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        }
    }
}
