namespace PakReader.Parsers.Objects
{
    public enum ESoundWaveLoadingBehavior : byte
    {
		// If set on a USoundWave, use the setting defined by the USoundClass. If set on the next parent USoundClass, or the default behavior defined via the au.streamcache cvars.
		Inherited = 0,
		// the first chunk of audio for this asset will be retained in the audio cache until a given USoundWave is either destroyed or USoundWave::ReleaseCompressedAudioData is called.
		RetainOnLoad = 1,
		// the first chunk of audio for this asset will be loaded into the cache from disk when this asset is loaded, but may be evicted to make room for other audio if it isn't played for a while.
		PrimeOnLoad = 2,
		// the first chunk of audio for this asset will not be loaded until this asset is played or primed.
		LoadOnDemand = 3,
		// Force all audio data for this audio asset to live outside of the cache and use the non-streaming decode pathways. Only usable if set on the USoundWave.
		ForceInline = 4,
		// This value is used to delineate when the value of ESoundWaveLoadingBehavior hasn't been cached on a USoundWave yet.
		Uninitialized = 0xFF
    }
}
