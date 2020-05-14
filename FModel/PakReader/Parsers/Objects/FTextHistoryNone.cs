namespace PakReader.Parsers.Objects
{
    public partial class FTextHistory
    {
        public sealed class None : FTextHistory
        {
            public readonly string CultureInvariantString;

            // https://github.com/EpicGames/UnrealEngine/blob/5677c544747daa1efc3b5ede31642176644518a6/Engine/Source/Runtime/Core/Private/Internationalization/Text.cpp#L974
            internal None(PackageReader reader)
            {
                if (reader.ReadInt32() != 0) // bHasCultureInvariantString
                {
                    CultureInvariantString = reader.ReadFString();
                }
            }
        }
    }
}
