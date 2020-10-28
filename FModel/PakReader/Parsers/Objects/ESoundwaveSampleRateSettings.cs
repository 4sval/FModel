namespace FModel.PakReader.Parsers.Objects
{
    public enum ESoundwaveSampleRateSettings : byte
    {
		Max,
		High,
		Medium,
		Low,
		Min,
		// Use this setting to resample soundwaves to the device's sample rate to avoid having to perform sample rate conversion at runtime.
		MatchDevice
	}
}
