using System.Text.RegularExpressions;

namespace FModel.Utils
{
    static class Strings
    {
        public static string GetReadableSize(double size)
        {
            string[] sizes = { Properties.Resources.B, Properties.Resources.KB, Properties.Resources.MB, Properties.Resources.GB, Properties.Resources.TB };
            int order = 0;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return string.Format("{0:# ###.##} {1}", size, sizes[order]).TrimStart();
        }

        public static string FixPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            Regex regexGame = new Regex(Regex.Escape("Game"));
            string fixedPath = regexGame.Replace(path, $"{Folders.GetGameName()}/Content", 1);
            int sep = fixedPath.LastIndexOf('.');
            return fixedPath.Substring(0, sep > 0 ? sep : fixedPath.Length);
        }
    }
}
