using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Windows;
using CUE4Parse.GameTypes.Tencent.PUBGMobile.Encryption.SM4;
using FModel.Settings;
using FModel.Views;
using Serilog;

namespace FModel.Services;

internal static class PubgMobileKeyService
{
    private static readonly ConcurrentDictionary<uint, byte> DeclinedSlots = new();
    private static readonly object PromptGate = new();

    public static void Initialize()
    {
        foreach (var entry in UserSettings.Default.PubgMobileKeySlots.ToArray())
        {
            if (!IsValid(entry.Value))
            {
                Log.Warning("Ignoring invalid stored PUBG Mobile dynamic key slot {Slot}", entry.Key);
                continue;
            }

            PUBGMobileSM4.RegisterDynamicKeySalt(entry.Key, entry.Value.Trim());
        }

        PUBGMobileSM4.DynamicKeySaltResolver = ResolveMissingKey;
        PUBGMobileSM4.DynamicKeySaltReplacementResolver = ReplaceInvalidKey;
    }

    private static string? ResolveMissingKey(uint slot)
    {
        if (!UserSettings.Default.PubgMobileAskForMissingKeys || DeclinedSlots.ContainsKey(slot))
            return null;

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return null;

        lock (PromptGate)
        {
            if (PUBGMobileSM4.TryGetDynamicKeySalt(slot, out var existing))
                return existing;
            if (DeclinedSlots.ContainsKey(slot))
                return null;

            string? value = null;
            try { value = dispatcher.Invoke(() => PubgKeyPrompt.Ask(slot)); }
            catch (Exception e) { Log.Warning(e, "Could not display missing PUBG Mobile key prompt for slot {Slot}", slot); }

            if (!IsValid(value))
            {
                DeclinedSlots[slot] = 0;
                return null;
            }

            var key = value.Trim();
            PUBGMobileSM4.RegisterDynamicKeySalt(slot, key);
            UserSettings.Default.PubgMobileKeySlots[slot] = key;
            UserSettings.Save();
            return key;
        }
    }

    private static string? ReplaceInvalidKey(uint slot)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return null;

        lock (PromptGate)
        {
            UserSettings.Default.PubgMobileKeySlots.TryGetValue(slot, out var previousValue);
            UserSettings.Default.PubgMobileKeySlots.Remove(slot);
            PUBGMobileSM4.ResetDynamicKeySalt(slot);
            DeclinedSlots.TryRemove(slot, out _);
            UserSettings.Save();

            string? value = null;
            try { value = dispatcher.Invoke(() => PubgKeyPrompt.Replace(slot, previousValue)); }
            catch (Exception e) { Log.Warning(e, "Could not display invalid PUBG Mobile key prompt for slot {Slot}", slot); }

            if (!IsValid(value))
            {
                DeclinedSlots[slot] = 0;
                return null;
            }

            var key = value.Trim();
            PUBGMobileSM4.RegisterDynamicKeySalt(slot, key);
            UserSettings.Default.PubgMobileKeySlots[slot] = key;
            UserSettings.Save();
            return key;
        }
    }

    internal static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 128 && value.Trim().All(c => c is >= ' ' and <= '~');
}
