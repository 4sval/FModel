﻿using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace FModel.Utils
{
    static class Strings
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FixPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Length < 5)
                return string.Empty;

            string trigger;
            {
                string tempPath = path[1..];
                trigger = tempPath.Substring(0, tempPath.IndexOf('/'));
            }

            Regex regex = new Regex(trigger);
            if (trigger.Equals("SrirachaRanch"))
                trigger += $"/{trigger}Core";

            string fixedPath = trigger switch
            {
                "Game" => regex.Replace(path, $"{Folders.GetGameName()}/Content", 1),
                "RegionCN" => regex.Replace(path, $"{Folders.GetGameName()}/Plugins/{trigger}/Content", 1),
                _ => regex.Replace(path, $"{Folders.GetGameName()}/Plugins/GameFeatures/{trigger}/Content", 1)
            };

            int sep = fixedPath.LastIndexOf('.');
            return fixedPath.Substring(0, sep > 0 ? sep : fixedPath.Length);
        }
    }
}
