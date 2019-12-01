using System;
using System.Diagnostics;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Utilities
{
    class DebugHelper
    {
        public static Logger Logger { get; private set; }

        public static void Init(string logFilePath)
        {
            Logger = new Logger(logFilePath);
        }

        public static void WriteLine(string message = "")
        {
            if (Logger != null)
            {
                Logger.WriteLine(message);
            }
            else
            {
                Debug.WriteLine(message);
            }
        }

        public static void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }

        public static void WriteException(string exception, string message = "Exception")
        {
            if (Logger != null)
            {
                Logger.WriteException(exception, message);
            }
            else
            {
                Debug.WriteLine(exception);
            }
        }

        public static void WriteException(Exception exception, string message = "Exception")
        {
            WriteException(exception.ToString(), message);
        }

        public static void WriteUserSettings()
        {
            DebugHelper.WriteLine("=============== USER SETTINGS ===============");
            DebugHelper.WriteLine("FPak_Path > " + FProp.Default.FPak_Path);
            DebugHelper.WriteLine("FOutput_Path > " + FProp.Default.FOutput_Path);
            DebugHelper.WriteLine("FPak_MainAES > " + FProp.Default.FPak_MainAES);
            DebugHelper.WriteLine("FRarity_Design > " + FProp.Default.FRarity_Design);
            DebugHelper.WriteLine("FLanguage > " + FProp.Default.FLanguage);
            DebugHelper.WriteLine("FIsFeatured > " + FProp.Default.FIsFeatured);
            DebugHelper.WriteLine("FUseWatermark > " + FProp.Default.FUseWatermark);
            DebugHelper.WriteLine("FWatermarkFilePath > " + FProp.Default.FWatermarkFilePath);
            DebugHelper.WriteLine("FWatermarkOpacity > " + FProp.Default.FWatermarkOpacity);
            DebugHelper.WriteLine("FWatermarkScale > " + FProp.Default.FWatermarkScale);
            DebugHelper.WriteLine("FWatermarkXPos > " + FProp.Default.FWatermarkXPos);
            DebugHelper.WriteLine("FWatermarkYPos > " + FProp.Default.FWatermarkYPos);
            DebugHelper.WriteLine("FChallengeWatermark > " + FProp.Default.FChallengeWatermark);
            DebugHelper.WriteLine("FUseChallengeWatermark > " + FProp.Default.FUseChallengeWatermark);
            DebugHelper.WriteLine("FBannerFilePath > " + FProp.Default.FBannerFilePath);
            DebugHelper.WriteLine("FBannerOpacity > " + FProp.Default.FBannerOpacity);
            DebugHelper.WriteLine("FPrimaryColor > " + FProp.Default.FPrimaryColor);
            DebugHelper.WriteLine("FSecondaryColor > " + FProp.Default.FSecondaryColor);
            DebugHelper.WriteLine("FUpdateSettings > " + FProp.Default.FUpdateSettings);
            DebugHelper.WriteLine("FDiffFileSize > " + FProp.Default.FDiffFileSize);
            DebugHelper.WriteLine("ReloadAES > " + FProp.Default.ReloadAES);
            DebugHelper.WriteLine("FOpenSounds > " + FProp.Default.FOpenSounds);
            DebugHelper.WriteLine("FAutoExtractRaw > " + FProp.Default.FAutoExtractRaw);
            DebugHelper.WriteLine("FAutoSaveJson > " + FProp.Default.FAutoSaveJson);
            DebugHelper.WriteLine("FAutoSaveImg > " + FProp.Default.FAutoSaveImg);
            DebugHelper.WriteLine("=============================================");
        }
    }
}
