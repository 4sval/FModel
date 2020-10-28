namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FMovieSceneFloatChannel : IUStruct
    {
        public readonly ERichCurveExtrapolation PreInfinityExtrap;
        public readonly ERichCurveExtrapolation PostInfinityExtrap;

        internal FMovieSceneFloatChannel(PackageReader reader)
        {
            PreInfinityExtrap = (ERichCurveExtrapolation)reader.ReadByte();
            PostInfinityExtrap = (ERichCurveExtrapolation)reader.ReadByte();

            //todo https://github.com/EpicGames/UnrealEngine/blob/release/Engine/Source/Runtime/MovieScene/Private/Channels/MovieSceneFloatChannel.cpp#L1092
        }
    }
}
