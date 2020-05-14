namespace PakReader.Pak
{
    class DefaultPakFilter : IPakFilter
    {
        public bool CheckFilter(string path, bool caseSensitive) => true;
    }
}
